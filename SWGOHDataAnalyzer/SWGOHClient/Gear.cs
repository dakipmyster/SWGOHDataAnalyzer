using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SWGOHInterface
{
    public class Gear
    {
        [JsonProperty("slot")]
        public double SlotPosition { get; set; }

        [JsonProperty("is_obtained")]
        public bool IsObtained { get; set; }

        [JsonProperty("base_id")]
        public string Id { get; set; }
        
    }
}
