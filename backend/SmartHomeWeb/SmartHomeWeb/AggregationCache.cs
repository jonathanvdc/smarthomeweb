using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmartHomeWeb.Model;

namespace SmartHomeWeb
{
	/// <summary>
	/// A data structure that caches data that pertains to 
	/// aggregation in-memory.
	/// </summary>
	public class AggregationCache
	{
		// Pre-fetch data up to 512 hours of measurements.
		private const int PrefetchSize = 512;

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

		private Task<Measurement[]> GetManyAveragesAsync(
			DateTime Start, DateTime End, TimeSpan Delta,
			Func<DateTime, Task<Measurement>> GetAverageAsync)
		{
			var results = new List<Task<Measurement>>();
			for (DateTime i = Start; i < End; i += Delta)
			{
				results.Add(GetAverageAsync(i));
			}
			return Task.WhenAll(results);
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
					await GetManyAveragesAsync(Day, Day.AddDays(1), TimeSpan.FromHours(1), GetHourAverageAsync), 
					SensorId, Day, Enumerable.Average);
				dayAverages[Day] = result;
				return result;
			}
		}

		/// <summary>
		/// Gets the month average for the given month.
		/// </summary>
		public async Task<Measurement> GetMonthAverageAsync(DateTime Month)
		{
			return MeasurementAggregation.Aggregate(
				await GetManyAveragesAsync(Month, Month.AddMonths(1), TimeSpan.FromDays(1), GetDayAverageAsync), 
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

