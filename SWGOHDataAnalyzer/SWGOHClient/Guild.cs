using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace SWGOHInterface
{
    public class Guild
    {
        [JsonProperty("guild_name")]
        public string GuildName { get; set; }

        [JsonProperty("snapshot_date")]
        public DateTime SnapshotDate { get; set; }

        [JsonProperty("players")]
        public List<Player> Players { get; set; }
    }
}
