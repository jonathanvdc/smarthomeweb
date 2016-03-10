namespace SmartHomeWeb.Modules.API
{
    public class ApiLocationsModule : ApiModule
    {
        public ApiLocationsModule() : base("api/locations")
        {
            ApiGet("/", (_, dc) =>
            dc.GetLocationsAsync());
            ApiGet("/{id}/", (p, dc) => dc.GetLocationByIdAsync((int)p["id"]));
        }
    }
}
