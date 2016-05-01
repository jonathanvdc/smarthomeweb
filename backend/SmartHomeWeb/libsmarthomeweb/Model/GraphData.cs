using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SmartHomeWeb.Model
{
    public sealed class GraphData
    {
        [JsonConstructor]
        private GraphData()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SmartHomeWeb.Model.GraphData"/> class.
        /// </summary>
        /// <param name="OwnerGuid">The graph's owner GUID.</param>
        /// <param name="Uri">The graph's URI.</param>
        /// <param name="Name">The graph's name.</param>
        public GraphData(string Uri, string Name, Guid OwnerGuid)
        {
            this.OwnerGuid = OwnerGuid;
            this.Uri = Uri;
            this.Name = Name;
        }

        //{"ownerGuid":"diana", "uri":"", "name":"test"}

        /// <summary>
        /// Gets the owner's globally unique identifier.
        /// </summary>
        [JsonIgnore]
        public Guid OwnerGuid { get; private set; }

        /// <summary>
        /// Gets the owner's GUID string.
        /// </summary>
        [JsonProperty("ownerGuid", Required = Required.Always)]
        public string OwnerGuidString
        {
            get { return OwnerGuid.ToString(); }
            private set { OwnerGuid = new Guid(value); }
        }

        [JsonProperty("uri", Required = Required.Always)]
        public string Uri { get; private set; }

        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; private set; }

        public override string ToString()
        {
            return string.Format("[GraphData: OwnerGuid={0}, Uri={1}, Name={2}]", OwnerGuid, Uri, Name);
        }
    }
}
