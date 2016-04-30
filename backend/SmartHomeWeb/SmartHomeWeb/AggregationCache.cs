using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmartHomeWeb.Model;

namespace SmartHomeWeb
{
	// When reading source code pertaining to aggregation, it
	// is useful to keep in mind that aggregation relies on a
	// hierarchical tree-like structure:
	// 
	//                    year-average
	//                         ^
	//                         |
	//                    month-average
	//                         ^
	//                         |
	//                    day-average
	//                         ^
	//                         |
	//                    hour-average
	//                         ^
	//                         |
	//                    measurements
	//
	// When computing an average, all required averages in the
	// layer below the current average layer will be computed 
	// first, and will then be used to compute the requested
	// average. Additionally, all averages are cached. 
	// Therefore, computing a year-average is very efficient if
	// the month-averages for that year have already been computed.
	//
	// This is analogous to how `make` behaves: the initial build
	// is typically slow, but incremental changes can be compiled
	// very quickly. Likewise, the  initial aggregation pass may 
	// take a bit of time, small to medium-sized additions to 
	// the pool of measurements can be processed very efficiently.
	//
	// Additionally, aggregation also need to take compaction into 
	// account: some of the lower aggregation tiers may have been
	// erased. In that case, we actually have to pull data from the
	// upper tiers, and duplicate it to fill the specified range of 
	// measurements.
	//
	// Note that we do not compact beyond day-averages, because
	// that just doesn't make sense: a decade of month-average data
	// corresponds to just two hours of per-minute data.

	/// <summary>
	/// A data structure that caches data that pertains to 
	/// aggregation in-memory.
	/// </summary>
	public sealed class AggregationCache
	{
		// Pre-fetch data for up to 512 hours of measurements.
		private const int PrefetchSize = 512;
		
		// Have every task process up to 64 hours of measurements
		// when computing hour averages in parallel.
		private const int ParallelComputeSize = 64;

		public AggregationCache(
			DataConnection Database, int SensorId, 
			DateTime CacheStart, DateTime CacheEnd)
		{
			this.Database = Database;
			this.SensorId = SensorId;
			this.CacheStart = CacheStart;
			this.CacheEnd = CacheEnd;

			this.hourData = new Dictionary<DateTime, List<Measurement>>();
			this.hourAverages = null;
			this.dayAverages = null;
			this.monthAverages = null;
			this.precomputedHours = null;
			this.precomputedDays = null;
			this.precomputedMonths = null;
		}

		public DataConnection Database { get; private set; }

		/// <summary>
		/// Gets the identifier of the sensor whose measurements
		/// are being aggregated.
		/// </summary>
		public int SensorId { get; private set; }

		/// <summary>
		/// Gets the date-time at which caching begins.
		/// </summary>
		public DateTime CacheStart { get; private set; }

		/// <summary>
		/// Gets the date-time at which caching end.
		/// </summary>
		public DateTime CacheEnd { get; private set; }

		private Dictionary<DateTime, List<Measurement>> hourData;
		private Dictionary<DateTime, Measurement> hourAverages;
		private Dictionary<DateTime, Measurement> dayAverages;
		private Dictionary<DateTime, Measurement> monthAverages;

		private HashSet<DateTime> precomputedHours;
		private HashSet<DateTime> precomputedDays;
		private HashSet<DateTime> precomputedMonths;

		private IEnumerable<FrozenPeriod> frozenPeriods;

		/// <summary>
		/// Fetches relevant frozen periods from the database.
		/// </summary>
		public async Task<IEnumerable<FrozenPeriod>> FetchFrozenPeriodsAsync()
		{
			if (frozenPeriods == null)
			{
				frozenPeriods = await Database.GetFrozenPeriodsAsync(CacheStart, CacheEnd);
			}
			return frozenPeriods;
		}

		private IEnumerable<FrozenPeriod> SubdivideCompactionPeriod(FrozenPeriod Original, FrozenPeriod New)
		{
			var inter = FrozenPeriod.Intersect(Original.Range, New.Range);
			if (FrozenPeriod.IsEmptyRange(inter))
			{
				return new FrozenPeriod[] { Original };
			}
			else
			{
				return new FrozenPeriod[] 
				{
					new FrozenPeriod(Original.StartTime, inter.Item1, Original.Compaction),
					new FrozenPeriod(inter.Item1, inter.Item2, FrozenPeriod.Max(Original.Compaction, New.Compaction)),
					new FrozenPeriod(inter.Item2, Original.EndTime, Original.Compaction)
				}.Where(item => !item.IsEmpty);
			}
		}

		/// <summary>
		/// Partitions the given time period by its compaction level. The resulting
		/// sequence of compaction periods is ordered. The given compaction level
        /// specifies the minimal compaction for this period of time.
		/// </summary>
		public async Task<IEnumerable<FrozenPeriod>> PartitionByCompaction(
            DateTime Start, DateTime End, CompactionLevel Compaction)
		{
			// Start with a single period of time,
			// and then subdivide that iteratively.
			var results = new List<FrozenPeriod>();
            results.Add(new FrozenPeriod(Start, End, Compaction));
			foreach (var item in await FetchFrozenPeriodsAsync())
			{
				var newResults = new List<FrozenPeriod>();
				foreach (var range in results)
				{
					newResults.AddRange(SubdivideCompactionPeriod(range, item));
				}
				results = newResults;
			}
			return results;
		}

        /// <summary>
        /// Partitions the given time period by its compaction level. The resulting
        /// sequence of compaction periods is ordered.
        /// </summary>
        public Task<IEnumerable<FrozenPeriod>> PartitionByCompaction(DateTime Start, DateTime End)
        {
            return PartitionByCompaction(Start, End, CompactionLevel.None);
        }

        /// <summary>
        /// Partitions the given time period by its compaction level. The resulting
        /// sequence of compaction periods is ordered.
        /// </summary>
        public static Task<IEnumerable<FrozenPeriod>> PartitionByCompaction(
            DataConnection Connection, DateTime Start, DateTime End,
            CompactionLevel Compaction = CompactionLevel.None)
        {
            // Use '0' as a dummy sensor ID. We won't be looking at
            // the sensor's measurements, anyway.
            // TODO: maybe create a separate class for this.
            var cache = new AggregationCache(Connection, 0, Start, End);
            return cache.PartitionByCompaction(Start, End, Compaction);
        }

		/// <summary>
		/// Prefetches precomputed hour average data from the database.
		/// </summary>
		public Task FetchHourAveragesAsync()
		{
			return FetchAveragesAsync(
				ref hourAverages, ref precomputedHours,
				DataConnection.HourAverageTableName,
				MeasurementAggregation.QuantizeHour,
				MeasurementAggregation.CeilingHour);
		}

		/// <summary>
		/// Prefetches precomputed day average data from the database.
		/// </summary>
		public Task FetchDayAveragesAsync()
		{
			return FetchAveragesAsync(
				ref dayAverages, ref precomputedDays, 
				DataConnection.DayAverageTableName,
				MeasurementAggregation.QuantizeDay,
				MeasurementAggregation.CeilingDay);
		}

		/// <summary>
		/// Prefetches precomputed months average data from the database.
		/// </summary>
		public Task FetchMonthAveragesAsync()
		{
			return FetchAveragesAsync(
				ref monthAverages, ref precomputedMonths, 
				DataConnection.MonthAverageTableName,
				MeasurementAggregation.QuantizeMonth,
				MeasurementAggregation.CeilingMonth);
		}

		private Task FetchAveragesAsync(
			ref Dictionary<DateTime, Measurement> Cache, 
			ref HashSet<DateTime> PrecomputedSet, string TableName,
			Func<DateTime, DateTime> QuantizeFloor, 
			Func<DateTime, DateTime> QuantizeCeiling)
		{
			if (Cache != null)
				// Don't fetch twice.
				return Task.FromResult(true);

			Cache = new Dictionary<DateTime, Measurement>();
			PrecomputedSet = new HashSet<DateTime>();
			return FetchAveragesImplAsync(
				Cache, PrecomputedSet, TableName, 
				QuantizeFloor, QuantizeCeiling);
		}

		private async Task FetchAveragesImplAsync(
			Dictionary<DateTime, Measurement> Cache, 
			HashSet<DateTime> PrecomputedSet, string TableName,
			Func<DateTime, DateTime> QuantizeFloor, 
			Func<DateTime, DateTime> QuantizeCeiling)
		{
			foreach (var item in await Database.GetMeasurementsAsync(
				TableName, SensorId, QuantizeFloor(CacheStart), 
				QuantizeCeiling(CacheEnd)))
			{
				Cache[item.Time] = item;
				PrecomputedSet.Add(item.Time);
			}
		}

		private async Task FetchMeasurementsAsync(DateTime Hour)
		{
			// Discard all old data.
			hourData = new Dictionary<DateTime, List<Measurement>>();

			// Determine where to stop.
			var endHour = Hour.AddHours(PrefetchSize);
			if (endHour > CacheEnd)
				endHour = CacheEnd;

			// Initialize the hour data cache.
			for (DateTime i = Hour; i < endHour; i = i.AddHours(1))
			{
				hourData[i] = new List<Measurement>();
			}
			
			// Fetch raw data.
			var measurements = await Database.GetRawMeasurementsAsync(SensorId, Hour, endHour);

			// Insert data into cache.
			foreach (var item in measurements)
			{
				var hour = MeasurementAggregation.Quantize(item.Time, TimeSpan.FromHours(1));
				hourData[hour].Add(item);
			}
		}

		private async Task<IEnumerable<Measurement>> GetMeasurementsAsync(DateTime Hour)
		{
			List<Measurement> result;
			if (hourData.TryGetValue(Hour, out result))
			{
				return result;
			}
			else
			{
				await FetchMeasurementsAsync(Hour);
				return hourData[Hour];
			}
		}

		/// <summary>
		/// Gets the hour average for the given hour.
		/// </summary>
		private async Task<Measurement> GetRealHourAverageAsync(DateTime Hour)
		{
			if (hourAverages == null)
				await FetchHourAveragesAsync();
			
			Measurement result;
			if (hourAverages.TryGetValue(Hour, out result))
			{
				return result;
			}
			else
			{
				result = MeasurementAggregation.Aggregate(
					await GetMeasurementsAsync(Hour), SensorId, Hour);
				hourAverages[Hour] = result;
				return result;
			}
		}

		/// <summary>
		/// Computes the given number of 
		/// </summary>
		private void ComputeInCacheHourAverages(
			DateTime Start, int Offset, int Count, Measurement[] Target)
		{
			for (int i = 0; i < Count; i++)
			{
				var hour = Start.AddHours(i + Offset);
				Target[i + Offset] = MeasurementAggregation.Aggregate(hourData[hour], SensorId, hour);
			}
		}

		private Task ComputeInCacheHourAveragesAsync(
			DateTime Start, int Offset, int Count, Measurement[] Target)
		{
			int taskCount = Count / ParallelComputeSize;
			int rem = Count % ParallelComputeSize;
			var tasks = new Task[taskCount + (rem > 0 ? 1 : 0)];
			for (int i = 0; i < taskCount; i++)
			{
				int startIndex = Offset + i * ParallelComputeSize;
				tasks[i] = Task.Run(() => ComputeInCacheHourAverages(
					Start, startIndex, ParallelComputeSize, Target));
			}
			if (rem > 0)
			{
				int startIndex = Offset + taskCount * ParallelComputeSize;
				tasks[taskCount] = Task.Run(() => ComputeInCacheHourAverages(
					Start, startIndex, rem, Target));
			}
			return Task.WhenAll(tasks);
		}

		/// <summary>
		/// Computes hour averages for a sizeable number of hours. 
		/// Results are stored in the given array.
		/// </summary>
		private async Task ComputeHourAveragesAsync(DateTime Start, int Offset, int Count, Measurement[] Target)
		{
			if (Count < ParallelComputeSize)
			{
				// Don't even try to parallelize if it's not worth the effort.
				for (int i = 0; i < Count; i++)
				{
					Target[i + Offset] = await GetRealHourAverageAsync(Start.AddHours(i));
				}
				return;
			}

			// Since we'll be aggregating lots of data, we'll
			// distribute the workload across multiple threads.
			int rem = Count % PrefetchSize;
			int iters = Count / PrefetchSize;
			for (int i = 0; i < iters; i++)
			{
				int startIndex = Offset + i * PrefetchSize;
				await FetchMeasurementsAsync(Start.AddHours(startIndex));
				await ComputeInCacheHourAveragesAsync(
					Start, startIndex, PrefetchSize, Target);
			}
			if (rem > 0)
			{
				int startIndex = Offset + Count - rem;
				await FetchMeasurementsAsync(Start.AddHours(startIndex));
				await ComputeInCacheHourAveragesAsync(
					Start, startIndex, rem, Target);
			}

			// Update the hour-averages dictionary.
			for (int i = 0; i < Count; i++)
			{
				var item = Target[i + Offset];
				hourAverages[item.Time] = item;
			}
		}

		/// <summary>
		/// Computes hour averages for a sizeable number of hours. 
		/// </summary>
		private async Task<Measurement[]> ComputeHourAveragesAsync(DateTime Start, int Count)
		{
			var results = new Measurement[Count];
			await ComputeHourAveragesAsync(Start, 0, Count, results);
			return results;
		}

		/// <summary>
		/// Gets the hour averages for a sizeable number of hours.
		/// </summary>
		private async Task<IEnumerable<Measurement>> GetRealHourAveragesAsync(DateTime Start, int Count)
		{
			if (hourAverages == null)
				await FetchHourAveragesAsync();

			// We want to compute _only_ those averages which
			// are already present in the cache.
			var results = new Measurement[Count];
			int regionSize = 0;
			for (int i = 0; i < Count; i++)
			{
				var key = Start.AddHours(i);
				if (hourAverages.TryGetValue(key, out results[i]))
				{
					if (regionSize > 0)
					{
						int regionStart = i - regionSize;
						await ComputeHourAveragesAsync(
							Start, regionStart, regionSize, results);
						regionSize = 0;
					}
				}
				else
				{
					// This item is not present in the cache.
					// Don't compute it just yet, though.
					// We'd much rather group the aggregations
					// together, and do them in parallel.
					regionSize++;
				}
			}
			if (regionSize > 0)
			{
				int regionStart = Count - regionSize;
				await ComputeHourAveragesAsync(
					Start, regionStart, regionSize, results);
			}
			return results;
		}

		private async Task<Measurement[]> GetManyAveragesAsync(
			DateTime Start, DateTime End, TimeSpan Delta,
			Func<DateTime, Task<Measurement>> GetAverageAsync)
		{
			var results = new List<Measurement>();
			for (DateTime i = Start; i < End; i += Delta)
			{
				results.Add(await GetAverageAsync(i));
			}
			return results.ToArray();
		}

		private async Task<IEnumerable<Measurement>> GetManyAveragesAsync(
			DateTime Start, int Count, Func<DateTime, int, DateTime> AddQuanta,
			Func<DateTime, Task<Measurement>> GetAverageAsync)
		{
			var results = new Measurement[Count];
			for (int i = 0; i < Count; i++)
			{
				var time = AddQuanta(Start, i);
				results[i] = await GetAverageAsync(time);
			}
			return results;
		}

		/// <summary>
		/// Gets the day average for the given day.
		/// </summary>
		private async Task<Measurement> GetRealDayAverageAsync(DateTime Day)
		{
			if (dayAverages == null)
				await FetchDayAveragesAsync();

			Measurement result;
			if (dayAverages.TryGetValue(Day, out result))
			{
				return result;
			}
			else
			{
				result = MeasurementAggregation.Aggregate(
					await GetRealHourAveragesAsync(Day, 24), 
					SensorId, Day, Enumerable.Average);
				dayAverages[Day] = result;
				return result;
			}
		}

		/// <summary>
		/// Gets the day averages for the given days.
		/// </summary>
		private async Task<IEnumerable<Measurement>> GetRealDayAveragesAsync(DateTime StartDay, int Count)
		{
			if (dayAverages == null)
				await FetchDayAveragesAsync();

			// We want to compute _only_ those averages which
			// are not present yet in the cache, but we also
			// want to do that in parallel.
			var results = new Measurement[Count];
			int regionSize = 0;
			for (int i = 0; i < Count; i++)
			{
				var key = StartDay.AddDays(i);
				if (dayAverages.TryGetValue(key, out results[i]))
				{
					if (regionSize > 0)
					{
						if (hourAverages == null)
							await FetchHourAveragesAsync();

						int regionStart = i - regionSize;
						var regionStartDay = StartDay.AddDays(regionStart);
						await ComputeHourAveragesAsync(
							regionStartDay, 24 * regionSize);
						for (int j = regionStart; j < i; j++)
						{
							results[j] = await GetRealDayAverageAsync(StartDay.AddDays(j));
						}
						regionSize = 0;
					}
				}
				else
				{
					// This item is not present in the cache.
					// Don't compute it just yet, though.
					// We'd much rather group the aggregations
					// together, and do them in parallel.
					regionSize++;
				}
			}
			if (regionSize > 0)
			{
				if (hourAverages == null)
					await FetchHourAveragesAsync();

				int regionStart = Count - regionSize;
				var regionStartDay = StartDay.AddDays(regionStart);
				await ComputeHourAveragesAsync(
					regionStartDay, 24 * regionSize);
				for (int j = regionStart; j < Count; j++)
				{
					results[j] = await GetRealDayAverageAsync(StartDay.AddDays(j));
				}
			}
			return results;
		}

		private async Task<IEnumerable<Measurement>> GetDayAveragesAsync(FrozenPeriod Period)
		{
			switch (Period.Compaction)
			{
				case CompactionLevel.DayAverages:
					// We'll re-create day-average data by replicating 
					// month-average data.
					var results = new List<Measurement>();
					for (var day = Period.StartTime; day < Period.EndTime; day = day.AddDays(1))
					{
						var monthMeasurement = await GetMonthAverageAsync(MeasurementAggregation.QuantizeMonth(day));
						results.Add(new Measurement(SensorId, day, monthMeasurement.MeasuredData, monthMeasurement.Notes));
					}
					return results;

				case CompactionLevel.HourAverages:
				case CompactionLevel.Measurements:
				case CompactionLevel.None:
				default:
					// We can just fetch or create "real" day-average data.
					return await GetRealDayAveragesAsync(Period.StartTime, (int)Period.Duration.TotalDays);
			}
		}

		/// <summary>
		/// Replicates each element in the given sequence a given 
		/// number of times.
		/// </summary>
		private IEnumerable<Measurement> ReplicateEach(
			IEnumerable<Measurement> Items, DateTime StartTime,
			DateTime EndTime, TimeSpan Delta, int ReplicationCount)
		{
			var t = StartTime;
			foreach (var item in Items)
			{
				for (int i = 0; i < ReplicationCount; i++)
				{
					if (t >= EndTime)
						yield break;

					yield return new Measurement(
						item.SensorId, t, 
						item.MeasuredData, item.Notes);
					t += Delta;
				}
			}
		}

		private async Task<IEnumerable<Measurement>> GetHourAveragesAsync(FrozenPeriod Period)
		{
			const int HoursPerDay = 24;

			switch (Period.Compaction)
			{
				case CompactionLevel.DayAverages:
				case CompactionLevel.HourAverages:
					// We'll re-create hour-average data by replicating 
					// day-average data.
					var results = await GetDayAveragesAsync(
						MeasurementAggregation.Quantize(Period.StartTime, TimeSpan.FromDays(1)),
						(int)Math.Ceiling(Period.Duration.TotalDays));
					return ReplicateEach(results, Period.StartTime, Period.EndTime, TimeSpan.FromHours(1), HoursPerDay).ToArray();

				case CompactionLevel.Measurements:
				case CompactionLevel.None:
				default:
					// We can just fetch or create "real" hour-average data.
					return await GetRealHourAveragesAsync(Period.StartTime, (int)Period.Duration.TotalHours);
			}
		}

		private async Task<IEnumerable<Measurement>> GetMeasurementsAsync(FrozenPeriod Period)
		{
			const int MinutesPerHour = 60;

			switch (Period.Compaction)
			{
				case CompactionLevel.DayAverages:
				case CompactionLevel.HourAverages:
				case CompactionLevel.Measurements:
					// We'll re-create measurement data by replicating 
					// hour-average data.
					var results = await GetHourAveragesAsync(
	                    MeasurementAggregation.Quantize(Period.StartTime, TimeSpan.FromHours(1)), 
	                    (int)Math.Ceiling(Period.Duration.TotalHours));
					return ReplicateEach(results, Period.StartTime, Period.EndTime, TimeSpan.FromMinutes(1), MinutesPerHour).ToArray();
					
				case CompactionLevel.None:
				default:
					// We can just fetch real measurements from the database.
					return await Database.GetRawMeasurementsAsync(SensorId, Period.StartTime, Period.EndTime);
			}
		}

		/// <summary>
		/// Partitions the given period of time by compaction level, 
		/// applies the given function to each partition, and 
		/// concatenates the results.
		/// </summary>
		private async Task<IEnumerable<Measurement>> GetOptionallyCompactedAsync(
			DateTime StartTime, DateTime EndTime, 
			Func<FrozenPeriod, Task<IEnumerable<Measurement>>> GetResultsAsync)
		{
			var results = new List<Measurement>();
			foreach (var item in await PartitionByCompaction(StartTime, EndTime))
			{
				results.AddRange(await GetResultsAsync(item));
			}
			return results;
		}

		/// <summary>
		/// Gets all measurements made in the period defined by
		/// the given start and end date-times. If these measurements
		/// have been compacted, then they will be synthesized from
		/// hour-average data.
		/// </summary>
		public Task<IEnumerable<Measurement>> GetMeasurementsAsync(DateTime StartTime, DateTime EndTime)
		{
			return GetOptionallyCompactedAsync(StartTime, EndTime, GetMeasurementsAsync);
		}

		/// <summary>
		/// Gets the hour-averages for the given hours. If these
		/// averages have been compacted, then they will be synthesized
		/// from day-average data.
		/// </summary>
		public Task<IEnumerable<Measurement>> GetHourAveragesAsync(DateTime StartHour, int Count)
		{
			return GetOptionallyCompactedAsync(StartHour, StartHour.AddHours(Count), GetHourAveragesAsync);
		}

		/// <summary>
		/// Gets the day-averages for the given days. If these
		/// averages have been compacted, then they will be synthesized
		/// from month-average data.
		/// </summary>
		public Task<IEnumerable<Measurement>> GetDayAveragesAsync(DateTime StartDay, int Count)
		{
			return GetOptionallyCompactedAsync(StartDay, StartDay.AddDays(Count), GetDayAveragesAsync);
		}

		/// <summary>
		/// Gets the month-average for the given month.
		/// </summary>
		public async Task<Measurement> GetMonthAverageAsync(DateTime Month)
		{
			if (monthAverages == null)
				await FetchMonthAveragesAsync();

			Measurement result;
			if (monthAverages.TryGetValue(Month, out result))
			{
				return result;
			}
			else
			{
				var timeSpan = Month.AddMonths(1) - Month;
				result = MeasurementAggregation.Aggregate(
					await GetRealDayAveragesAsync(Month, (int)timeSpan.TotalDays), 
					SensorId, Month, Enumerable.Average);
				monthAverages[Month] = result;
				return result;
			}
		}

		/// <summary>
		/// Gets the month-averages for the given months.
		/// </summary>
		public Task<IEnumerable<Measurement>> GetMonthAveragesAsync(DateTime StartMonth, int Count)
		{
			return GetManyAveragesAsync(StartMonth, Count, (dt, i) => dt.AddMonths(i), GetMonthAverageAsync);
		}

		private void FlushResults(
			Dictionary<DateTime, Measurement> Results,
			HashSet<DateTime> Precomputed,
			string TableName)
		{
			if (Results == null)
				return;

			// This command resource is managed manually, because
			// we may have to insert lots of measurements.
			var cmd = Database.CreateCommand();
			try
			{
				cmd.CommandText = $"INSERT INTO {TableName}(sensorId, unixtime, measured, notes) " +
					"VALUES (@sensorId, @unixtime, @measured, @notes)";
				cmd.Parameters.AddWithValue("@sensorId", 0);
				cmd.Parameters.AddWithValue("@unixtime", 0L);
				cmd.Parameters.AddWithValue("@measured", default(double?));
				cmd.Parameters.AddWithValue("@notes", "");
				foreach (var item in Results)
				{
					if (!Precomputed.Contains(item.Key))
					{
						var val = item.Value;
						cmd.Parameters["@sensorId"].Value = val.SensorId;
						cmd.Parameters["@unixtime"].Value = DatabaseHelpers.CreateUnixTimeStamp(val.Time);
						cmd.Parameters["@measured"].Value = val.MeasuredData;
						cmd.Parameters["@notes"].Value = val.Notes;
						cmd.ExecuteNonQuery();
					}
				}
			}
			finally
			{
				cmd.Dispose();
			}
		}

		/// <summary>
		/// Discards all computed and cached hour-averages.
		/// </summary>
		public void DiscardHourAverages()
		{
			hourAverages = null;
			precomputedHours = null;
		}

		/// <summary>
		/// Discards all computed and cached day-averages.
		/// </summary>
		public void DiscardDayAverages()
		{
			dayAverages = null;
			precomputedDays = null;
		}

		/// <summary>
		/// Discards all computed and cached month-averages.
		/// </summary>
		public void DiscardMonthAverages()
		{
			monthAverages = null;
			precomputedMonths = null;
		}

		/// <summary>
		/// Writes data from this aggregation cache to the database.
		/// </summary>
		public void FlushResults()
		{
			FlushResults(hourAverages, precomputedHours, DataConnection.HourAverageTableName);
			FlushResults(dayAverages, precomputedDays, DataConnection.DayAverageTableName);
			FlushResults(monthAverages, precomputedMonths, DataConnection.MonthAverageTableName);
		}
	}
}

