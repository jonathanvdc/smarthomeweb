using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartHomeWeb.Model
{
    public class NewsfeedPost
    {
        public string UserName;
        public string Name;
        public string Message;

        public NewsfeedPost(string userName, string name, string message)
        {
            UserName = userName;
            Name = name;
            Message = message;
        }
    }
}
