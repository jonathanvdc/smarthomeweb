using System;
using System.Linq;
using System.Threading.Tasks;

namespace SmartHomeWeb.Modules.API
{
    public class ApiReportModule : ApiModule
    {
        public ApiReportModule() : base("api/report")
        {
            ApiGet("/{numDays:int}/{guids}", async (p, dc) =>
            {
                int numDays = p["numDays"];
                var guids = ((string) p["guids"]).Split().Select(Guid.Parse);

                var start = DateTime.Today.AddDays(-numDays);

                return (await Task.WhenAll(guids.Select(async guid => new {
                    Key = guid,
                    Value = await dc.GetTotalDayAveragesAsync(guid, start, numDays)
                }))).ToDictionary(x => x.Key, x => x.Value);
            });
        }
    }
}
