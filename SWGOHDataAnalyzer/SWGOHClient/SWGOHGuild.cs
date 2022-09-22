using Newtonsoft.Json;

namespace SWGOHInterface
{
    public class SWGOHGuild
    {
        [JsonProperty("data")]
        public GuildData GuildData { get; set; }
    }
}
