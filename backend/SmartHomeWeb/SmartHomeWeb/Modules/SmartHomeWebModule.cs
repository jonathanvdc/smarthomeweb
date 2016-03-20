using System;
using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Security;

namespace SmartHomeWeb.Modules
{
    public class SmartHomeWebModule : NancyModule
    {
        public SmartHomeWebModule(IFindUserMapper userMapper)
        {
            // StaticConfiguration.EnableHeadRouting = true;
            
            Get["/"] = parameters =>
            {
                string userName = Context.CurrentUser == null ? "nobody" : Context.CurrentUser.UserName;
                Console.WriteLine($"Logged in as {userName}");
                return View["home.cshtml"];
            };

            // Pages for individual tables
            Get["/person", true] = async (parameters, ct) =>
            {
                var persons = await DataConnection.Ask(x => x.GetPersonsAsync());
                return View["person.cshtml", persons];
            };
            Get["/location", true] = async (parameters, ct) =>
            {
                var locations = await DataConnection.Ask(x => x.GetLocationsAsync());
                return View["locations.cshtml", locations];
            };
            Get["/message", true] = async (parameters, ct) =>
            {
                var messages = await DataConnection.Ask(x => x.GetMessagesAsync());
                return View["message.cshtml", messages];
            };
            Get["/sensor", true] = async (parameters, ct) =>
            {
                var sensors = await DataConnection.Ask(x => x.GetSensorsAsync());
                return View["sensor.cshtml", sensors];
            };
            Get["/measurement", true] = async (parameters, ct) =>
            {
                var measurements = await DataConnection.Ask(x => x.GetMeasurementsAsync());
                return View["measurement.cshtml", measurements];
            };

            Get["/login"] = _ => View["login.cshtml"];

            Post["/login"] = parameter =>
            {
                string name = Request.Form.username;
                string pass = Request.Form.password;
                UserIdentity user;

                if (userMapper.FindUser(name, pass, out user))
                {
                    Console.WriteLine("Found user: " + user.Guid);
                    return this.LoginAndRedirect(user.Guid, DateTime.Now.AddYears(1), "/");
                }
                else
                {
                    return Response.AsRedirect("/nopass");
                }
            };
            Get["/logout"] = parameter => ComingSoonPage;
            Get["/nopass"] = parameter => {
                Console.WriteLine("nopass");
                return NotAuthorizedPage;
            };

            Get["/graphing", true] = async (parameters, ct) =>
            {
                var measurements = await DataConnection.Ask(x => x.GetMeasurementsAsync());
                return View["graph.cshtml", measurements];
            };


            Get["/mydata", true] = async (parameters, ct) =>
            {
                this.RequiresAuthentication();
                var locations = await DataConnection.Ask(x => x.GetLocationsForPersonAsync(((UserIdentity)Context.CurrentUser).Guid));
                var locationlist = new System.Collections.Generic.List<Model.Locationextended>();

                foreach (var pair in locations) {
                    var sensorsinlocations = await DataConnection.Ask(x => x.GetSensorsAtLocation(pair));
                    var locest = new Model.Locationextended(pair);

                    foreach (var sensor in sensorsinlocations)
                    {
                        var sensorex = new Model.Sensorextended(sensor);
                        var measurements = await DataConnection.Ask(x => x.GetMeasurementsFromSensorAsync(sensor));
                        foreach (var measurement in measurements)
                        {
                            sensorex.addmeasurement(measurement);
                        }
                        locest.addsensor(sensorex);
                    }
                    locationlist.Add(locest);
                };
                return View["mydata.cshtml", locationlist];
            };
        }
        public static string ErrorPage => @"
                <html>
                    <body>
                        <h1>Something went horribly, horribly wrong. Our code monkeys are working on the problem.</h1>
                    </body>
                </html>";

        public static string NotAuthorizedPage => @"
                <html>
                    <body>
                        <h1>You shall not pass.</h1>
                    </body>
                </html>";

        public static string ComingSoonPage => @"
                <html>
                    <body>
                        <h1>Not implemented yet, our engineers are working very hard to provide this page for you.</h1>
                    </body>
                </html>";

        public static string EmptyPage => "";
    }    
}