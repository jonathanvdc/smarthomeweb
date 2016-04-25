using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartHomeWeb.Model
{
    public class NewsfeedPost
    {
        public Person Sender;
        public string Message;

        public NewsfeedPost(Person sender, string message)
        {
            Sender = sender;
            Message = message;
        }
    }
}
