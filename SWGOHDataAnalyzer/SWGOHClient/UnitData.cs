using System.Collections.Generic;
using Newtonsoft.Json;

namespace SWGOHInterface
{
    public class UnitData
    {
        [JsonProperty("relic_tier")]
        public int RelicTier { get; set; }

        [JsonProperty("gear")]
        public List<Gear> Gear { get; set; }

        [JsonProperty("power")]
        public int Power { get; set; }

        [JsonProperty("gear_level")]
        public int GearLevel { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("base_id")]
        public string UnitId { get; set; }

        [JsonProperty("level")]
        public int Level { get; set; }

        [JsonProperty("rarity")]
        public int Rarity { get; set; }

        [JsonProperty("stats")]
        public UnitStats UnitStats { get; set; }

        [JsonProperty("ability_data")]
        public List<UnitAbilities> UnitAbilities { get; set; }

        [JsonProperty("zeta_abilities")]
        public List<string> AppliedZetas { get; set; }

        [JsonProperty("omicron_abilities")]
        public List<string> AppliedOmicrons { get; set; }

        [JsonProperty("combat_type")]
        public CombatType UnitType { get; set; }

        public List<Mod> UnitMods { get; set; }
    }
}
