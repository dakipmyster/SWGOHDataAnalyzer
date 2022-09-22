using Newtonsoft.Json;

namespace SWGOHInterface
{
    public class Guild
    {
        [JsonProperty("data")]
        public GuildData GuildData { get; set; }
    }
}
