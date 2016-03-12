using System.Collections.Generic;
using System.IO;
using Nancy;
using Newtonsoft.Json;
using SmartHomeWeb.Model;

namespace SmartHomeWeb.Modules.API
{
    public class ApiPersonsModule : ApiModule
    {
        public ApiPersonsModule() : base("api/persons")
        {
            ApiGet("/", (_, dc) => dc.GetPersonsAsync());
            ApiGet("/{id}/", (p, dc) => dc.GetPersonByIdAsync((int) p["id"]));

            ApiPost<List<PersonData>>("/", (_, items, dc) => dc.InsertPersonAsync(items));
        }
    }
}
