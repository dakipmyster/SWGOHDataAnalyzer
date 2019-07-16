using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SWGOHInterface
{
    public class Guild
    {
        [JsonProperty("players")]
        public List<Player> Players { get; set; }

        [JsonProperty("data")]
        public GuildData GuildData { get; set; }
    }
}
