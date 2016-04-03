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
        /// Return a measurement that represents an average of all
        /// measurements in the input.
        /// </summary>
        /// <param name="measurements">A list of measurements.</param>
        /// <param name="time">A time to tag the aggregated method with.</param>
        /// <returns>A new measurement, created by taking the median of all
        /// values of the input measurements, concatenating all their notes,
        /// and combining that data with the given DateTime.</returns>
        public static Measurement Average(IEnumerable<Measurement> measurements, DateTime time)
        {
            var array = measurements as Measurement[] ?? measurements.ToArray();
            if (array.Length == 0)
                throw new ArgumentException("The given list of measurements was empty.");

            double median = Median(array.Select(m => m.MeasuredData).OrderBy(d => d));

            StringBuilder notesBuilder = new StringBuilder();
            foreach (var measurement in array)
                notesBuilder.Append(measurement.Notes);

            var sensorId = array.First().SensorId;
            return new Measurement(sensorId, time, median, notesBuilder.ToString());
        }

        private static double Median(IOrderedEnumerable<double> orderedEnumerable)
        {
            var array = orderedEnumerable.ToArray();
            var c = array.Length;
            return c % 2 == 0 ? (array[c / 2 - 1] + array[c / 2]) / 2 : array[c / 2];
        }
    }
}