using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartHomeWeb.Model
{
    public class LocationExtended
    {
        public List<Sensor> Sensors { get; private set; }

        public LocationExtended(Location location)
        {
            this.Location = location;
            this.Sensors = new List<Sensor>();
        }

        public void AddSensor(Sensor sensor)
        {
            this.Sensors.Add(sensor);
        }

        public Location Location { get; private set; }


    }
}
