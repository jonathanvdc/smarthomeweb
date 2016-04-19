using System;

namespace SmartHomeWeb.Model
{
    public class WallPost
    {
        public string SourceUserName;
        public string DestinationUserName;
        public string Message;

        public WallPost(string src, string dest, string message)
        {
            SourceUserName = src;
            DestinationUserName = dest;
            Message = message;
        }

    }
}
