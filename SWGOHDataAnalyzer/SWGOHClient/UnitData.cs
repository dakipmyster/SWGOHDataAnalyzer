using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SWGOHInterface
{
    public class UnitData
    {
        [JsonProperty("gear")]
        public List<Gear> Gear { get; set; }

        [JsonProperty("power")]
        public int Power { get; set; }

        [JsonProperty("gear_level")]
        public int GearLevel { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

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
    }
}
