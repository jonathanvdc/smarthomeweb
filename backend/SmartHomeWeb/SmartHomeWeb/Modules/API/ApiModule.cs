using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Nancy.NegotiatorExtensions;
using Newtonsoft.Json;

namespace SmartHomeWeb.Modules.API
{

    public abstract class ApiModule : Nancy.NancyModule
    {
        protected ApiModule(string modulePath) : base(modulePath) { }

        /// <summary>
        /// Turn an operation on a parameter list and a data connection (which
        /// queries a value of type <typeparamref name="T"/> from the database)
        /// into an async API route handler that performs this operation and
        /// responds with query result serialized as a JSON object.
        /// </summary>
        /// <typeparam name="T">The query's result type.</typeparam>
        /// <param name="operation">The operation to wrap around.</param>
        /// <returns>A route handler.</returns>
        protected Func<dynamic, CancellationToken, Task<dynamic>> Ask<T>(
            Func<dynamic, DataConnection, Task<T>> operation)
        {
            return async (parameters, ct) =>
            {
                T result = await DataConnection.Ask<T>(dc => operation(parameters, dc));
                var json = JsonConvert.SerializeObject(result);
            
                var statusCode = result == null
                    ? Nancy.HttpStatusCode.NotFound
                    : Nancy.HttpStatusCode.Accepted;

                Nancy.Response response = json;
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
    }
}
