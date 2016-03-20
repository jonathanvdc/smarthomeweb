using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartHomeWeb.Model
{
    public class Locationextended
    {
        public List<Sensorextended> sensoren { get; private set; }

        public Locationextended(Location location)
        {
            this.location = location;
            this.sensoren = new List<Sensorextended>();
        }

        public void addsensor(Sensorextended sensor)
        {
            this.sensoren.Add(sensor);
        }

        public Location location { get; private set; }


    }
}
