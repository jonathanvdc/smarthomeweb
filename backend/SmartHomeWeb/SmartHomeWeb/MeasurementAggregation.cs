using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SmartHomeWeb.Model;

namespace SmartHomeWeb
{
    public static class MeasurementAggregation
    {
		/// <summary>
		/// Quantize the given date-time, for the
		/// given quantum of time.
		/// </summary>
		public static DateTime Quantize(DateTime Time, TimeSpan Quantum)
		{
			long tTicks = Time.Ticks;
			return new DateTime(tTicks - tTicks % Quantum.Ticks, Time.Kind);
		}

		public static DateTime QuantizeHour(DateTime Time)
		{
			return Quantize(Time, TimeSpan.FromHours(1));
		}

		public static DateTime QuantizeDay(DateTime Time)
		{
			return Quantize(Time, TimeSpan.FromDays(1));
		}

		public static DateTime QuantizeMonth(DateTime Time)
		{
			return new DateTime(Time.Year, Time.Month, 1, 0, 0, 0, 0, Time.Kind);
		}

		public static DateTime QuantizeYear(DateTime Time)
		{
			return new DateTime(Time.Year, 1, 1, 0, 0, 0, 0, Time.Kind);
		}

		public static DateTime CeilingHour(DateTime Time)
		{
			long tTicks = Time.Ticks;
			long hTicks = TimeSpan.FromHours(1).Ticks;
			long rem = tTicks % hTicks;
			if (rem == 0)
				return Time;
			else
				return new DateTime(tTicks - rem + hTicks, Time.Kind);
		}

		public static DateTime CeilingDay(DateTime Time)
		{
			if (Time.TimeOfDay == TimeSpan.Zero)
				return Time;
			else
				return QuantizeDay(Time).AddDays(1);
		}

		public static DateTime CeilingMonth(DateTime Time)
		{
			if (Time.Day == 1 && Time.TimeOfDay == TimeSpan.Zero)
				return Time;
			else
				return QuantizeMonth(Time).AddMonths(1);
		}

        /// <summary>
        /// Return a measurement that represents an aggregation of all
        /// measurements in the input.
        /// </summary>
        /// <param name="Measurements">
        /// A list of measurements.
        /// </param>
        /// <param name="SensorId">
        /// A sensor ID to tag the aggregated measurement with.
        /// </param>
        /// <param name="Time">
        /// A time to tag the aggregated measurement with.
        /// </param>
        /// <param name="AggregateData">
        /// A function that aggregates the data in the measurements.
        /// </param>
        /// <returns>A new measurement, created by taking aggregating all
        /// values of the input measurements with the given aggregation
        /// function, concatenating all their notes,
        /// and combining that data with the given DateTime.</returns>
        /// <remarks>If the given sequence of measurements does not
        /// contain any non-null measurements, then a null measurement
        /// is returned.</remarks>
        public static Measurement Aggregate(
            IEnumerable<Measurement> Measurements,
            int SensorId, DateTime Time,
            Func<IEnumerable<double>, double> AggregateData)
        {
            var array = Measurements as Measurement[] ?? Measurements.ToArray();

			var dataPoints = new List<double>(array.Length);
			foreach (var m in array)
			{
				var data = m.MeasuredData;
				if (data.HasValue)
					dataPoints.Add(data.Value);
			}

            double? aggrData = dataPoints.Count != 0 ? AggregateData(dataPoints) : (double?)null;

            var notesBuilder = new StringBuilder();
            foreach (var measurement in array)
                notesBuilder.Append(measurement.Notes);

            return new Measurement(SensorId, Time, aggrData, notesBuilder.ToString());
        }

        /// <summary>
        /// Return a measurement that represents an average of all
        /// measurements in the input.
        /// </summary>
        /// <param name="Measurements">A list of measurements.</param>
        /// <param name="SensorId">A sensor ID to tag the aggregated measurement with.</param>
        /// <param name="Time">A time to tag the aggregated method with.</param>
        /// <returns>A new measurement, created by taking the median of all
        /// values of the input measurements, concatenating all their notes,
        /// and combining that data with the given DateTime.</returns>
        /// <remarks>If the given sequence of measurements does not
        /// contain any non-null measurements, then 'null' is returned.</remarks>
        public static Measurement Aggregate(IEnumerable<Measurement> Measurements, int SensorId, DateTime Time)
        {
            // TODO: Test NonOutlierMean!
            return Aggregate(Measurements, SensorId, Time, NonOutlierMean);
        }

        /// <summary>
        /// Computes the median of the given sequence of double-precision
        /// floating-point values, assuming it is already sorted, and contains
        /// at least one element.
        /// </summary>
        /// <param name="data">The input data, sorted.</param>
        /// <returns>The median of the input data.</returns>
        private static double MedianSorted(IReadOnlyList<double> data)
        {
            var c = data.Count;
            if (c < 1)
                throw new ArgumentException("Median of empty list");
            return c % 2 == 0 ? (data[c / 2 - 1] + data[c / 2]) / 2 : data[c / 2];
        }


        /// <summary>
        /// Computes the median of the given sequence of double-precision
        /// floating-point values, assuming it contains at least one element.
        /// </summary>
        /// <param name="data">The input data.</param>
        /// <returns>The median of the input data.</returns>
        private static double Median(IEnumerable<double> data)
            => MedianSorted(data.OrderBy(d => d).ToArray());

        /// <summary>
        /// Computes the first and third quartiles given sequence of double-
        /// precision floating-point values, assuming it contains at least
        /// one element.
        /// </summary>
        /// <param name="data">The input data.</param>
        /// <returns>A tuple whose Item1 contains the Q1, and whose Item2
        /// contains the Q3 of the input data.</returns>
        private static Tuple<double, double> Quartiles(IEnumerable<double> data)
        {
            var array = data.OrderBy(d => d).ToArray();
            var c = array.Length;
            if (c < 1)
                throw new ArgumentException("Quartiles of empty list");


            // To find Q1 and Q3, we split the array into two pieces. If its
            // length is even, we split it nicely in half:
            //
            //                      right
            //                   ~~~~~~~~~~
            //     [ 1  2  3  4  5  6  7  8 ]
            //       ~~~~~~~~~~
            //          left
            //
            // If its length is odd, both halves get the array's median in it:
            //
            //                       right
            //                   ~~~~~~~~~~~~~
            //     [ 1  2  3  4  5  6  7  8  9 ]
            //       ~~~~~~~~~~~~~
            //           left
            //
            // Then (Q1, Q3) = (Median(left), Median(right)).

            var left  = new ArraySegment<double>(array, 0,     c - c / 2);
            var right = new ArraySegment<double>(array, c / 2, c - c / 2);
            return new Tuple<double, double>(MedianSorted(left), MedianSorted(right));
        }

        /// <summary>
        /// Compute the mean of the data, but disregard any outliers. An
        /// outlier is defined as a measurement below Q1 - 1.5IQR or above
        /// Q3 + 1.5IQR.
        /// </summary>
        /// <param name="data">The input data.</param>
        /// <returns>The non-outlier mean of the input data.</returns>
        private static double NonOutlierMean(IEnumerable<double> data)
        {
            var array = data as double[] ?? data.ToArray();
			if (array.Length < 1)
				throw new ArgumentException("NonOutlierMean of empty list");
			else if (array.Length == 1)
				return array[0];

            var quartiles = Quartiles(array);
            var q1 = quartiles.Item1;
            var q3 = quartiles.Item2;
            const double tol = 0.01;
            double lowerBound = 2.5 * q1 - 1.5 * q3 - tol;
            double upperBound = 2.5 * q3 - 1.5 * q1 + tol;

			// Use an old-fashioned foreach loop to speed 
			// things up here a bit.
			long count = 0;
			double result = 0.0;
			foreach (var x in array)
			{
				if (x >= lowerBound && x <= upperBound)
				{
					result += x;
					count++;
				}
			}
			return result / count;
        }
    }
}
