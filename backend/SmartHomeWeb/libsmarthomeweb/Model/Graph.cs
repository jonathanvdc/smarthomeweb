using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SmartHomeWeb.Model
{
    public sealed class Graph : IEquatable<Graph>
    {
        [JsonConstructor]
        private Graph()
        {
        }

        [JsonIgnore]
        public int Id
        {
            get; set;
            
        }
        //{"ownerUserName":"diana", "graphURI":"", "graphName":"test"}
        [JsonProperty("Owner", Required = Required.Always)]
        public string Owner
        {
            get; set;
        }

        [JsonProperty("GraphURI", Required = Required.Always)]
        public string GraphURI
        {
            get; set;
        }

        [JsonProperty("GraphName", Required = Required.Always)]
        public string GraphName
        {
            get; set;
        }
        /// <summary>
        /// constructs a graph from parameters
        /// </summary>
        /// <param name="gu">The URI string for the graph</param>
        /// <param name="gn">The graph's name</param>
        /// <param name="oun">The graph's owner's username</param>
        /// <param name="id">The graph's id</param>
        public Graph(string gu, string gn, string oun, int id)
        {
            Owner = oun;
            GraphURI = gu;
            GraphName = gn;
            Id = id;
        }

        public bool Equals(Graph other)
        {
            return Id == other.Id; //DB primary key, if this doesn't guarantee equality something is terribly wrong.
        }

    }
}
