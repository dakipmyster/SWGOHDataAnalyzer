﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SWGOHInterface
{
    public class Guild
    {
        [JsonProperty("data")]
        public GuildData GuildData { get; set; }
    }
}
