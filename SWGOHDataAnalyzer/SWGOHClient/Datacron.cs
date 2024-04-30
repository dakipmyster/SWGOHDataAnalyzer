using Newtonsoft.Json;
using System.Collections.Generic;

namespace SWGOHInterface
{
    public class Datacron
    {
        public string PlayerName { get; set; }

        [JsonProperty("tier")]
        public decimal Tier { get; set; }

        [JsonProperty("display_name")]
        public string Name { get; set; }

        [JsonProperty("template_base_id")]
        public string TemplateId { get; set; }

        [JsonProperty("reroll_count")]
        public decimal RerollCount { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("set_id")]
        public string SetId { get; set; }

        [JsonProperty("tiers")]
        public List<DatacronTier> Tiers { get; set; }
    }
}
