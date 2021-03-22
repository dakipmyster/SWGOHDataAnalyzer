using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace SWGOHInterface
{
    public class Mod
    {
        public int PlayerId { get; set; }

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

        [JsonProperty("secondary_stats")]
        public List<ModDetails> SecondaryStats { get; set; }
    }
}
