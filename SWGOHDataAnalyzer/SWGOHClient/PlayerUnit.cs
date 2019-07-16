using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SWGOHInterface
{
    public class PlayerUnit
    {
        [JsonProperty("data")]
        public UnitData UnitData { get; set; }
    }
}
