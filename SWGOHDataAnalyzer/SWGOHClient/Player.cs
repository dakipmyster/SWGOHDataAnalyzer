using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SWGOHInterface
{
    public class Player
    {
        [JsonProperty("units")]
        public List<PlayerUnit> PlayerUnits { get; set; }

        [JsonProperty("data")]
        public PlayerData PlayerData { get; set; }

        [JsonProperty("mods")]
        public List<Mod> Mods { get; set; }

        [JsonProperty("datacrons")]
        public List<Datacron> Datacrons { get; set; }
    }
}
