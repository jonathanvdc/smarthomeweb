using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SmartHomeWeb.Model;

namespace SmartHomeWeb.Modules.API
{
    public class ApiPersonsModule : ApiModule
    {
        public ApiPersonsModule() : base("api/persons")
        {
            ApiGet("/", (_, dc) => dc.GetPersonsAsync());
            ApiGet("/{id}/", (p, dc) => dc.GetPersonByIdAsync((int) p["id"]));

            Post["/", true] = async (_, ct) =>
            {
                using (var textReader = new StreamReader(Request.Body))
                {
                    string data = await textReader.ReadToEndAsync();
                    var items = JsonConvert.DeserializeObject<List<PersonData>>(data);
                    await DataConnection.Ask(dc => dc.InsertPersonAsync(items));
                    return Nancy.HttpStatusCode.Created;
                }
            };
        }
    }
}
