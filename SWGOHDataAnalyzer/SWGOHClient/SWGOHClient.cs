using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Dasync.Collections;

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
        public async Task<Player> GetPlayerDataAsync()
        {
            //var response = await m_httpClient.GetAsync($"https://swgoh.gg/api/player/714669639/?");
            var response = await m_httpClient.GetAsync($"https://swgoh.gg/api/player/714669639");
            return JsonConvert.DeserializeObject<Player>(await response.Content.ReadAsStringAsync());
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

            await Guild.Players.ParallelForEachAsync(async player =>
            {
                var playerMod = new PlayerMod();
                var modHttpClient = new HttpClient();
                var modResponse = await modHttpClient.GetAsync($"https://swgoh.gg/api/players/{player.PlayerData.AllyCode}/mods");

                if (modResponse.StatusCode == HttpStatusCode.OK)
                    playerMod = JsonConvert.DeserializeObject<PlayerMod>(await modResponse.Content.ReadAsStringAsync());

                if(playerMod.Mods?.Count > 0)
                {
                    foreach(var unit in player.PlayerUnits)
                    {
                        unit.UnitData.UnitMods = new List<Mod>();
                        unit.UnitData.UnitMods.AddRange(playerMod.Mods.Where(a => a.ToonId == unit.UnitData.UnitId));
                        unit.UnitData.UnitMods.ForEach(a => a.PlayerId = player.PlayerData.AllyCode);
                    }
                }
                
            }, maxDegreeOfParallelism: Environment.ProcessorCount);

            ResponseCode = response.StatusCode;
        }

    }
}
