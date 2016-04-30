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
            ApiGet<Graph>("/{username}/{graphname}", (p, dc) =>
            {
                this.RequiresAuthentication();

                return Context.CurrentUser.UserName != p.username ?  //only return the graph if the current user is the user who saved the graph, saved graphs are not public.
                    null : 
                    dc.GetGraphByOwnerAndNameAsync(p["username"], p["graphname"]);
            });
            ApiPost<Graph, object>("/", async (_, g, dc) =>
            {
                this.RequiresAuthentication();
                const string regex = @"^data:image\/png;base64,.*$";
                var match = Regex.Match(g.GraphURI, regex, RegexOptions.None);
                if ((await dc.GetPersonByUsernameAsync(Context.CurrentUser.UserName)).GuidString == g.Owner
                    && match.Success && match.Captures[0].Value == g.GraphURI)
                //Check regex match and username correctness, if it's correct, insert graph to DB, else fail silently (Server side debug message - client side silence)
                        await dc.InsertGraphAsync(g.GraphURI, g.Owner, g.GraphName);
                else Console.WriteLine("Graph was submitted, but not saved due to reasons.\r\n(Regex mismatch on image data or incorrect username)");
            });
        }
    }
}
