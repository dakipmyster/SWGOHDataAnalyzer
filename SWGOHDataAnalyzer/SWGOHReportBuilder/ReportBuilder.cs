using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SWGOHDBInterface;
using SWGOHMessage;
using System.IO;
using iText.Html2pdf;
using iText.Kernel.Pdf;

namespace SWGOHReportBuilder
{
    public class ReportBuilder
    {        
        DataBuilder m_dataBuilder;
        string m_playerGPDifferences;

        public ReportBuilder()
        {
            m_dataBuilder = new DataBuilder();
        }

        public bool CanRunReport()
        {
            return m_dataBuilder.CanRunReport();
        }

        public async Task CompileReport()
        {
            m_dataBuilder.GetSnapshotNames();

            List<Task> tasks = new List<Task>();

            tasks.Add(m_dataBuilder.CollectPlayerGPDifferences());
            
            string fileName = SWGOHMessageSystem.InputMessage("Enter in the filename for the report");
            
            await Task.WhenAll(tasks.ToArray());

            await BuildReport(fileName);
        }

        private async Task BuildReport(string fileName)
        {
            StringBuilder pdfString = new StringBuilder();

            pdfString.Append(@"<html><head><style>
table, th, td {
  border: 1px solid black;
}
</style></head><body>");
            List<Task> tasks = new List<Task>();

            tasks.Add(FormatPlayerGPDifferences());

            await Task.WhenAll(tasks.ToArray());

            pdfString.AppendLine(m_playerGPDifferences);
            
            pdfString.AppendLine(@"</body></html>");

            string folderPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\SWGOHDataAnalyzer\\{fileName}.pdf";

            using (FileStream fs = File.Open(folderPath, FileMode.OpenOrCreate))
            {
                HtmlConverter.ConvertToPdf(pdfString.ToString(),fs, new ConverterProperties());
            }

            SWGOHMessageSystem.OutputMessage($"Report saved at {folderPath}");
        }

        private async Task FormatPlayerGPDifferences()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("This section goes over the Galatic Power (GP) differences for players between snapshots.  Here is the top ten players who have gained the most Galatic Power by total and by percentage from the previous snapshot.");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<td>Player Name</td>");
            sb.AppendLine("<td>Previous Galatic Power</td>");
            sb.AppendLine("<td>New Galatic Power</td>");
            sb.AppendLine("<td>Galatic Power Increase</td>");
            sb.AppendLine("</tr>");
            foreach (PlayerData player in m_dataBuilder.PlayerData.OrderByDescending(a => a.GalaticPowerDifference).Take(10))
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{player.PlayerName}</td>");
                sb.AppendLine($"<td>{player.OldGalaticPower}</td>");
                sb.AppendLine($"<td>{player.NewGalaticPower}</td>");
                sb.AppendLine($"<td>{player.GalaticPowerDifference}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<td>Player Name</td>");
            sb.AppendLine("<td>Previous Galatic Power</td>");
            sb.AppendLine("<td>New Galatic Power</td>");
            sb.AppendLine("<td>Galatic Power % Increase</td>");
            sb.AppendLine("</tr>");
            foreach (PlayerData player in m_dataBuilder.PlayerData.OrderByDescending(a => a.GalaticPowerPercentageDifference).Take(10))
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{player.PlayerName}</td>");
                sb.AppendLine($"<td>{player.OldGalaticPower}</td>");
                sb.AppendLine($"<td>{player.NewGalaticPower}</td>");
                sb.AppendLine($"<td>{player.GalaticPowerPercentageDifference}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");

            sb.AppendLine("Here is the full list of players and their Galatic Power differences.");

            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<td>Player Name</td>");
            sb.AppendLine("<td>Previous Galatic Power</td>");
            sb.AppendLine("<td>New Galatic Power</td>");
            sb.AppendLine("<td>Galatic Power Increase</td>");
            sb.AppendLine("<td>Galatic Power % Increase</td>");
            sb.AppendLine("</tr>");
            foreach (PlayerData player in m_dataBuilder.PlayerData.OrderBy(a => a.PlayerName).ToList())
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{player.PlayerName}</td>");
                sb.AppendLine($"<td>{player.OldGalaticPower}</td>");
                sb.AppendLine($"<td>{player.NewGalaticPower}</td>");
                sb.AppendLine($"<td>{player.GalaticPowerDifference}</td>");
                sb.AppendLine($"<td>{player.GalaticPowerPercentageDifference}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table>");

            m_playerGPDifferences = sb.ToString();
        }
    }
}

/*
Top 10 of every stat
Who 7* toon
Who G13 toon
Top 10 GP jump by number
Top 10 GP jump by percentage of their previous/current GP (edited) 
Kyle L 2:59 PM
Gold Group Members
Zetas Applied
Kyle L 3:34 PM
Journey or Legendary unlock
*/
