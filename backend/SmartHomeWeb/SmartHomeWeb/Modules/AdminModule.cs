using Nancy;
using Nancy.Security;

namespace SmartHomeWeb.Modules
{
    public class AdminModule : NancyModule
    {
        public AdminModule() : base("/admin")
        {
            this.RequiresClaims("admin");

            Get["/person", true] = async (parameters, ct) =>
            {
                var persons = await DataConnection.Ask(x => x.GetPersonsAsync());
                return View["admin-person.cshtml", persons];
            };

            Get["/sensor", true] = async (parameters, ct) =>
            {
                var items = await DataConnection.Ask(dc => dc.GetSensorTagsPairsAsync());
                return View["admin-sensor.cshtml", items];
            };

            Get["/location", true] = async (parameters, ct) =>
            {
                var locations = await DataConnection.Ask(x => x.GetLocationsAsync());
                return View["admin-location.cshtml", locations];
            };
        }
    }
}