using System;
using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Security;
using SmartHomeWeb.Model;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using Nancy.Extensions;
using Nancy.Session;

namespace SmartHomeWeb.Modules
{
    public class SmartHomeWebModule : NancyModule
    {
        private IFindUserMapper UserMapper;

        public SmartHomeWebModule(IFindUserMapper userMapper)
        {
            UserMapper = userMapper;

            // StaticConfiguration.EnableHeadRouting = true;

            Before += ctx =>
            {
                TextResources.Culture = (CultureInfo)this.Request.Session["CurrentCulture"];
                return null;
            };

            Get["/"] = parameters =>
                Context.CurrentUser.IsAuthenticated()
                    ? Response.AsRedirect("/newsfeed")
                    : (dynamic)View["home.cshtml"];

            // Pages for individual tables
            Get["/person", true] = async (parameters, ct) =>
            {
                var persons = await DataConnection.Ask(x => x.GetPersonsAsync());
                return View["person.cshtml", persons];
            };

            Get["/person/{username}", true] = GetProfile;

            Post["/person/{username}", true] = async (parameters, ct) =>
            {
                this.RequiresAuthentication();
                await FriendRequest(FormHelpers.GetString(Request.Form, "friendname"));
                return await GetProfile(parameters, ct);
            };
            
            Get["/person/{username}/wall", true] = GetWall;

            Post["/person/{username}/wall", true] = PostWall;

            Get["/location", true] = async (parameters, ct) =>
            {
                var locations = await DataConnection.Ask(x => x.GetLocationsAsync());
                return View["locations.cshtml", locations];
            };
            Get["/message", true] = GetMessage;

            Post["/message", true] = PostMessage;

            Get["/friends", true] = GetFriends;

            Post["/friends", true] = PostFriends;

            Post["/friend-request", true] = async (parameters, ct) =>
            {
                this.RequiresAuthentication();
                await FriendRequest(FormHelpers.GetString(Request.Form, "friendname"));

                // TODO: this is a hack. Maybe make which success message to display a parameter of FriendRequest().
                if (!string.IsNullOrEmpty(ViewBag.Success))
                    ViewBag.Success = TextResources.FriendRequestAccepted;

                return await GetFriends(parameters, ct);
            };

            Get["/sensor", true] = async (parameters, ct) =>
            {
                IEnumerable<Tuple<Sensor, IEnumerable<string>>> items;
                using (var dc = await DataConnection.CreateAsync())
                {
                    items = await dc.GetSensorTagsPairsAsync();
                }
                return View["sensor.cshtml", items];
            };
            
            Get["/add-tag/{id?}", true] = GetAddTag;

            Post["/add-tag/{id?}", true] = PostAddTag;

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

                return userMapper.FindUser(name, pass, out user)
                    ? this.LoginAndRedirect(user.Guid, DateTime.Now.AddYears(1))
                    : Response.AsRedirect("/nopass");
            };
            Get["/logout"] = parameter => this.Logout("/");

            Get["/nopass"] = parameter => NotAuthorizedPage;

            Get["/graphing", true] = async (parameters, ct) =>
            {
                var measurements = await DataConnection.Ask(x => x.GetMeasurementsAsync());
                return View["graph.cshtml", measurements];
            };

            //TODO this junk
            Get["/groups", true] = GetGroups;

            Post["/groups", true] = PostGroups;

            Get["/groups/{groupid}", true] = GetGroup;

            //Get["/groups/{groupid}/invite", true] = GetGroupInvite;

            //Post["/groups/{groupid}/invite", true] = PostGroupInvite;

            Get["/add-location", true] = GetAddLocation;

			Post["/add-location", true] = PostAddLocation;

            Get["/add-has-location", true] = GetAddHasLocation;

            Post["/add-has-location", true] = async (parameters, ct) =>
            {
                this.RequiresAuthentication();

                var locationId = int.Parse((string) FormHelpers.GetString(Request.Form, "location"));
                var personLocationPair = new PersonLocationPair(CurrentUserGuid(), locationId);
                await DataConnection.Ask(d => d.InsertHasLocationPairAsync(personLocationPair));

                return await GetAddHasLocation(parameters, ct);
            };

            Get["/add-person", true] = GetAddPerson;

            Post["/add-person", true] = PostAddPerson;

            Get["/newsfeed", true] = GetNewsfeed;

            Get["/dashboard", true] = GetDashboard;

            Get["/set-culture"] = parameters =>
            {
                string lcid = Request.Query["lcid"];
                Request.Session["CurrentCulture"] = CultureInfo.GetCultureInfo(lcid);
                var referrer = Request.Headers.Referrer;
                return Response.AsRedirect(string.IsNullOrWhiteSpace(referrer) ? "/" : referrer);
            };
            
        }
        private async Task<dynamic> GetGroup(dynamic parameters, CancellationToken ct)
        {
            this.RequiresAuthentication();
            Tuple<bool, IEnumerable<Person>, IEnumerable<WallPost>, string> tuple;
            using (var dc = await DataConnection.CreateAsync())
            {
                IEnumerable<Person> memberList = await dc.GetMembersForGroupAsync(parameters.groupid);
                IEnumerable<WallPost> postList = await dc.GetPostsForGroupAsync(parameters.groupid);
                var isMember = false;
                foreach (var m in memberList)
                {
                    if (m.Guid == CurrentUserGuid()) isMember = true;
                }
                tuple = new Tuple<bool, IEnumerable<Person>, IEnumerable<WallPost>, string>(isMember, memberList, postList, (await dc.GetGroupByIdAsync(parameters.groupid)).Name);
            }
            
            return View["groupprofile", tuple];
        }
        private async Task<dynamic> GetWall(dynamic parameters, CancellationToken ct)
        {
            // this.RequiresAuthentication(); // Disabled because the wall should be publicly viewable. We can hide things from users using a DB flag later.
            // Maybe an int 0 = public, 1 = logged in users only, 2 = friends only, 3 = private message
            // Or some other variation thereof. Loads of options!

            // Get all messages addressed to the current user
            IEnumerable<WallPost> posts;
            var signedIn = Context.CurrentUser.IsAuthenticated();
            using (var dc = await DataConnection.CreateAsync())
            {
                Person recipient = await dc.GetPersonByUsernameAsync(parameters.username);
                posts = await dc.GetWallPostsAsync(recipient.Guid);
            }
            var viewstuff = new Tuple<string, IEnumerable<WallPost>, bool>(parameters.username, posts, signedIn);
            return View["wall.cshtml", viewstuff];
        }
        private async Task<dynamic> PostWall(dynamic parameters, CancellationToken ct)
        {
            using (var dc = await DataConnection.CreateAsync())
            {
                var recipient = (await dc.GetPersonByUsernameAsync(parameters.username)).Guid;
                var sender = CurrentUserGuid();
                var message = FormHelpers.GetString(Request.Form, "wallpost");

                if (string.IsNullOrWhiteSpace(message))
                {
                    ViewBag.Error = "Please type a message before submitting!";
                }
                else
                {
                    await dc.InsertMessageAsync(new MessageData(sender, recipient, message));
                }
            }

            return await GetWall(parameters, ct);
        }
        private async Task<dynamic> GetGroups(dynamic parameters, CancellationToken ct)
        {
            this.RequiresAuthentication();
            List<Group> groups;
            using (var dc = await DataConnection.CreateAsync())
            {
                groups = (await dc.GetGroupsForUserAsync(CurrentUserGuid())).ToList();

            }
            
            return View["Group", groups];
        }
        private async Task<dynamic> PostGroups(dynamic parameters, CancellationToken ct)
        {
            this.RequiresAuthentication();

            using (var dc = await DataConnection.CreateAsync())
            {
                var name = FormHelpers.GetString(Request.Form, "NG_name");
                var description = FormHelpers.GetString(Request.Form, "NG_description");
                var members = new List<Person> {await dc.GetPersonByGuidAsync(CurrentUserGuid())};
                var group = new Group(-1, name, description, members);
                await dc.InsertGroupAsync(group);
            }


            return await GetGroups(parameters, ct);
        }
        
        private async Task<dynamic> GetAddHasLocation(dynamic parameters, CancellationToken ct)
        {
            this.RequiresAuthentication();

            // First, acquire all locations.
            var newLocations = new HashSet<Location>(await DataConnection.Ask(dc => dc.GetLocationsAsync()));
            // Then remove all locations that were already assigned to the person.
            newLocations.ExceptWith(await DataConnection.Ask(dc => dc.GetLocationsForPersonAsync(CurrentUserGuid())));
            return View["add-has-location.cshtml", newLocations];
        }

        private Guid CurrentUserGuid()
        {
            return ((UserIdentity) Context.CurrentUser).Guid;
        }

        private async Task<object> GetNewsfeed(dynamic parameters, CancellationToken ct)
        {
            this.RequiresAuthentication();

            var newsfeedPosts = new List<NewsfeedPost>();

            using (var dc = await DataConnection.CreateAsync())
            {
                var messages = await dc.GetMessagesAsync();
                foreach (var m in messages)
                {
                    var recipient = await dc.GetPersonByGuidAsync(m.Data.RecipientGuid);

                    // TODO: write a query for this (all messages for one recipient.)
                    if (recipient.Data.UserName == Context.CurrentUser.UserName)
                    {
                        var sender = await dc.GetPersonByGuidAsync(m.Data.SenderGuid);
                        newsfeedPosts.Add(new NewsfeedPost(sender.Data.UserName, m.Data.Message));
                    }
                }
            }

            return View["newsfeed.cshtml", newsfeedPosts];
        }

        private async Task<object> PostAddPerson(dynamic parameters, CancellationToken ct)
        {
            ViewBag.Success = "";
            ViewBag.Error = "";

            string username = FormHelpers.GetString(Request.Form, "personusername");
            string name = FormHelpers.GetString(Request.Form, "personname");
            string password = FormHelpers.GetRawString(Request.Form, "personpassword");
            string repeatPassword = FormHelpers.GetRawString(Request.Form, "personpasswordrepeat");
            string address = FormHelpers.GetString(Request.Form, "personaddress");
            DateTime? birthdate = FormHelpers.GetDate(Request.Form, "personbirthdate");
            string city = FormHelpers.GetString(Request.Form, "personcity");
            string zipcode = FormHelpers.GetString(Request.Form, "personzipcode");

            if (string.IsNullOrWhiteSpace(username))
            {
                ViewBag.Error = TextResources.EmptyUserNameError;
            }
            else if (string.IsNullOrWhiteSpace(name))
            {
                ViewBag.Error = TextResources.EmptyNameError;
            }
            else if (string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = TextResources.EmptyPasswordError;
            }
            else if (password != repeatPassword)
            {
                ViewBag.Error = TextResources.PasswordMismatchError;
            }
            else if (!birthdate.HasValue)
            {
                ViewBag.Error = TextResources.BadlyFormattedBirthDateError;
            }
            else
            {
                // DataConnection.Ask is used here, because it conveniently wraps everything in
                // a single transaction.
                await DataConnection.Ask(async dc =>
                {
                    var sender = await dc.GetPersonByUsernameAsync(username);
                    if (sender != null)
                    {
                        ViewBag.Error = string.Format(TextResources.PersonAlreadyExistsError, username);
                    }
                    else
                    {
                        var personData = new PersonData(username, password, name, birthdate.Value, address, city, zipcode);
                        // Create the person
                        await dc.InsertPersonAsync(personData);
                        ViewBag.Success = TextResources.AddedPersonMessage;
                    }
                });
            }

            if (string.IsNullOrWhiteSpace((string)ViewBag.Error))
            {
                UserIdentity user;
                UserMapper.FindUser(username, password, out user);
                // Everything went fine. Log in and redirect to the profile page.
                return this.LoginAndRedirect(user.Guid, DateTime.Now.AddYears(1), "/person/" + username);
            }
            else
            {
                return await GetAddPerson(parameters, ct);
            }
        }

        private async Task<dynamic> GetDashboard(dynamic parameters, CancellationToken ct)
        {
            this.RequiresAuthentication();
            var locations = await DataConnection.Ask(x => x.GetLocationsForPersonAsync(CurrentUserGuid()));
            var locationsWithSensors = new List<LocationWithSensors>();

            foreach (var location in locations)
            {
                var sensors = await DataConnection.Ask(x => x.GetSensorsAtLocationAsync(location));
                locationsWithSensors.Add(new LocationWithSensors(location, sensors.ToList()));
            }

            return View["dashboard.cshtml", locationsWithSensors];
        }

        private async Task<dynamic> PostAddLocation(dynamic parameters, CancellationToken ct)
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
                        string name = FormHelpers.GetString(Request.Form, "locationname");
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            // Don't create locations with empty names.
                            ViewBag.Error = TextResources.EmptyNameError;
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
                                await dc.InsertLocationAsync(new LocationData(name, sender.Guid, null));
                                var newLoc = await dc.GetLocationByNameAsync(name);
                                await dc.InsertHasLocationPairAsync(new PersonLocationPair(sender.Guid, newLoc.Id));
                                ViewBag.Success = TextResources.AddedLocationMessage;
                            }
                        }
                    }
                });
            }

            return await GetAddLocation(parameters, ct);
        }

        /// <summary>
        /// Perform a friend request from the current user to a given username.
        /// </summary>
        /// <param name="userName">The username of the friend to send a request to.</param>
        private async Task FriendRequest(string userName)
        {
            ViewBag.Error = "";
            ViewBag.Success = "";

            if (!Context.CurrentUser.IsAuthenticated())
            {
                ViewBag.Error = TextResources.AddFriendNotAuthenticatedText;
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
                        var recipient = await dc.GetPersonByUsernameAsync(userName);
                        if (recipient == null)
                        {
                            ViewBag.Error = TextResources.UserDoesNotExistError;
                        }
                        else
                        {
                            await dc.InsertFriendsPairAsync(new PersonPair(sender.Guid, recipient.Guid));
                            ViewBag.Success = TextResources.FriendRequestSent;
                        }
                    }
                }
            }
        }

        private async Task<dynamic> PostMessage(dynamic parameters, CancellationToken ct)
        {
            ViewBag.Error = "";
            ViewBag.Success = "";

            using (var dc = await DataConnection.CreateAsync())
            {
                if (!Context.CurrentUser.IsAuthenticated())
                {
                    ViewBag.Error = TextResources.SendMessageNotAuthenticatedText;
                }
                else
                {
                    var sender = await dc.GetPersonByUsernameAsync(Context.CurrentUser.UserName);
                    if (sender == null)
                    {
                        ViewBag.Error = TextResources.UserNameNotFoundError;
                    }
                    else
                    {
                        await Console.Out.WriteLineAsync((string) Request.Form["messagename"]);
                        var recipient = await dc.GetPersonByUsernameAsync(FormHelpers.GetString(Request.Form, "messagename"));
                        if (recipient == null)
                        {
                            ViewBag.Error = TextResources.UserDoesNotExistError;
                        }
                        else
                        {
                            var messageData = new MessageData(sender.Guid, recipient.Guid, Request.Form["messagebody"]);
                            await dc.InsertMessageAsync(messageData);
                            ViewBag.Success = TextResources.MessageSent;
                        }
                    }
                }
            }

            return await GetMessage(parameters, ct);
        }

        private async Task<dynamic> PostFriends(dynamic parameters, CancellationToken ct)
        {
            this.RequiresAuthentication();
            ViewBag.Error = "";
            ViewBag.Success = "";

            if (!Context.CurrentUser.IsAuthenticated())
            {
                ViewBag.Error = TextResources.AddFriendNotAuthenticatedText;
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
                        await Console.Out.WriteLineAsync((string) Request.Form["friendname"]);
                        var recipient = await dc.GetPersonByUsernameAsync(FormHelpers.GetString(Request.Form, "friendname"));
                        if (recipient == null)
                        {
                            ViewBag.Error = TextResources.UserDoesNotExistError;
                        }
                        else
                        {
                            await dc.InsertFriendsPairAsync(new PersonPair(sender.Guid, recipient.Guid));
                            ViewBag.Success = TextResources.FriendRequestSent;
                        }
                    }
                }
            }

            return await GetFriends(parameters, ct);
        }

        private async Task<dynamic> PostAddTag(dynamic parameters, CancellationToken ct)
        {
            this.RequiresAuthentication();
            ViewBag.Error = "";
            ViewBag.Success = "";

            if (!Context.CurrentUser.IsAuthenticated())
            {
                ViewBag.Error = TextResources.AddTagNotAuthenticated;
            }
            else
            {
                using (var dc = await DataConnection.CreateAsync())
                {
                    int sensorId = (int)Request.Form["sensor-id"];
                    string tag = FormHelpers.GetString(Request.Form, "tag-name");

                    Console.WriteLine("Tagging Sensor {0} with \"{1}\"", sensorId, tag);

                    var sensor = await dc.GetSensorByIdAsync(sensorId);

                    if (sensor == null)
                    {
                        ViewBag.Error = TextResources.SensorDoesNotExistError;
                    }
                    else
                    {
                        await dc.InsertSensorTagAsync(sensorId, tag);
                        ViewBag.Success = TextResources.TagAdded;
                    }
                }
            }
            return Response.AsRedirect(String.Format("/add-tag/{0}", (string)Request.Form["sensor-id"]));
        }

#pragma warning disable 1998
        private async Task<dynamic> GetAddLocation(dynamic parameters, CancellationToken ct)
		{
			this.RequiresAuthentication();

			return View["add-location.cshtml"];
		}

		private async Task<dynamic> GetAddPerson(dynamic parameters, CancellationToken ct)
		{
			return View["add-person.cshtml"];
		}

        private async Task<dynamic> GetAddTag(dynamic parameters, CancellationToken ct)
        {
            this.RequiresAuthentication();

            var sensors = await DataConnection.Ask(x => x.GetSensorsAsync());

            ViewBag.HighlightedSensorId = parameters.id;
            return View["add-tag.cshtml", sensors];
        }
#pragma warning restore 1998

        private async Task<dynamic> GetFriends(dynamic parameters, CancellationToken ct)
        {
            this.RequiresAuthentication();
            var sent = await DataConnection.Ask(x => x.GetSentFriendRequestsAsync(CurrentUserGuid()));
            var received = await DataConnection.Ask(x => x.GetRecievedFriendRequestsAsync(CurrentUserGuid()));
            var friends = await DataConnection.Ask(x => x.GetFriendsAsync(CurrentUserGuid()));

            var model = new Dictionary<FriendsState, IEnumerable<Person>>
            {
                [FriendsState.FriendRequestSent] = sent,
                [FriendsState.FriendRequestRecieved] = received,
                [FriendsState.Friends] = friends
            };

            return View["friends.cshtml", model];
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

        public static string NotAuthorizedPage => @"
                <html>
                    <body>
                        <h1>You shall not pass.</h1>
                    </body>
                </html>";
    }    
}