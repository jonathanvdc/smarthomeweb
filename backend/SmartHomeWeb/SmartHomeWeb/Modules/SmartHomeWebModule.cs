using System;
using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Security;
using SmartHomeWeb.Model;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Threading;
using System.Threading.Tasks;

namespace SmartHomeWeb.Modules
{
    public class SmartHomeWebModule : NancyModule
    {
        public SmartHomeWebModule(IFindUserMapper userMapper)
        {
            // StaticConfiguration.EnableHeadRouting = true;
            
            Get["/"] = parameters => View["home.cshtml"];

            // Pages for individual tables
            Get["/person", true] = async (parameters, ct) =>
            {
                var persons = await DataConnection.Ask(x => x.GetPersonsAsync());
                return View["person.cshtml", persons];
            };

            Get["/person={username}", true] = GetProfile;

            Post["/person={username}", true] = async (parameters, ct) =>
            {
                this.RequiresAuthentication();
                ViewBag.Error = "";
                ViewBag.Success = "";

                if (!Context.CurrentUser.IsAuthenticated())
                {
                    ViewBag.Error = "You must log in to add friends.";
                }
                else
                {
                    using (var dc = await DataConnection.CreateAsync())
                    {
                        var sender = await dc.GetPersonByUsernameAsync(Context.CurrentUser.UserName);
                        if (sender == null)
                        {
                            ViewBag.Error = "I couldn't find your username in the database...?";
                        }
                        else
                        {
                            await Console.Out.WriteLineAsync((string)Request.Form["friendname"]);
                            var recipient = await dc.GetPersonByUsernameAsync((string)Request.Form["friendname"]);
                            if (recipient == null)
                            {
                                ViewBag.Error = "That person doesn't exist.";
                            }
                            else
                            {
                                await dc.InsertFriendsPairAsync(new PersonPair(sender.Guid, recipient.Guid));
                                ViewBag.Success = "Friend request sent!";
                            }
                        }
                    }
                }

                return await GetProfile(parameters, ct);
            };
            
            Get["/location", true] = async (parameters, ct) =>
            {
                var locations = await DataConnection.Ask(x => x.GetLocationsAsync());
                return View["locations.cshtml", locations];
            };
            Get["/message", true] = GetMessage;

            Post["/message", true] = async (parameters, ct) =>
            {
                ViewBag.Error = "";
                ViewBag.Success = "";

                using (var dc = await DataConnection.CreateAsync())
                {
                    if (!Context.CurrentUser.IsAuthenticated())
                    {
                        ViewBag.Error = "You must log in to send messages.";
                    }
                    else
                    {
                        var sender = await dc.GetPersonByUsernameAsync(Context.CurrentUser.UserName);
                        if (sender == null)
                        {
                            ViewBag.Error = "I couldn't find your username in the database...?";
                        }
                        else
                        {
                            await Console.Out.WriteLineAsync((string)Request.Form["messagename"]);
                            var recipient = await dc.GetPersonByUsernameAsync((string)Request.Form["messagename"]);
                            if (recipient == null)
                            {
                                ViewBag.Error = "That person doesn't exist.";
                            }
                            else
                            {
                                var messageData = new MessageData(sender.Guid, recipient.Guid, Request.Form["messagebody"]);
                                await dc.InsertMessageAsync(messageData);
                                ViewBag.Success = "Message sent!";
                            }
                        }
                    }
                }

                return await GetMessage(parameters, ct);
            };

            Get["/friends", true] = GetFriends;

            Post["/friends", true] = async (parameters, ct) =>
            {
                this.RequiresAuthentication();
                ViewBag.Error = "";
                ViewBag.Success = "";

                if (!Context.CurrentUser.IsAuthenticated())
                {
                    ViewBag.Error = "You must log in to add friends.";
                }
                else
                {
                    using (var dc = await DataConnection.CreateAsync())
                    {
                        var sender = await dc.GetPersonByUsernameAsync(Context.CurrentUser.UserName);
                        if (sender == null)
                        {
							ViewBag.Error = TextResources.UserNameNotFoundError;
                        }
                        else
                        {
                            await Console.Out.WriteLineAsync((string)Request.Form["friendname"]);
                            var recipient = await dc.GetPersonByUsernameAsync((string)Request.Form["friendname"]);
                            if (recipient == null)
                            {
                                ViewBag.Error = "That person doesn't exist.";
                            }
                            else
                            {
                                await dc.InsertFriendsPairAsync(new PersonPair(sender.Guid, recipient.Guid));
                                ViewBag.Success = "Friend request sent!";
                            }
                        }
                    }
                }

                return await GetFriends(parameters, ct);
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
                    return this.LoginAndRedirect(user.Guid, DateTime.Now.AddYears(1), "/");
                }
                else
                {
                    return Response.AsRedirect("/nopass");
                }
            };
            Get["/logout"] = parameter => this.Logout("/");

            Get["/nopass"] = parameter => NotAuthorizedPage;

            Get["/graphing", true] = async (parameters, ct) =>
            {
                var measurements = await DataConnection.Ask(x => x.GetMeasurementsAsync());
                return View["graph.cshtml", measurements];
            };

			Get["/add-location", true] = GetAddLocation;

			Post["/add-location", true] = async (parameters, ct) =>
			{
				this.RequiresAuthentication();
				ViewBag.Error = "";
				ViewBag.Success = "";

				if (!Context.CurrentUser.IsAuthenticated())
				{
					ViewBag.Error = TextResources.AddLocationNotAuthenticatedText;
				}
				else
				{
					// DataConnection.Ask is used here, because it conveniently wraps everything in
					// a single transaction.
					await DataConnection.Ask(async dc =>
					{
						var sender = await dc.GetPersonByUsernameAsync(Context.CurrentUser.UserName);
						if (sender == null)
						{
							ViewBag.Error = TextResources.UserNameNotFoundError;
						}
						else
						{
							// Retrieve the requested location name, and trim whitespace on both sides.
							string name = ((string)Request.Form["locationname"]).Trim();
							if (string.IsNullOrWhiteSpace(name))
							{
								// Don't create locations with empty names.
								ViewBag.Error = TextResources.WhitespaceNameError;
							}
							else
							{
								await Console.Out.WriteLineAsync(name);
								var loc = await dc.GetLocationByNameAsync(name);
								if (loc != null)
								{
									ViewBag.Error = string.Format(TextResources.LocationAlreadyExistsError, name);
								}
								else
								{
									await dc.InsertLocationAsync(new LocationData(name, sender.Guid));
									var newLoc = await dc.GetLocationByNameAsync(name);
									await dc.InsertHasLocationPairAsync(new PersonLocationPair(sender.Guid, newLoc.Id));
									ViewBag.Success = TextResources.AddedLocationMessage;
								}
							}
						}
					});
				}

				return await GetAddLocation(parameters, ct);
			};

            Get["/add-has-location", true] = async (parameters, ct) =>
            {
                this.RequiresAuthentication();

                // First, acquire all locations.
                var newLocations = new HashSet<Location>(await DataConnection.Ask(dc => dc.GetLocationsAsync()));
                // Then remove all locations that were already assigned to the
                // person.
                newLocations.ExceptWith(await DataConnection.Ask(dc => dc.GetLocationsForPersonAsync(((UserIdentity)Context.CurrentUser).Guid)));
                return View["add-has-location.cshtml", newLocations];
            };

			Get["/add-person", true] = GetAddPerson;

            Get["/mydata", true] = async (parameters, ct) =>
            {
                this.RequiresAuthentication();
                var locations = await DataConnection.Ask(x => x.GetLocationsForPersonAsync(((UserIdentity)Context.CurrentUser).Guid));
                var locationsWithSensors = new List<LocationWithSensors>();

                foreach (var location in locations) {
                    var sensors = await DataConnection.Ask(x => x.GetSensorsAtLocation(location));
                    locationsWithSensors.Add(new LocationWithSensors(location, sensors.ToList()));
                }

                // TODO VIEWBAG
                // TODO write a query for this, too?
                var usernameMessageTuples = new List<Tuple<string, string>>();

                using (var dc = await DataConnection.CreateAsync())
                {
                    var messages = await dc.GetMessagesAsync();
                    foreach (var m in messages)
                    {
                        var recipient = await dc.GetPersonByGuidAsync(m.Data.RecipientGuid);
                        if (recipient.Data.UserName == Context.CurrentUser.UserName)
                        {
                            var sender = await dc.GetPersonByGuidAsync(m.Data.SenderGuid);
                            usernameMessageTuples.Add(Tuple.Create(sender.Data.UserName, m.Data.Message));
                        }
                    }
                }

                ViewBag.Messages = usernameMessageTuples;
                return View["mydata.cshtml", locationsWithSensors];
            };
        }

		private async Task<dynamic> GetAddLocation(dynamic parameters, CancellationToken ct)
		{
			this.RequiresAuthentication();

			return View["add-location.cshtml"];
		}

		private async Task<dynamic> GetAddPerson(dynamic parameters, CancellationToken ct)
		{
			return View["add-person.cshtml"];
		}

        private async Task<dynamic> GetFriends(dynamic parameters, CancellationToken ct)
        {
            this.RequiresAuthentication();
            var friends = await DataConnection.Ask(x => x.GetFriendsAsync(((UserIdentity)Context.CurrentUser).Guid));
            return View["friends.cshtml", friends];
        }

        private async Task<dynamic> GetMessage(dynamic parameters, CancellationToken ct)
        {
            var messages = await DataConnection.Ask(x => x.GetMessagesAsync());
            return View["message.cshtml", messages];
        }

        private async Task<dynamic> GetProfile(dynamic parameters, CancellationToken ct)
        {
            using (var dc = await DataConnection.CreateAsync())
            {
                Person person = await dc.GetPersonByUsernameAsync(parameters.username);
                FriendsState state = 0;
                if (Context.CurrentUser.IsAuthenticated())
                {
                    Person currentUser = await dc.GetPersonByUsernameAsync(Context.CurrentUser.UserName);
					state = await dc.GetFriendsState(currentUser.Guid, person.Guid);
                }
                Tuple<Person, FriendsState> tuple = Tuple.Create(person, state);
                return View["profile.cshtml", tuple];
            }
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