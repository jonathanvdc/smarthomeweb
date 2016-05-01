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

        public Graph(int Id, GraphData Data)
        {
            this.Id = Id;
            this.Data = Data;
        }

        [JsonProperty("id", Required = Required.Always)]
        public int Id { get; private set; }

        [JsonProperty("data", Required = Required.Always)]
        public GraphData Data { get; private set; }

        public bool Equals(Graph other)
        {
            return Id == other.Id; //DB primary key, if this doesn't guarantee equality something is terribly wrong.
        }

        public override bool Equals(object obj)
        {
            return obj is Graph && Equals((Graph)obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("[Graph: Id={0}, Data={1}]", Id, Data);
        }
    }
}
