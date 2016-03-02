using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartHomeWeb.Modules.API
{
    public class ApiPersonsModule : Nancy.NancyModule
    {
        public ApiPersonsModule() : base("api/persons")
        {
            Get["/"] = _ => Nancy.HttpStatusCode.NotImplemented;
        }
    }
}
