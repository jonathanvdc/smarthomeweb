using System;

namespace SmartHomeWeb.Model
{
    public class WallPost
    {
        public int Id;
        public Person Source;
        public Person Destination;
        public string Message;
        public Graph Image;
        public WallPost(int id, Person src, Person dest, string message, Graph image = null)
        {
            Id = id;
            Source = src;
            Destination = dest;
            Message = message;
            Image = image;
        }

    }
}
