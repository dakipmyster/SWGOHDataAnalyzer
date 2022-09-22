using Newtonsoft.Json;

namespace SWGOHInterface
{
    public class DatacronTier
    {
        [JsonProperty("scope_target_name")]
        public string StatName { get; set; }

        [JsonProperty("stat_value")]
        public decimal StatValue { get; set; }
    }
}
