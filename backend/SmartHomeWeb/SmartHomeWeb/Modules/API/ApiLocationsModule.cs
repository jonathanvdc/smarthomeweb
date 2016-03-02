using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SmartHomeWeb.Model;

namespace SmartHomeWeb.Modules.API
{
    public class ApiLocationsModule : ApiModule
    {
        public ApiLocationsModule() : base("api/locations")
        {
            ApiGet("/", (_, dc) => dc.GetLocationsAsync());
            ApiGet("/{id}/", (p, dc) => dc.GetLocationByIdAsync((int)p["id"]));
        }
    }
}
