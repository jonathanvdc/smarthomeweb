using System.Collections.Generic;

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
