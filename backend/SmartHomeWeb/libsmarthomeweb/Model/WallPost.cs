using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartHomeWeb.Model
{
    public class WallPost
    {
        public string UserName;
        public string Message;

        public WallPost(string userName, string message)
        {
            UserName = userName;
            Message = message;
        }
    }
}
