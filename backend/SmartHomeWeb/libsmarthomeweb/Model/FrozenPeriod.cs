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

		/// <summary>
		/// Gets the range of time described by this frozen
		/// period.
		/// </summary>
		[JsonIgnore]
		public Tuple<DateTime, DateTime> Range { get { return Tuple.Create(StartTime, EndTime); } }

		/// <summary>
		/// Gets a value indicating whether this range is empty.
		/// </summary>
		/// <value><c>true</c> if this range is empty; otherwise, <c>false</c>.</value>
		[JsonIgnore]
		public bool IsEmpty { get { return IsEmptyRange(Range); } }

		/// <summary>
		/// Tests if this time period contains the given
		/// time.
		/// </summary>
		public bool Contains(DateTime Time)
		{
			return Time >= StartTime && Time <= EndTime;
		}

		/// <summary>
		/// Tests if this time period overlaps with the given
		/// time period.
		/// </summary>
		public bool OverlapsWith(FrozenPeriod Other)
		{
			return Other.StartTime <= this.EndTime && this.StartTime <= Other.EndTime;
		}

		public static CompactionLevel Max(CompactionLevel Left, CompactionLevel Right)
		{
			if ((int)Left < (int)Right)
				return Right;
			else
				return Left;
		}

		public static CompactionLevel Min(CompactionLevel Left, CompactionLevel Right)
		{
			if ((int)Left > (int)Right)
				return Right;
			else
				return Left;
		}

		public static DateTime Max(DateTime Left, DateTime Right)
		{
			if (Left < Right)
				return Right;
			else
				return Left;
		}

		public static DateTime Min(DateTime Left, DateTime Right)
		{
			if (Left > Right)
				return Right;
			else
				return Left;
		}

		public static Tuple<DateTime, DateTime> Intersect(
			Tuple<DateTime, DateTime> LeftRange, 
			Tuple<DateTime, DateTime> RightRange)
		{
			return Tuple.Create(
				Max(LeftRange.Item1, RightRange.Item1),
				Min(LeftRange.Item2, RightRange.Item2));
		}

		public static bool IsEmptyRange(Tuple<DateTime, DateTime> Range)
		{
			return Range.Item1 >= Range.Item2;
		}
	}
}

