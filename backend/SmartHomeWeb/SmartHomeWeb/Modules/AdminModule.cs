using Nancy;
using Nancy.Security;

namespace SmartHomeWeb.Modules
{
    public class AdminModule : NancyModule
    {
        public AdminModule() : base("/admin")
        {
            this.RequiresClaims("admin");

            Get["person"] = _ => "Person admin page";
            Get["sensor"] = _ => "Sensor admin page";
            Get["location"] = _ => "Location admin page";
        }
    }
}