using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartHomeWeb.Model
{
    public class LocationWithSensors
    {
        public LocationWithSensors(Location location, List<Sensor> sensors)
        {
            Location = location;
            Sensors = sensors;
        }

        public Location Location { get; private set; }
        public List<Sensor> Sensors { get; private set; }
    }
}
