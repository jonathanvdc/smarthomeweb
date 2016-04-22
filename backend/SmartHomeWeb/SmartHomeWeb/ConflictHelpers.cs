using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartHomeWeb.Model;

namespace SmartHomeWeb
{
	public static class TimeHelpers
	{
		private static Tuple<DateTime, DateTime, HashSet<int>> ComputeTimePeriod(Measurement[] Data, int Offset, int Count)
		{
			DateTime min = Data[Offset].Time;
			DateTime max = Data[Offset].Time;
			var ids = new HashSet<int>();

			int end = Offset + Count;
			for (int i = Offset; i < end; i++)
			{
				var time = Data[i].Time;
				if (time < min)
					min = time;
				if (time > max)
					max = time;

				ids.Add(Data[i].SensorId);
			}

			return Tuple.Create(min, max, ids);
		}

		private static Tuple<DateTime, DateTime, HashSet<int>> ComputeTimePeriod(List<Measurement> Data, int Offset, int Count)
		{
			DateTime min = Data[Offset].Time;
			DateTime max = Data[Offset].Time;
			var ids = new HashSet<int>();

			int end = Offset + Count;
			for (int i = Offset; i < end; i++)
			{
				var time = Data[i].Time;
				if (time < min)
					min = time;
				if (time > max)
					max = time;

				ids.Add(Data[i].SensorId);
			}

			return Tuple.Create(min, max, ids);
		}

		private static Tuple<DateTime, DateTime, HashSet<int>> MetaMinMax(Tuple<DateTime, DateTime, HashSet<int>>[] Tuples, int Offset, int Count)
		{
			DateTime min = Tuples[Offset].Item1;
			DateTime max = Tuples[Offset].Item2;
			var ids = new HashSet<int>();

			int end = Offset + Count;
			for (int i = Offset; i < end; i++)
			{
				var triple = Tuples[i];
				if (triple.Item1 < min)
					min = triple.Item1;
				if (triple.Item2 > max)
					max = triple.Item2;

				ids.UnionWith(triple.Item3);
			}

			return Tuple.Create(min, max, ids);
		}

		private static async Task<V> AggregateParallel<T, V>(
			T Data, int Count, Func<T, int, int, V> Aggregate, 
			Func<V[], int, int, V> MetaAggregate)
		{
			const int TaskSize = 1 << 15;

			if (Count <= TaskSize)
				// Don't parallelize if we don't have to.
				return Aggregate(Data, 0, Count);

			// Okay, so we'll parallelize. Do this
			// by creating an appropriate number of tasks.
			int taskCount = Count / TaskSize;
			int rem = Count % TaskSize;
			var tasks = new Task<V>[taskCount + (rem > 0 ? 1 : 0)];
			for (int i = 0; i < taskCount; i++)
			{
				int startIndex = i * TaskSize;
				tasks[i] = Task.Run(() => 
					Aggregate(Data, startIndex, TaskSize));
			}
			if (rem > 0)
			{
				int startIndex = taskCount * TaskSize;
				tasks[taskCount] = Task.Run(() => 
					Aggregate(Data, startIndex, rem));
			}

			// Wait until all tasks have completed.
			var resultArr = await Task.WhenAll(tasks);
			// Finally, call this function recursively.
			return await AggregateParallel(resultArr, resultArr.Length, MetaAggregate, MetaAggregate);
		}

		/// <summary>
		/// Tries to compute a time period that encompasses the given 
		/// sequence of measurements, i.e. the period during which 
		/// caches may have to be invalidated if the sequence of
		/// measurements is inserted into the database.
		/// Additionally, the set of all sensor identifiers is
		/// determined. 
		/// If this is infeasible, then 'null' is returned. 
		/// If it is feasible, then the computation
		/// may be performed in parallel.
		/// </summary>
		public static Task<Tuple<DateTime, DateTime, HashSet<int>>> ComputeConflictPeriodAsync(
			IEnumerable<Measurement> Data)
		{
			if (Data is Measurement[])
			{
				var arr = (Measurement[])Data;
				return AggregateParallel(arr, arr.Length, ComputeTimePeriod, MetaMinMax);
			}
			else if (Data is List<Measurement>)
			{
				var list = (List<Measurement>)Data;
				return AggregateParallel(list, list.Count, ComputeTimePeriod, MetaMinMax);
			}
			else
			{
				// Perfectly possible to compute the time for this case, 
				// but probably infeasible.
				return Task.FromResult<Tuple<DateTime, DateTime, HashSet<int>>>(null);
			}
		}
	}
}

