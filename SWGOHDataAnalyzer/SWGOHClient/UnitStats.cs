using Newtonsoft.Json;

namespace SWGOHInterface
{
    public class UnitStats
    {
        public double Thickness => Health + Protection;

        [JsonProperty("1")]
        public double Health { get; set; }

        [JsonProperty("28")]
        public double Protection { get; set; }

        [JsonProperty("5")]
        public double Speed { get; set; }

        [JsonProperty("6")]
        public double PhysicalOffense { get; set; }

        [JsonProperty("7")]
        public double SpecialOffense { get; set; }

        [JsonProperty("8")]
        public double PhysicalDefense { get; set; }

        [JsonProperty("9")]
        public double SpecialDefense { get; set; }

        [JsonProperty("14")]
        public double PhysicalCriticalChance { get; set; }

        [JsonProperty("15")]
        public double SpecialCriticalChance { get; set; }
        
        [JsonProperty("17")]
        public double Potency { get; set; }

        [JsonProperty("18")]
        public double Tenacity { get; set; }

    }
}
