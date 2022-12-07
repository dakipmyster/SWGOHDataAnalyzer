using Newtonsoft.Json;

namespace SWGOHInterface
{
    public class PlayerUnit
    {
        [JsonProperty("data")]
        public UnitData UnitData { get; set; }
    }
}
