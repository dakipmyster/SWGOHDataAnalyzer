using System.Threading.Tasks;
using SWGOHMessage;
using SWGOHInterface;
using SWGOHDBInterface;
using SWGOHReportBuilder;
using System.Net;
using System;

namespace SWGOHDataAnalyzer
{
    class Program
    {
        static async Task Main(string[] args)
        {   
            while(true)
            {
                switch(SWGOHMessageSystem.InputMessage(@"Enter the number option from the menu below to perform the task

1. Create Snapshot
2. Compare Snapshots
3. Quit app"))
                {
                    case "1":
                        await GetGuildData();
                        break;
                    case "2":
                        await CompareSnapshots();
                        break;
                    case "3":
                        System.Environment.Exit(0);
                        break;
                    default:
                        SWGOHMessageSystem.OutputMessage("Invalid selection, try again");
                        break;
                }                
            }
        }

        private static async Task CompareSnapshots()
        {
            ReportBuilder builder = new ReportBuilder();

            if (builder.CanRunReport())
                await builder.CompileReport();
        }

        private static async Task GetGuildData()
        {
            SWGOHClient client = new SWGOHClient(SWGOHMessageSystem.InputMessage("Enter in the SWGOH.GG Guild Id.  The Guild Id can be found in the URL, example being https://swgoh.gg/g/20799/mdndalorian-vdnguard/.  The 20799 in the URL is the Guild Id"));

            Task dataPullTask = client.GetGuildData();

            DBInterface dBInterface = new DBInterface(SWGOHMessageSystem.InputMessage("Type in the snapshot name for the data being pulled right now.  Press Enter to continue"));

            SWGOHMessageSystem.OutputMessage("Pulling down guild data...");

            await dataPullTask;

            if (client.ResponseCode == HttpStatusCode.OK)
            {
                SWGOHMessageSystem.OutputMessage("Data pull complete, writing to snapshot.");

                dBInterface.WriteDataToDB(client.Guild);
            }
            else
            {
                SWGOHMessageSystem.OutputMessage($"Data pull failed with the following HTTP status code: {client.ResponseCode.ToString()}");
            }
        }

    }
}
