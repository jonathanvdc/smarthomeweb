using System;
using System.Threading;
using System.Threading.Tasks;
using Nancy;
using Newtonsoft.Json;
using System.IO;

namespace SmartHomeWeb.Modules.API
{

    public abstract class ApiModule : NancyModule
    {
        protected ApiModule(string modulePath) : base(modulePath) { }

        // Operation types:
        //
        //   ApiGet, ApiPost : (Path, (Params, Connection) → Task<T>) → Handler

        /// <summary>
        /// Turn an operation on a parameter list and a data connection (which
        /// queries a value of type <typeparamref name="T"/> from the database)
        /// into an async API route handler that performs this operation and
        /// responds with query result serialized as a JSON object.
        /// </summary>
        /// <typeparam name="T">The query's result type.</typeparam>
        /// <param name="operation">The operation to wrap around.</param>
        /// <returns>A route handler.</returns>
        public static Func<dynamic, CancellationToken, Task<dynamic>> Ask<T>(
            Func<dynamic, DataConnection, Task<T>> operation)
        {
            return async (parameters, ct) =>
            {
                T result = await DataConnection.Ask<T>(dc => operation(parameters, dc));
                var json = JsonConvert.SerializeObject(result);

                var statusCode = result == null
                    ? HttpStatusCode.NotFound
                    : HttpStatusCode.Accepted;

                Response response = json;
                response.ContentType = "application/json";
                response.StatusCode = statusCode;
                return response;
            };
        }

        /// <summary>
        /// Register an API GET operation: <code>ApiGet(path, f)</code> is
        /// short for <code>Get[path, true] = Ask(f)</code>.
        /// </summary>
        /// <typeparam name="T">The query's result type.</typeparam>
        /// <param name="path">The route path to register at.</param>
        /// <param name="operation">The operation to wrap around.</param>
        protected void ApiGet<T>(string path, Func<dynamic, DataConnection, Task<T>> operation)
        {
            Get[path, true] = Ask(operation);
        }

        protected Func<dynamic, CancellationToken, Task<dynamic>> Recieve<T>(
            Func<dynamic, T, DataConnection, Task> operation)
        {
            return async (arg, ct) =>
            {
                using (var textReader = new StreamReader(Request.Body))
                {
                    string data = await textReader.ReadToEndAsync();
                    var item = JsonConvert.DeserializeObject<T>(data);
                    await DataConnection.Ask(dc => operation(arg, item, dc));
                    return HttpStatusCode.Created;
                }
            };
        }

        protected void ApiPost<T>(
            string path, Func<dynamic, T, DataConnection, Task> operation)
        {
            Post[path, true] = Recieve<T>(operation);
        }
    }
}
