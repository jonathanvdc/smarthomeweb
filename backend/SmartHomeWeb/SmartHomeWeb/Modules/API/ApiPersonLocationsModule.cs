using System.Collections.Generic;
using SmartHomeWeb.Model;
using System;

namespace SmartHomeWeb.Modules.API
{
    public class ApiPersonLocationsModule : ApiModule
    {
        public ApiPersonLocationsModule() : base("api/has-location")
        {
            ApiGet("/", (p, dc) => dc.GetHasLocationPairsAsync());
            ApiGet("/locations/{guid}/", (p, dc) => dc.GetLocationsForPersonAsync(new Guid(p["guid"])));
            ApiGet("/persons/{id}/", (p, dc) => dc.GetPersonsAtLocationAsync((int)p["id"]));

            ApiPost<List<PersonLocationPair>, object>("/", (_, items, dc) => dc.InsertHasLocationPairAsync(items));
        }
    }
}
