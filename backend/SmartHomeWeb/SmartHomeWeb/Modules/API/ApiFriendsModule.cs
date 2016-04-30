using System.Collections.Generic;
using SmartHomeWeb.Model;
using System;

namespace SmartHomeWeb.Modules.API
{
    public class ApiFriendsModule : ApiModule
    {
        public ApiFriendsModule() : base("api/friends")
        {
            ApiGet("/", (p, dc) => dc.GetFriendsAsync());
            ApiGet("/{guid}/", (p, dc) => dc.GetFriendsAsync(new Guid(p["guid"])));
            ApiGet("/requests/recieved/{guid}", (p, dc) => dc.GetRecievedFriendRequestsAsync(new Guid(p["guid"])));
            ApiGet("/requests/recieved/count/{guid}", (p, dc) => dc.GetRecievedFriendRequestsCountAsync(new Guid(p["guid"])));
            ApiGet("/requests/sent/{guid}", (p, dc) => dc.GetSentFriendRequestsAsync(new Guid(p["guid"])));

            ApiPost<List<PersonPair>, object>("/", (_, items, dc) => dc.InsertFriendsPairAsync(items));
        }
    }
}
