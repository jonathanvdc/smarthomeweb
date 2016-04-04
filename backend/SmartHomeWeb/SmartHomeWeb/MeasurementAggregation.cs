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

            var dataPoints = array.Select(m => m.MeasuredData).Where(m => m.HasValue).Select(m => m.Value);
            double? aggrData = dataPoints.Any() ? AggregateData(dataPoints) : (double?)null;

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
            return Aggregate(Measurements, SensorId, Time, Median);
        }

        /// <summary>
        /// Computes the median of the given sequence of double-precision
        /// floating-point values.
        /// </summary>
        public static double Median(IEnumerable<double> data)
        {
            var array = data.OrderBy(d => d).ToArray();
            var c = array.Length;
            return c % 2 == 0 ? (array[c / 2 - 1] + array[c / 2]) / 2 : array[c / 2];
        }
    }
}