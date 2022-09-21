using Newtonsoft.Json;

namespace SWGOHInterface
{
    public class GuildMember
    {
        [JsonProperty("ally_code")]
        public string AllyClode { get; set; }
    }
}
