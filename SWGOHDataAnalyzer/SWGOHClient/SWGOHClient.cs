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
        public List<Player> Players;
        public HttpStatusCode ResponseCode;

        /// <summary>
        /// Consturctor
        /// </summary>
        /// <param name="guildId">Id of the guild to pull for</param>
        public SWGOHClient(string guildId)
        {
            m_GuildId = guildId;
            m_httpClient = new HttpClient();
            Players = new List<Player>();
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

        public async Task GetPlayerData()
        {
            throw new NotImplementedException();
            //TODO: If I want this to do something, refactor it
            //var response = await m_httpClient.GetAsync($"http://api.swgoh.gg/player/{m_GuildId}");

            //var player = new Player();
            //var playerMod = new PlayerMod();

            //Guild = new Guild();
            //Guild.GuildData = new GuildData();
            //Guild.GuildData.GuildName = "";

            //if (response.StatusCode == HttpStatusCode.OK)
            //{
            //    player = JsonConvert.DeserializeObject<Player>(await response.Content.ReadAsStringAsync());
            //    playerMod = JsonConvert.DeserializeObject<PlayerMod>(await response.Content.ReadAsStringAsync());
            //}
            //else throw new Exception("No data found");

            //if (playerMod.Mods?.Count > 0)
            //{
            //    await player.PlayerUnits.ParallelForEachAsync(async unit =>
            //    {
            //        unit.UnitData.UnitMods = new List<Mod>();
            //        unit.UnitData.UnitMods.AddRange(playerMod.Mods.Where(a => a.ToonId == unit.UnitData.UnitId));
            //        unit.UnitData.UnitMods.ForEach(a => a.PlayerId = player.PlayerData.AllyCode);

            //    }, maxDegreeOfParallelism: Environment.ProcessorCount);
            //}

            //Guild.Players = new List<Player>();
            //Guild.Players.Add(player);

            //ResponseCode = response.StatusCode;
        }

        /// <summary>
        /// Pulls guild data per guild id
        /// </summary>
        /// <returns></returns>
        public async Task GetGuildData()
        {            
            var response = await m_httpClient.GetAsync($"http://api.swgoh.gg/guild/{m_GuildId}");

            if (response.StatusCode == HttpStatusCode.OK)
                Guild = JsonConvert.DeserializeObject<Guild>(await response.Content.ReadAsStringAsync());

            await Guild.GuildData.Members.ParallelForEachAsync(async guildMember =>
            {
                try
                {
                    var player = new Player();
                    var playerHttpClient = new HttpClient();
                    var modResponse = await playerHttpClient.GetAsync($"http://api.swgoh.gg/player/{guildMember.AllyClode}");

                    if (modResponse.StatusCode == HttpStatusCode.OK)
                        player = JsonConvert.DeserializeObject<Player>(await modResponse.Content.ReadAsStringAsync());

                    lock(Players)
                        Players.Add(player);
                }
                catch(Exception ex)
                {

                }

            }, maxDegreeOfParallelism: Environment.ProcessorCount);
            //}, maxDegreeOfParallelism: 1);

            ResponseCode = response.StatusCode;
        }

    }
}
