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

        [JsonProperty("is_omega")]
        public bool IsOmega { get; set; }        

        [JsonProperty("name")]
        public string AbilityName { get; set; }

        [JsonProperty("id")]
        public string AbilityId { get; set; }

        [JsonProperty("ability_tier")]
        public int AbilityTier { get; set; }

        [JsonProperty("tier_max")]
        public int TierMax { get; set; }

        [JsonProperty("has_omicron_learned")]
        public bool HasOmicronLearned { get; set; }

        [JsonProperty("has_zeta_learned")]
        public bool HasZetaLearned { get; set; }
    }
}
