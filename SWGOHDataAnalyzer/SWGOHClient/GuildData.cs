using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SWGOHInterface
{
    public class GuildData
    {
        [JsonProperty("name")]
        public string GuildName { get; set; }
    }
}
