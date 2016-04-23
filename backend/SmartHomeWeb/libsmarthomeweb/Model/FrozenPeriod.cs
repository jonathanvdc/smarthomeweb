using System;
using Newtonsoft.Json;

namespace SmartHomeWeb
{
	/// <summary>
	/// Defines various levels of measurement compaction.
	/// </summary>
	public enum CompactionLevel
	{
		/// <summary>
		/// No data is compacted.
		/// </summary>
		None = 0,

		/// <summary>
		/// Raw measurements are compacted: only hour-averages 
		/// and up are available. 
		/// </summary>
		Measurements = 1,

		/// <summary>
		/// Raw measurements and hour-averages are compacted: 
		/// only day-averages and up are available. 
		/// </summary>
		HourAverages = 2,

		/// <summary>
		/// Raw measurements, hour-averages and day-averages 
		/// are compacted: only month-averages and up are available. 
		/// </summary>
		DayAverages = 3
	}

	/// <summary>
	/// Describes a frozen period in the database.
	/// </summary>
	public sealed class FrozenPeriod
	{
		[JsonConstructor]
		private FrozenPeriod() 
		{ }

		public FrozenPeriod(
			DateTime StartTime, DateTime EndTime, CompactionLevel Compaction)
		{
			if (EndTime < StartTime) 
			{
				throw new ArgumentException($"{nameof(StartTime)} was greater than {nameof(EndTime)}");
			}

			this.StartTime = StartTime;
			this.EndTime = EndTime;
			this.Compaction = Compaction;
		}

		/// <summary>
		/// Gets the date-time at which this frozen period starts.
		/// </summary>
		/// <value>The start time.</value>
		[JsonProperty("startTime")]
		public DateTime StartTime { get; private set; }

		/// <summary>
		/// Gets the date-time at which this frozen period ends.
		/// </summary>
		/// <value>The end time.</value>
		[JsonProperty("endTime")]
		public DateTime EndTime { get; private set; }

		/// <summary>
		/// Gets this frozen period's level of compaction.
		/// </summary>
		[JsonProperty("compactionLevel")]
		public CompactionLevel Compaction { get; private set; }

		/// <summary>
		/// Gets this frozen period's duration.
		/// </summary>
		[JsonIgnore]
		public TimeSpan Duration { get { return EndTime - StartTime; } }
	}
}

