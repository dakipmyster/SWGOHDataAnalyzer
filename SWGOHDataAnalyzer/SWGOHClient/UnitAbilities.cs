using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SWGOHInterface
{
    public class UnitAbilities
    {
        [JsonProperty("is_zeta")]
        public bool IsZeta { get; set; }

        [JsonProperty("name")]
        public string AbilityName { get; set; }

        [JsonProperty("id")]
        public string AbilityId { get; set; }
    }
}
