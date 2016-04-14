using System.Collections.Generic;
using SmartHomeWeb.Model;

namespace SmartHomeWeb.Modules.API
{
    public class ApiLocationsModule : ApiModule
    {
        public ApiLocationsModule() : base("api/locations")
        {
            ApiGet("/", (_, dc) => dc.GetLocationsAsync());
            ApiGet("/{id}/", (p, dc) => dc.GetLocationByIdAsync((int)p["id"]));

			// POST API to create new locations.
            ApiPost<List<LocationData>, object>("/", (_, items, dc) => dc.InsertLocationAsync(items));

			// PUT API to update existing locations.
			ApiPut<List<Location>, object>("/", (_, items, dc) => dc.UpdateLocationAsync(items));
			ApiPut<LocationData, dynamic>("/{id}/", (p, item, dc) => dc.UpdateLocationAsync(new Location((int)p["id"], item)));
        }
    }
}
