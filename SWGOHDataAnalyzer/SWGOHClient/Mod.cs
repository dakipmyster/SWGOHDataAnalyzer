using System.Collections.Generic;
using Newtonsoft.Json;


namespace SWGOHInterface
{
    public class Mod
    {
        public string PlayerName { get; set; }

        public string UnitName { get; set; }

        [JsonProperty("id")]
        public string ModId { get; set; }

        [JsonProperty("character")]
        public string ToonId { get; set; }

        [JsonProperty("set")]
        public string Set { get; set; }

        [JsonProperty("primary_stat")]
        public ModDetails PrimaryModData { get; set; }

        [JsonProperty("tier")]
        public string Tier { get; set; }

        [JsonProperty("rarity")]
        public string Rarity { get; set; }

        [JsonProperty("slot")]
        public string Slot { get; set; }

        [JsonProperty("secondary_stats")]
        public List<ModDetails> SecondaryStats { get; set; }
    }
}
