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
        /// <param name="Chart">The autofitted ranges that define this graph.</param>
        /// <param name="Name">The graph's name.</param>
        public GraphData(IEnumerable<AutofitRange> Chart, string Name, Guid OwnerGuid)
        {
            this.OwnerGuid = OwnerGuid;
            this.Chart = Chart;
            this.Name = Name;
        }

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

        [JsonProperty("chart", Required = Required.Always)]
        private AutofitRange[] chartArray;

        [JsonIgnore]
        public IEnumerable<AutofitRange> Chart
        {
            get { return chartArray; }
            private set { chartArray = value.ToArray(); }
        }

        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; private set; }

        public override string ToString()
        {
            return string.Format("[GraphData: OwnerGuid={0}, Range={1}, Name={2}]", OwnerGuid, Chart, Name);
        }
    }
}
