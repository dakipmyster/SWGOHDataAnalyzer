using System.Threading.Tasks;
using SWGOHMessage;
using SWGOHInterface;
using SWGOHDBInterface;
using System.Configuration;

namespace SWGOHDataAnalyzer
{
    class Program
    {
        static async Task Main(string[] args)
        {   
            SWGOHClient client = new SWGOHClient(ConfigurationManager.AppSettings.Get("guildid"));

            Task dataPullTask = client.GetGuildData();

            DBInterface dBInterface = new DBInterface(SWGOHMessageSystem.InputMessage("Type in the snapshot name for the data being pulled right now.  Press Enter to continue"));

            SWGOHMessageSystem.OutputMessage("Pulling down guild data");

            await dataPullTask;

            SWGOHMessageSystem.OutputMessage("Data pull complete, writing to snapshot.");

            dBInterface.WriteDataToDB(client.Guild);

        }

    }
}
