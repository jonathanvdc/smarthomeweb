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
			this.hourAverages = new Dictionary<DateTime, Measurement>();
			this.dayAverages = new Dictionary<DateTime, Measurement>();
			this.precomputedHours = new HashSet<DateTime>();
			this.precomputedDays = new HashSet<DateTime>();
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

		public int TotalHours { get { return (int)(CacheEnd - CacheStart).TotalHours; } }

		private Dictionary<DateTime, List<Measurement>> hourData;
		private Dictionary<DateTime, Measurement> hourAverages;
		private Dictionary<DateTime, Measurement> dayAverages;

		private HashSet<DateTime> precomputedHours;
		private HashSet<DateTime> precomputedDays;

		/// <summary>
		/// Prefetches precomputed hour average data from the database.
		/// </summary>
		public Task PrefetchHourAveragesAsync()
		{
			return PrefetchAveragesAsync(
				hourAverages, precomputedHours,
				DataConnection.HourAverageTableName);
		}

		/// <summary>
		/// Prefetches precomputed day average data from the database.
		/// </summary>
		public async Task PrefetchDayAveragesAsync()
		{
			await PrefetchAveragesAsync(
				dayAverages, precomputedDays, 
				DataConnection.DayAverageTableName);
			
			if (dayAverages.Count < TotalHours)
				// If the number of cached hours in the day-average cache 
				// does not equal the total number of hours, then we will
				// also prefetch the entire hour-average cache.
				await PrefetchHourAveragesAsync();
		}

		private async Task PrefetchAveragesAsync(
			Dictionary<DateTime, Measurement> Cache, 
			HashSet<DateTime> PrecomputedSet, string TableName)
		{
			foreach (var item in await Database.GetMeasurementsAsync(
				TableName, SensorId, CacheStart, CacheEnd))
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
			var measurements = await Database.GetMeasurementsAsync(SensorId, Hour, endHour);

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
		public async Task<Measurement> GetHourAverageAsync(DateTime Hour)
		{
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
					Target[i + Offset] = await GetHourAverageAsync(Start.AddHours(i));
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
		public async Task<IEnumerable<Measurement>> GetHourAveragesAsync(DateTime Start, int Count)
		{
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

		/// <summary>
		/// Gets the day average for the given day.
		/// </summary>
		public async Task<Measurement> GetDayAverageAsync(DateTime Day)
		{
			Measurement result;
			if (dayAverages.TryGetValue(Day, out result))
			{
				return result;
			}
			else
			{
				result = MeasurementAggregation.Aggregate(
					await GetHourAveragesAsync(Day, 24), 
					SensorId, Day, Enumerable.Average);
				dayAverages[Day] = result;
				return result;
			}
		}

		/// <summary>
		/// Gets the day averages for the given days.
		/// </summary>
		public async Task<IEnumerable<Measurement>> GetDayAveragesAsync(DateTime StartDay, int Count)
		{
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
						int regionStart = i - regionSize;
						var regionStartDay = StartDay.AddDays(regionStart);
						await ComputeHourAveragesAsync(
							regionStartDay, 24 * regionSize);
						for (int j = regionStart; j < i; j++)
						{
							results[j] = await GetDayAverageAsync(regionStartDay.AddDays(j));
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
				int regionStart = Count - regionSize;
				var regionStartDay = StartDay.AddDays(regionStart);
				await ComputeHourAveragesAsync(
					regionStartDay, 24 * regionSize);
				for (int j = regionStart; j < Count; j++)
				{
					results[j] = await GetDayAverageAsync(regionStartDay.AddDays(j));
				}
			}
			return results;
		}

		/// <summary>
		/// Gets the month average for the given month.
		/// </summary>
		public async Task<Measurement> GetMonthAverageAsync(DateTime Month)
		{
			var timeSpan = Month.AddMonths(1) - Month;
			return MeasurementAggregation.Aggregate(
				await GetDayAveragesAsync(Month, (int)timeSpan.TotalDays), 
				SensorId, Month, Enumerable.Average);
		}

		/// <summary>
		/// Writes data from this aggregation cache to the database.
		/// </summary>
		public async Task FlushHoursAsync()
		{
			foreach (var item in hourAverages)
			{
				if (!precomputedHours.Contains(item.Key))
				{
					await Database.InsertMeasurementAsync(item.Value, DataConnection.HourAverageTableName);
				}
			}
		}

		/// <summary>
		/// Writes data from this aggregation cache to the database.
		/// </summary>
		public async Task FlushDaysAsync()
		{
			await FlushHoursAsync();
			foreach (var item in dayAverages)
			{
				if (!precomputedDays.Contains(item.Key))
				{
					await Database.InsertMeasurementAsync(item.Value, DataConnection.DayAverageTableName);
				}
			}
		}
	}
}

