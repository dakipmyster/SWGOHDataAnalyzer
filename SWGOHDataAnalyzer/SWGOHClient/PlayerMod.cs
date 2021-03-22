using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SWGOHInterface
{
    public class PlayerMod
    {
        [JsonProperty("mods")]
        public List<Mod> Mods { get; set; }
    }
}
