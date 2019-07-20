using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SWGOHInterface
{
    public class PlayerData
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("galactic_power")]
        public int PlayerPower { get; set; }
    }
}
