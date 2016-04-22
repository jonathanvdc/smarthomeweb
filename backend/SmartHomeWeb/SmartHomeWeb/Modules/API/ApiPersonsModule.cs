using System.Collections.Generic;
using System.IO;
using Nancy;
using Newtonsoft.Json;
using SmartHomeWeb.Model;
using System;

namespace SmartHomeWeb.Modules.API
{
    public class ApiPersonsModule : ApiModule
    {
        public ApiPersonsModule() : base("api/persons")
        {
            ApiGet("/", (_, dc) => dc.GetPersonsAsync());
            ApiGet("/{g:guid}/", (p, dc) => dc.GetPersonByGuidAsync((Guid)p["g"]));
            ApiGet("/search/{search}", (p, dc) => dc.GetPersonsBySearchString((string) p["search"]));

            ApiPost<List<PersonData>, object>("/", (_, items, dc) => dc.InsertPersonAsync(items));
            
            ApiDelete("/{g:guid}/", (p, dc) => dc.DeletePersonAsync((Guid)p["g"]));
        }
    }
}
