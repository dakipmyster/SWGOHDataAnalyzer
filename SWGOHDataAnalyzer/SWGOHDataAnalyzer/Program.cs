using System.Threading.Tasks;
using SWGOHMessage;
using SWGOHInterface;
using System.Configuration;

namespace SWGOHDataAnalyzer
{
    class Program
    {
        static async Task Main(string[] args)
        {   
            SWGOHClient client = new SWGOHClient(ConfigurationManager.AppSettings.Get("guildid"));

            SWGOHMessageSystem.OutputMessage("Pulling down guild data");

            await client.GetGuildData();
        }

    }
}
