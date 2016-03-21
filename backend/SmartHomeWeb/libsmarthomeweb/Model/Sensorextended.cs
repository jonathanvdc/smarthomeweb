using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartHomeWeb.Model
{
    public class SensorExtended
    {
        public List<Measurement> Measurements { get; private set; }

        public SensorExtended(Sensor sensor)
        {
            this.Sensor = sensor;
            this.Measurements = new List<Measurement>();
        }

        public void AddMeasurement(Measurement measurement)
        {
            this.Measurements.Add(measurement);
        }

        public Sensor Sensor { get; private set; }


    }
}