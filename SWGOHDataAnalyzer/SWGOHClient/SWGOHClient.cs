using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SWGOHInterface
{
    public class SWGOHClient
    {
        private string m_GuildId;
        private HttpClient m_httpClient;

        public Guild Guild;
        public HttpStatusCode ResponseCode;

        /// <summary>
        /// Consturctor
        /// </summary>
        /// <param name="guildId">Id of the guild to pull for</param>
        public SWGOHClient(string guildId)
        {
            m_GuildId = guildId;
            m_httpClient = new HttpClient();
        }

        /// <summary>
        /// Pulls player data per ally code
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetPlayerNameByAllyCodeAsync()
        {
            var response = await m_httpClient.GetAsync($"https://swgoh.gg/api/player/714669639");
            var converted = JsonConvert.DeserializeObject<Player>(await response.Content.ReadAsStringAsync());
            return converted.PlayerData.Name;
        }

        /// <summary>
        /// Pulls guild data per guild id
        /// </summary>
        /// <returns></returns>
        public async Task GetGuildData()
        {            
            var response = await m_httpClient.GetAsync($"https://swgoh.gg/api/guild/{m_GuildId}");

            if(response.StatusCode == HttpStatusCode.OK)
                Guild = JsonConvert.DeserializeObject<Guild>(await response.Content.ReadAsStringAsync());

            ResponseCode = response.StatusCode;
        }

    }
}
