using System.Collections.Generic;
using Newtonsoft.Json;

namespace SWGOHInterface
{
    public class GuildData
    {
        [JsonProperty("name")]
        public string GuildName { get; set; }

        [JsonProperty("members")]
        public List<GuildMember> Members { get; set; }
    }
}
