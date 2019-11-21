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
                try
                {
                    switch (SWGOHMessageSystem.InputMessage(@"Enter the number option from the menu below to perform the task

1. Create Snapshot
2. Compare Snapshots
3. Generate simple report from latest data
4. Quit app"))
                    {
                        case "1":
                            await GetGuildData();
                            break;
                        case "2":
                            await CompareSnapshots();
                            break;
                        case "3":
                            await RunSimpleReport();
                            break;
                        case "4":
                            Environment.Exit(0);
                            break;
                        default:
                            SWGOHMessageSystem.OutputMessage("Invalid selection, try again");
                            break;
                    }
                }
                catch(Exception ex)
                {
                    SWGOHMessageSystem.OutputMessage($"An error occurred while processing the report with the following message {ex.Message} \r\n Stacktrace:{ex.StackTrace} \r\n");
                }
            }
        }

        /// <summary>
        /// Method to start the running of comparing snapshots
        /// </summary>
        /// <returns></returns>
        private static async Task CompareSnapshots()
        {
            ReportBuilder builder = new ReportBuilder();

            if (builder.CanRunReport())
                await builder.CompileReport();
        }

        /// <summary>
        /// Method to start the running of collecting guild data
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Method to start the running of creating a simple report
        /// </summary>
        /// <returns></returns>
        private static async Task RunSimpleReport()
        {
            SWGOHClient client = new SWGOHClient(SWGOHMessageSystem.InputMessage("Enter in the SWGOH.GG Guild Id.  The Guild Id can be found in the URL, example being https://swgoh.gg/g/20799/mdndalorian-vdnguard/.  The 20799 in the URL is the Guild Id"));

            Task dataPullTask = client.GetGuildData();

            string fileName = SWGOHMessageSystem.InputMessage("Enter in the filename for the report");
            string characterName = SWGOHMessageSystem.InputMessage("Enter in the toon to highlight for the report. Multiple toons can be added via comma delimited. If there is not a toon you wish to highlight, press enter to continue");

            SWGOHMessageSystem.OutputMessage("Pulling down guild data...");

            await dataPullTask;

            ReportBuilder builder = new ReportBuilder(fileName, characterName);
                        
            await builder.CompileSimpleReport(client.Guild);
        }

    }
}
