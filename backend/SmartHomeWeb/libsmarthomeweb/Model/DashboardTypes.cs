using System;
using System.Collections.Generic;

namespace SmartHomeWeb.Model
{
    public class DashboardType : Tuple<List<Tuple<Location, IEnumerable<string>, List<Tuple<Sensor, IEnumerable<string>>>>>, string>
    {
        public DashboardType(List<Tuple<Location, IEnumerable<string>, List<Tuple<Sensor, IEnumerable<string>>>>> item1, string item2) : base(item1, item2)
        {
        }
    }
}
