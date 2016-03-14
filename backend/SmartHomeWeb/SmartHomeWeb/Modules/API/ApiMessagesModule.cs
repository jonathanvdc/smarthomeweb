using System.Collections.Generic;
using System.IO;
using Nancy;
using Newtonsoft.Json;
using SmartHomeWeb.Model;
using System;

namespace SmartHomeWeb.Modules.API
{
    public class ApiMessagesModule : ApiModule
    {
        public ApiMessagesModule() : base("api/messages")
        {
            ApiGet("/", (_, dc) => dc.GetMessagesAsync());
            // TODO: somehow get 
            // ApiGet("/{id}/", (p, dc) => dc.GetMessageByIdAsync((int)p["id"]));

            ApiPost<List<MessageData>, object>("/", (_, items, dc) => dc.InsertMessageAsync(items));
        }
    }
}
