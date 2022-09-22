using Newtonsoft.Json;
using System.Collections.Generic;

namespace SWGOHInterface
{
    public class Datacron
    {
        [JsonProperty("tier")]
        public decimal Tier { get; set; }

        [JsonProperty("tiers")]
        public List<DatacronTier> Tiers { get; set; }
    }
}
