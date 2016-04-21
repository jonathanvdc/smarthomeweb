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

        public static Func<dynamic, CancellationToken, Task<dynamic>> Ask(
            Func<dynamic, DataConnection, Task> operation)
        {
            return Ask<int>(async (p, dc) => {
                await operation(p, dc); return 0;
            });
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

        protected Func<dynamic, CancellationToken, Task<dynamic>> Recieve<T, TParam>(
            Func<TParam, T, DataConnection, Task> operation)
        {
            return async (arg, ct) =>
            {
                var a = (TParam)arg;
                using (var textReader = new StreamReader(Request.Body))
                {
                    string data = await textReader.ReadToEndAsync();
                    var item = JsonConvert.DeserializeObject<T>(data);
                    await DataConnection.Ask(dc => operation(a, item, dc));
                    return HttpStatusCode.Created;
                }
            };
        }

        protected void ApiPost<T, TParam>(
            string path, Func<TParam, T, DataConnection, Task> operation)
        {
            Post[path, true] = Recieve<T, TParam>(operation);
        }

		protected void ApiPut<T, TParam>(
			string path, Func<TParam, T, DataConnection, Task> operation)
		{
			Put[path, true] = Recieve<T, TParam>(operation);
		}

		protected void ApiDelete<T, TParam>(
			string path, Func<TParam, T, DataConnection, Task> operation)
		{
			Delete[path, true] = Recieve<T, TParam>(operation);
		}

        protected void ApiDelete(
            string path, Func<dynamic, DataConnection, Task> operation)
        {
            Delete[path, true] = Ask(operation);
        }
    }
}
