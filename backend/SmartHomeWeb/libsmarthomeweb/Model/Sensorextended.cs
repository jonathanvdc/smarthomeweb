using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartHomeWeb.Model
{
    public class Sensorextended
    {
        public List<Measurement> sensoren { get; private set; }

        public Sensorextended(Sensor sensor)
        {
            this.sensor = sensor;
            this.sensoren = new List<Measurement>();
        }

        public void addmeasurement(Measurement measurement)
        {
            this.sensoren.Add(measurement);
        }

        public Sensor sensor { get; private set; }


    }
}