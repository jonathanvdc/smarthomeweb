using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartHomeWeb.Model
{
    public class LocationExtended
    {
        public List<SensorExtended> Sensors { get; private set; }

        public LocationExtended(Location location)
        {
            this.Location = location;
            this.Sensors = new List<SensorExtended>();
        }

        public void AddSensor(SensorExtended sensor)
        {
            this.Sensors.Add(sensor);
        }

        public Location Location { get; private set; }


    }
}
