﻿using Newtonsoft.Json;


namespace SWGOHInterface
{
    public class ModDetails
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("display_value")]
        public string Value { get; set; }

        [JsonProperty("roll")]
        public string Roll { get; set; }
    }
}
