using System.Collections.Generic;
using SmartHomeWeb.Model;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Nancy;
using Nancy.Extensions;
using Nancy.ModelBinding;
using Nancy.Security;
using Nancy.ViewEngines.Razor;
using Newtonsoft.Json;

namespace SmartHomeWeb.Modules.API
{
    public class ApiGraphModule : ApiModule
    {
        public ApiGraphModule() : base("api/graphs")
        {
            ApiGet<Graph>("/{userguid}/{graphname}", (p, dc) =>
            {
                this.RequiresAuthentication();

                var userGuid = Guid.Parse(p["userguid"]);
                // Only return the graph if the current user is the user who saved the graph, saved graphs are not public.
                return ((UserIdentity)Context.CurrentUser).Guid != userGuid 
                    ? null 
                    : dc.GetGraphByOwnerAndNameAsync(userGuid, p["graphname"]);
            });
            ApiPost<GraphData, object>("/", async (_, g, dc) =>
            {
                this.RequiresAuthentication();
                if ((await dc.GetPersonByUsernameAsync(Context.CurrentUser.UserName)).GuidString == g.OwnerGuidString)
                    // Check regex match and username correctness, if it's correct, insert graph to DB, else fail silently (Server side debug message - client side silence)
                    await dc.InsertGraphAsync(g);
                else 
                    Console.WriteLine("Graph was submitted, but not saved due to reasons. (incorrect username)");
            });
        }
    }
}
