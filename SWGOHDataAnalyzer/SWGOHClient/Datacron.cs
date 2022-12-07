using Newtonsoft.Json;
using System.Collections.Generic;

namespace SWGOHInterface
{
    public class Datacron
    {
        public string PlayerName { get; set; }

        [JsonProperty("tier")]
        public decimal Tier { get; set; }

        [JsonProperty("reroll_count")]
        public decimal RerollCount { get; set; }

        [JsonProperty("id")]
        public string DataCronId { get; set; }

        [JsonProperty("tiers")]
        public List<DatacronTier> Tiers { get; set; }
    }
}
