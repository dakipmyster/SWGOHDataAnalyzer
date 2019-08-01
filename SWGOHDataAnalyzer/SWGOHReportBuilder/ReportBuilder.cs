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
        string m_UnitGPDifferences;
        string m_topTwentySection;
        string m_sevenStarSection;
        string m_gearTwelveToons;
        string m_gearThirteenToons;
        string m_zetasApplied;
        string m_journeyOrLegendaryUnlock;
        string m_journeyPrepared;
        string m_goldMembers;
        string m_detailedData;

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

            string fileName = SWGOHMessageSystem.InputMessage("Enter in the filename for the report");

            SWGOHMessageSystem.OutputMessage("Processing Report....");

            tasks.Add(m_dataBuilder.CollectPlayerGPDifferences());
            tasks.Add(m_dataBuilder.CollectShipData());
            tasks.Add(m_dataBuilder.CollectUnitData());

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
                        
            tasks.Add(SevenStarSection());
            tasks.Add(GearTwelveToons());
            tasks.Add(GearThirteenToons());
            tasks.Add(ZetasApplied());
            tasks.Add(JourneyOrLegendaryUnlock());
            tasks.Add(JourneyPrepared());
            tasks.Add(GoldMembers());
            tasks.Add(FormatPlayerGPDifferences());
            tasks.Add(UnitGPDifferences());
            tasks.Add(TopTwentySection());
            tasks.Add(DetailedData());

            await Task.WhenAll(tasks.ToArray());

            pdfString.AppendLine(m_playerGPDifferences);
            pdfString.AppendLine(m_UnitGPDifferences);
            pdfString.AppendLine(m_topTwentySection);
            pdfString.AppendLine(m_sevenStarSection);
            pdfString.AppendLine(m_gearTwelveToons);
            pdfString.AppendLine(m_gearThirteenToons);
            pdfString.AppendLine(m_zetasApplied);
            pdfString.AppendLine(m_journeyOrLegendaryUnlock);
            pdfString.AppendLine(m_journeyPrepared);
            pdfString.AppendLine(m_goldMembers);
            pdfString.AppendLine(m_detailedData);

            pdfString.AppendLine(@"</body></html>");

            string folderPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\SWGOHDataAnalyzer\\{fileName}.pdf";

            using (FileStream fs = File.Open(folderPath, FileMode.OpenOrCreate))
            {
                HtmlConverter.ConvertToPdf(pdfString.ToString(),fs, new ConverterProperties());
            }

            SWGOHMessageSystem.OutputMessage($"Report saved at {folderPath}");
        }

        private async Task UnitGPDifferences()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("This section highlights the top 20 toons who have had the greatest GP increase.");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("<th>Toon</th>");
            sb.AppendLine("<th>Old Power</th>");
            sb.AppendLine("<th>New Power</th>");
            sb.AppendLine("<th>Difference in Power</th>");
            sb.AppendLine("</tr>");
            foreach (UnitData unit in m_dataBuilder.UnitData.OrderByDescending(a => a.PowerDifference).Take(20))
            {                
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{unit.PlayerName}</td>");
                sb.AppendLine($"<td>{unit.UnitName}</td>");
                sb.AppendLine($"<td>{unit.OldPower}</td>");
                sb.AppendLine($"<td>{unit.NewPower}</td>");
                sb.AppendLine($"<td>{unit.PowerDifference}</td>");
                sb.AppendLine("</tr>");                
            }

            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");

            m_UnitGPDifferences = sb.ToString();

            await Task.CompletedTask;            
        }

        private async Task GoldMembers()
        {
            StringBuilder sb = new StringBuilder();
            List<UnitData> rebels = new List<UnitData>();
            List<UnitData> jkr = new List<UnitData>();
            List<UnitData> dr = new List<UnitData>();
            List<UnitData> trimitive = new List<UnitData>();
            List<UnitData> resistance = new List<UnitData>();
            List<UnitData> bountyHunter = new List<UnitData>();
            List<UnitData> nightsister = new List<UnitData>();
            List<UnitData> trooper = new List<UnitData>();
            List<UnitData> oldRepublic = new List<UnitData>();
            List<UnitData> sepratistDroid = new List<UnitData>();
            List<UnitData> bugs = new List<UnitData>();
            List<UnitData> galaticRepublic = new List<UnitData>();
            List<UnitData> ewok = new List<UnitData>();
            List<UnitData> firstOrder = new List<UnitData>();

            sb.AppendLine("This section is to showcase players who have invested the gear and zetas for 'meta' or key toons of factions");
            sb.AppendLine("<p/>");
            sb.AppendLine("Rebels team. CLS(lead, binds all things), RHan(zeta), Chewie(both), R2(number crunch), C3P0(oh my goodness)");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("</tr>");

            rebels.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Commander Luke Skywalker" && a.NewGearLevel > 11 && a.NewZetas.Contains("It Binds All Things")));
            rebels.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Han Solo" && a.NewGearLevel > 11 && a.NewZetas.Contains("Shoots First")));
            rebels.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Chewbacca" && a.NewGearLevel > 11 && a.NewZetas.Contains("Loyal Friend") && a.NewZetas.Contains("Raging Wookiee")));
            rebels.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "R2-D2" && a.NewGearLevel > 11 && a.NewZetas.Contains("Number Crunch")));
            rebels.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "C-3PO" && a.NewGearLevel > 11 && a.NewZetas.Contains("Oh My Goodness!")));

            var groupedRebels = rebels.Select(a => a.PlayerName).ToList().GroupBy(b => b);
            foreach(var player in groupedRebels)
            {
                if(player.Count() == 5)
                {
                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td>{player.Key}</td>");
                    sb.AppendLine("</tr>");
                }
            }        

            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");

            sb.AppendLine("JKR team. GMY(Battle Meditation), Jolee(That Looks Pretty Bad), Bastilla, GK, JKR(all)");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("</tr>");

            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Jolee Bindo" && a.NewGearLevel > 11 && a.NewZetas.Contains("That Looks Pretty Bad")));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Grand Master Yoda" && a.NewGearLevel > 11 && a.NewZetas.Contains("Battle Meditation")));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Bastila Shan" && a.NewGearLevel > 11));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "General Kenobi" && a.NewGearLevel > 11));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Jedi Knight Revan" && a.NewGearLevel > 11 && a.NewZetas.Contains("Direct Focus") && a.NewZetas.Contains("Hero") && a.NewZetas.Contains("General")));

            var groupedJKR = jkr.Select(a => a.PlayerName).ToList().GroupBy(b => b);
            foreach (var player in groupedJKR)
            {
                if (player.Count() == 5)
                {
                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td>{player.Key}</td>");
                    sb.AppendLine("</tr>");
                }
            }

            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");

            sb.AppendLine("DR team. DR(All), Badstilla(Sith Apprentice) HK(Self-Reconstruction), Malak(all)");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("</tr>");

            dr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "HK-47" && a.NewGearLevel > 11 && a.NewZetas.Contains("Self-Reconstruction")));
            dr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Bastila Shan (Fallen)" && a.NewGearLevel > 11 && a.NewZetas.Contains("Sith Apprentice")));
            dr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Darth Malak" && a.NewGearLevel > 11 && a.NewZetas.Contains("Gnawing Terror") && a.NewZetas.Contains("Jaws of Life")));
            dr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Darth Revan" && a.NewGearLevel > 11 && a.NewZetas.Contains("Lord of the Sith") && a.NewZetas.Contains("Conqueror") && a.NewZetas.Contains("Villain")));

            var groupedDR = dr.Select(a => a.PlayerName).ToList().GroupBy(b => b);
            foreach (var player in groupedDR)
            {
                if (player.Count() == 4)
                {
                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td>{player.Key}</td>");
                    sb.AppendLine("</tr>");
                }
            }

            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");

            sb.AppendLine("Trimitive team. Traya(all), Sion(Lord of Pain), Nihilis(Lord of Hunger)");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("</tr>");

            trimitive.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Darth Sion" && a.NewGearLevel > 11 && a.NewZetas.Contains("Lord of Pain")));
            trimitive.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Darth Nihilus" && a.NewGearLevel > 11 && a.NewZetas.Contains("Lord of Hunger")));
            trimitive.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Darth Traya" && a.NewGearLevel > 11 && a.NewZetas.Contains("Lord of Betrayal") && a.NewZetas.Contains("Compassion is Weakness")));

            var groupedTrimitive = trimitive.Select(a => a.PlayerName).ToList().GroupBy(b => b);
            foreach (var player in groupedTrimitive)
            {
                if (player.Count() == 3)
                {
                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td>{player.Key}</td>");
                    sb.AppendLine("</tr>");
                }
            }

            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");

            sb.AppendLine("Resistance team. JTR(lead), Finn, BB8(Roll with the Punches), Holdo");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("</tr>");

            resistance.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "BB-8" && a.NewGearLevel > 11 && a.NewZetas.Contains("Roll with the Punches")));
            resistance.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Rey (Jedi Training)" && a.NewGearLevel > 11 && a.NewZetas.Contains("Inspirational Presence")));
            resistance.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Finn" && a.NewGearLevel > 11));
            resistance.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Amilyn Holdo" && a.NewGearLevel > 11));

            var groupedResistance = resistance.Select(a => a.PlayerName).ToList().GroupBy(b => b);
            foreach (var player in groupedResistance)
            {
                if (player.Count() == 4)
                {
                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td>{player.Key}</td>");
                    sb.AppendLine("</tr>");
                }
            }

            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");
            /*
            sb.AppendLine("Bounty Hunter team. GMY(Battle Meditation), Jolee(That Looks Pretty Bad), Bastilla, GK, JKR(all)");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("</tr>");

            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Jolee Bindo" && a.NewGearLevel > 11 && a.NewZetas.Contains("That Looks Pretty Bad")));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Grand Master Yoda" && a.NewGearLevel > 11 && a.NewZetas.Contains("Battle Meditation")));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Bastila Shan" && a.NewGearLevel > 11));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "General Kenobi" && a.NewGearLevel > 11));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Jedi Knight Revan" && a.NewGearLevel > 11 && a.NewZetas.Contains("Direct Focus") && a.NewZetas.Contains("Hero") && a.NewZetas.Contains("General")));

            var groupedJKR = jkr.Select(a => a.PlayerName).ToList().GroupBy(b => b);
            foreach (var player in groupedJKR)
            {
                if (player.Count() == 5)
                {
                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td>{player.Key}</td>");
                    sb.AppendLine("</tr>");
                }
            }

            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");

            sb.AppendLine("Nightsister team. GMY(Battle Meditation), Jolee(That Looks Pretty Bad), Bastilla, GK, JKR(all)");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("</tr>");

            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Jolee Bindo" && a.NewGearLevel > 11 && a.NewZetas.Contains("That Looks Pretty Bad")));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Grand Master Yoda" && a.NewGearLevel > 11 && a.NewZetas.Contains("Battle Meditation")));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Bastila Shan" && a.NewGearLevel > 11));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "General Kenobi" && a.NewGearLevel > 11));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Jedi Knight Revan" && a.NewGearLevel > 11 && a.NewZetas.Contains("Direct Focus") && a.NewZetas.Contains("Hero") && a.NewZetas.Contains("General")));

            var groupedJKR = jkr.Select(a => a.PlayerName).ToList().GroupBy(b => b);
            foreach (var player in groupedJKR)
            {
                if (player.Count() == 5)
                {
                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td>{player.Key}</td>");
                    sb.AppendLine("</tr>");
                }
            }

            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");

            sb.AppendLine("Troopers team. GMY(Battle Meditation), Jolee(That Looks Pretty Bad), Bastilla, GK, JKR(all)");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("</tr>");

            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Jolee Bindo" && a.NewGearLevel > 11 && a.NewZetas.Contains("That Looks Pretty Bad")));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Grand Master Yoda" && a.NewGearLevel > 11 && a.NewZetas.Contains("Battle Meditation")));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Bastila Shan" && a.NewGearLevel > 11));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "General Kenobi" && a.NewGearLevel > 11));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Jedi Knight Revan" && a.NewGearLevel > 11 && a.NewZetas.Contains("Direct Focus") && a.NewZetas.Contains("Hero") && a.NewZetas.Contains("General")));

            var groupedJKR = jkr.Select(a => a.PlayerName).ToList().GroupBy(b => b);
            foreach (var player in groupedJKR)
            {
                if (player.Count() == 5)
                {
                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td>{player.Key}</td>");
                    sb.AppendLine("</tr>");
                }
            }

            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");

            sb.AppendLine("Old Republic team. GMY(Battle Meditation), Jolee(That Looks Pretty Bad), Bastilla, GK, JKR(all)");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("</tr>");

            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Jolee Bindo" && a.NewGearLevel > 11 && a.NewZetas.Contains("That Looks Pretty Bad")));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Grand Master Yoda" && a.NewGearLevel > 11 && a.NewZetas.Contains("Battle Meditation")));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Bastila Shan" && a.NewGearLevel > 11));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "General Kenobi" && a.NewGearLevel > 11));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Jedi Knight Revan" && a.NewGearLevel > 11 && a.NewZetas.Contains("Direct Focus") && a.NewZetas.Contains("Hero") && a.NewZetas.Contains("General")));

            var groupedJKR = jkr.Select(a => a.PlayerName).ToList().GroupBy(b => b);
            foreach (var player in groupedJKR)
            {
                if (player.Count() == 5)
                {
                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td>{player.Key}</td>");
                    sb.AppendLine("</tr>");
                }
            }

            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");

            sb.AppendLine("Sepratist Droid team. GMY(Battle Meditation), Jolee(That Looks Pretty Bad), Bastilla, GK, JKR(all)");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("</tr>");

            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Jolee Bindo" && a.NewGearLevel > 11 && a.NewZetas.Contains("That Looks Pretty Bad")));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Grand Master Yoda" && a.NewGearLevel > 11 && a.NewZetas.Contains("Battle Meditation")));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Bastila Shan" && a.NewGearLevel > 11));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "General Kenobi" && a.NewGearLevel > 11));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Jedi Knight Revan" && a.NewGearLevel > 11 && a.NewZetas.Contains("Direct Focus") && a.NewZetas.Contains("Hero") && a.NewZetas.Contains("General")));

            var groupedJKR = jkr.Select(a => a.PlayerName).ToList().GroupBy(b => b);
            foreach (var player in groupedJKR)
            {
                if (player.Count() == 5)
                {
                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td>{player.Key}</td>");
                    sb.AppendLine("</tr>");
                }
            }

            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");

            sb.AppendLine("Bugs team. GMY(Battle Meditation), Jolee(That Looks Pretty Bad), Bastilla, GK, JKR(all)");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("</tr>");

            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Jolee Bindo" && a.NewGearLevel > 11 && a.NewZetas.Contains("That Looks Pretty Bad")));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Grand Master Yoda" && a.NewGearLevel > 11 && a.NewZetas.Contains("Battle Meditation")));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Bastila Shan" && a.NewGearLevel > 11));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "General Kenobi" && a.NewGearLevel > 11));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Jedi Knight Revan" && a.NewGearLevel > 11 && a.NewZetas.Contains("Direct Focus") && a.NewZetas.Contains("Hero") && a.NewZetas.Contains("General")));

            var groupedJKR = jkr.Select(a => a.PlayerName).ToList().GroupBy(b => b);
            foreach (var player in groupedJKR)
            {
                if (player.Count() == 5)
                {
                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td>{player.Key}</td>");
                    sb.AppendLine("</tr>");
                }
            }

            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");

            sb.AppendLine("Galatic Republic team. GMY(Battle Meditation), Jolee(That Looks Pretty Bad), Bastilla, GK, JKR(all)");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("</tr>");

            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Jolee Bindo" && a.NewGearLevel > 11 && a.NewZetas.Contains("That Looks Pretty Bad")));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Grand Master Yoda" && a.NewGearLevel > 11 && a.NewZetas.Contains("Battle Meditation")));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Bastila Shan" && a.NewGearLevel > 11));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "General Kenobi" && a.NewGearLevel > 11));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Jedi Knight Revan" && a.NewGearLevel > 11 && a.NewZetas.Contains("Direct Focus") && a.NewZetas.Contains("Hero") && a.NewZetas.Contains("General")));

            var groupedJKR = jkr.Select(a => a.PlayerName).ToList().GroupBy(b => b);
            foreach (var player in groupedJKR)
            {
                if (player.Count() == 5)
                {
                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td>{player.Key}</td>");
                    sb.AppendLine("</tr>");
                }
            }

            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");

            sb.AppendLine("Ewok team. GMY(Battle Meditation), Jolee(That Looks Pretty Bad), Bastilla, GK, JKR(all)");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("</tr>");

            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Jolee Bindo" && a.NewGearLevel > 11 && a.NewZetas.Contains("That Looks Pretty Bad")));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Grand Master Yoda" && a.NewGearLevel > 11 && a.NewZetas.Contains("Battle Meditation")));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Bastila Shan" && a.NewGearLevel > 11));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "General Kenobi" && a.NewGearLevel > 11));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Jedi Knight Revan" && a.NewGearLevel > 11 && a.NewZetas.Contains("Direct Focus") && a.NewZetas.Contains("Hero") && a.NewZetas.Contains("General")));

            var groupedJKR = jkr.Select(a => a.PlayerName).ToList().GroupBy(b => b);
            foreach (var player in groupedJKR)
            {
                if (player.Count() == 5)
                {
                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td>{player.Key}</td>");
                    sb.AppendLine("</tr>");
                }
            }

            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");

            sb.AppendLine("First Order team. GMY(Battle Meditation), Jolee(That Looks Pretty Bad), Bastilla, GK, JKR(all)");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("</tr>");

            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Jolee Bindo" && a.NewGearLevel > 11 && a.NewZetas.Contains("That Looks Pretty Bad")));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Grand Master Yoda" && a.NewGearLevel > 11 && a.NewZetas.Contains("Battle Meditation")));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Bastila Shan" && a.NewGearLevel > 11));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "General Kenobi" && a.NewGearLevel > 11));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Jedi Knight Revan" && a.NewGearLevel > 11 && a.NewZetas.Contains("Direct Focus") && a.NewZetas.Contains("Hero") && a.NewZetas.Contains("General")));

            var groupedJKR = jkr.Select(a => a.PlayerName).ToList().GroupBy(b => b);
            foreach (var player in groupedJKR)
            {
                if (player.Count() == 5)
                {
                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td>{player.Key}</td>");
                    sb.AppendLine("</tr>");
                }
            }
            
            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");
            */
            m_goldMembers = sb.ToString();

            await Task.CompletedTask;
        }

        private async Task JourneyPrepared()
        {
            StringBuilder sb = new StringBuilder();
            List<Unlock> unlocks = new List<Unlock>();
            List<string> malakLocked = new List<string>();
            List<string> jediKnightRevanLocked = new List<string>();
            List<string> darthRevanLocked = new List<string>();
            List<string> commanderLukeSkywalkerLocked = new List<string>();
            List<string> jediTrainingReyLocked = new List<string>();

            sb.AppendLine("This section highlights all of the players whom are awaitng the return of Journey Toons. This only factors in if the player meets the min requirement in game to participate in the event to unlock the toon");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("<th>Toon</th>");
            sb.AppendLine("</tr>");

            foreach(PlayerData player in m_dataBuilder.PlayerData)
            {
                if (m_dataBuilder.UnitData.Where(a => a.PlayerName == player.PlayerName && a.UnitName == "Darth Malak").Count() == 0)
                    malakLocked.Add(player.PlayerName);

                if (m_dataBuilder.UnitData.Where(a => a.PlayerName == player.PlayerName && a.UnitName == "Rey (Jedi Training)").Count() == 0)
                    jediTrainingReyLocked.Add(player.PlayerName);

                if (m_dataBuilder.UnitData.Where(a => a.PlayerName == player.PlayerName && a.UnitName == "Darth Revan").Count() == 0)
                    darthRevanLocked.Add(player.PlayerName);

                if (m_dataBuilder.UnitData.Where(a => a.PlayerName == player.PlayerName && a.UnitName == "Jedi Knight Revan").Count() == 0)
                    jediKnightRevanLocked.Add(player.PlayerName);

                if (m_dataBuilder.UnitData.Where(a => a.PlayerName == player.PlayerName && a.UnitName == "Commander Luke Skywalker").Count() == 0)
                    commanderLukeSkywalkerLocked.Add(player.PlayerName);
            }
            
            foreach(string player in commanderLukeSkywalkerLocked)
            {
                if(m_dataBuilder.UnitData.Where(a => a.NewRarity == 7 &&
                    (a.UnitName == "Obi-Wan Kenobi (Old Ben)" ||
                    a.UnitName == "Stormtrooper Han" ||
                    a.UnitName == "R2-D2" ||
                    a.UnitName == "Princess Leia" ||
                    a.UnitName == "Luke Skywalker (Farmboy)")
                    ).Count() == 5)
                {
                    unlocks.Add(new Unlock(player, "Commander Luke Skywalker"));
                }
            }

            foreach (string player in jediTrainingReyLocked)
            {
                if (m_dataBuilder.UnitData.Where(a => a.NewRarity == 7 &&
                     (a.UnitName == "Rey (Scavenger)" ||
                     a.UnitName == "Finn" ||
                     a.UnitName == "BB-8" ||
                     a.UnitName == "Veteran Smuggler Chewbacca" ||
                     a.UnitName == "Veteran Smuggler Han Solo")
                    ).Count() == 5)
                {
                    unlocks.Add(new Unlock(player, "Rey (Jedi Training)"));
                }
            }

            foreach (string player in jediKnightRevanLocked)
            {
                if (m_dataBuilder.UnitData.Where(a => a.NewRarity == 7 &&
                     (a.UnitName == "T3-M4" ||
                     a.UnitName == "Mission Vao" ||
                     a.UnitName == "Zaalbar" ||
                     a.UnitName == "Jolee Bindo" ||
                     a.UnitName == "Bastila Shan")
                    ).Count() == 5)
                {
                    unlocks.Add(new Unlock(player, "Jedi Knight Revan"));
                }
            }

            foreach (string player in darthRevanLocked)
            {
                if (m_dataBuilder.UnitData.Where(a => a.NewRarity == 7 &&
                     (a.UnitName == "Bastila Shan (Fallen)" ||
                     a.UnitName == "Canderous Ordo" ||
                     a.UnitName == "Carth Onasi" ||
                     a.UnitName == "HK-47" ||
                     a.UnitName == "Juhani")
                    ).Count() == 5)
                {
                    unlocks.Add(new Unlock(player, "Darth Revan"));
                }
            }

            //For Malak, we have to make sure they have at least 4 of the any toons and then specifically the revans ready since they are specifically required
            foreach (string player in malakLocked)
            {
                if (m_dataBuilder.UnitData.Where(a => a.NewRarity == 7 && a.NewPower > 17499 &&
                    (a.UnitName == "Bastila Shan (Fallen)" ||
                     a.UnitName == "Canderous Ordo" ||
                     a.UnitName == "Carth Onasi" ||
                     a.UnitName == "HK-47" ||
                     a.UnitName == "Juhani")
                    ).Count() > 3)
                {
                    if (m_dataBuilder.UnitData.Where(a => a.NewRarity == 7 && a.NewPower > 17499 &&
                        (a.UnitName == "T3-M4" ||
                         a.UnitName == "Mission Vao" ||
                         a.UnitName == "Zaalbar" ||
                         a.UnitName == "Jolee Bindo" ||
                         a.UnitName == "Bastila Shan")
                        ).Count() > 3)
                    {
                        if (m_dataBuilder.UnitData.Where(a => a.NewRarity == 7 && a.NewPower > 17499 && a.UnitName == "Jedi Knight Revan").Count() == 1 &&
                            m_dataBuilder.UnitData.Where(a => a.NewRarity == 7 && a.NewPower > 17499 && a.UnitName == "Darth Revan").Count() == 1)
                        {
                            unlocks.Add(new Unlock(player, "Darth Malak"));
                        }
                    }
                }
            }

            foreach (Unlock unlock in unlocks.OrderBy(a => a.PlayerName))
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{unlock.PlayerName}</td>");
                sb.AppendLine($"<td>{unlock.UnitOrShipName}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");

            m_journeyPrepared = sb.ToString();

            await Task.CompletedTask;
        }

        private async Task JourneyOrLegendaryUnlock()
        {
            StringBuilder sb = new StringBuilder();
            List<Unlock> unlocks = new List<Unlock>();

            sb.AppendLine("This section highlights all players who have unlocked a Legendary or Journey toon/ship.");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("<th>Toon</th>");
            sb.AppendLine("</tr>");

            var filteredUnitList = m_dataBuilder.UnitData.Where(a => a.OldRarity == 0 && a.NewRarity != 0 && (
                a.UnitName == "Jedi Knight Revan" || 
                a.UnitName == "Darth Revan" ||
                a.UnitName == "Grand Master Yoda" ||
                a.UnitName == "C-3PO" ||
                a.UnitName == "Commander Luke Skywalker" ||
                a.UnitName == "Rey (Jedi Training)" ||
                a.UnitName == "Chewbacca" ||
                a.UnitName == "Grand Admiral Thrawn" ||
                a.UnitName == "Emperor Palpatine" ||
                a.UnitName == "BB-8" ||
                a.UnitName == "R2-D2" ||
                a.UnitName == "Padmé Amidala" ||
                a.UnitName == "Darth Malak"
            ));

            foreach (UnitData unit in filteredUnitList.OrderBy(a => a.PlayerName))
                unlocks.Add(new Unlock(unit.PlayerName, unit.UnitName));

            var filteredShipList = m_dataBuilder.ShipData.Where(a => a.OldRarity == 0 && a.NewRarity != 0 && (
            a.ShipName == "Chimaera" ||
            a.ShipName == "Han's Millennium Falcon"
            ));

            foreach (ShipData ship in filteredShipList.OrderBy(a => a.PlayerName))
                unlocks.Add(new Unlock(ship.PlayerName, ship.ShipName));          

            foreach(Unlock unlock in unlocks.OrderBy(a => a.PlayerName))
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{unlock.PlayerName}</td>");
                sb.AppendLine($"<td>{unlock.UnitOrShipName}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");

            m_journeyOrLegendaryUnlock = sb.ToString();

            await Task.CompletedTask;
        }

        private async Task ZetasApplied()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("This section highlights all of the toons that have been given zetas since the last snapshot.");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("<th>Toon</th>");
            sb.AppendLine("<th>Zetas</th>");
            sb.AppendLine("</tr>");
            foreach (UnitData unit in m_dataBuilder.UnitData.OrderBy(a => a.PlayerName))
            {
                if (unit.OldZetas.Count < unit.NewZetas.Count)
                {
                    IEnumerable<string> zetaDifferences = unit.NewZetas.Except(unit.OldZetas);

                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td>{unit.PlayerName}</td>");
                    sb.AppendLine($"<td>{unit.UnitName}</td>");
                    sb.AppendLine($"<td>{string.Join(",", zetaDifferences.ToArray())}</td>");
                    sb.AppendLine("</tr>");                    
                }
            }

            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");

            m_zetasApplied = sb.ToString();

            await Task.CompletedTask;
        }

        private async Task GearThirteenToons()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("This section highlights all of the toons that have been geared to 13 since the last snapshot.");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("<th>Toon</th>");            
            sb.AppendLine("</tr>");
            foreach (UnitData unit in m_dataBuilder.UnitData.OrderBy(a => a.PlayerName))
            {
                if(unit.OldGearLevel < 13 && unit.NewGearLevel == 13)
                {
                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td>{unit.PlayerName}</td>");
                    sb.AppendLine($"<td>{unit.UnitName}</td>");
                    sb.AppendLine("</tr>");
                }
            }
            
            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");
                        
            m_gearThirteenToons = sb.ToString();

            await Task.CompletedTask;
        }

        private async Task GearTwelveToons()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("This section highlights all of the toons that have been geared to 12 since the last snapshot.");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("<th>Toon</th>");
            sb.AppendLine("</tr>");
            foreach (UnitData unit in m_dataBuilder.UnitData.OrderBy(a => a.PlayerName))
            {
                if (unit.OldGearLevel < 12 && unit.NewGearLevel == 12)
                {
                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td>{unit.PlayerName}</td>");
                    sb.AppendLine($"<td>{unit.UnitName}</td>");
                    sb.AppendLine("</tr>");
                }
            }

            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");

            m_gearTwelveToons = sb.ToString();

            await Task.CompletedTask;
        }

        private async Task TopTwentySection()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("This section highlights the top 20 toons of various stats.  Only the stats that are affected by mods with multiple primary or secondary capabilities are highlighted here (IE Crit Damage only has a single primary stat increase, so its a easly obtained ceiling).");
            sb.AppendLine("<p/>");
            sb.AppendLine("<b>Health</b>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("<th>Toon</th>");
            sb.AppendLine("<th>Total Health</th>");
            sb.AppendLine("</tr>");
            foreach (UnitData unit in m_dataBuilder.UnitData.OrderByDescending(a => a.CurrentHealth).Take(20))
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{unit.PlayerName}</td>");
                sb.AppendLine($"<td>{unit.UnitName}</td>");
                sb.AppendLine($"<td>{unit.CurrentHealth}</td>");
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");

            sb.AppendLine("<b>Protection</b>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("<th>Toon</th>");
            sb.AppendLine("<th>Total Protection</th>");
            sb.AppendLine("</tr>");
            foreach (UnitData unit in m_dataBuilder.UnitData.OrderByDescending(a => a.CurrentProtection).Take(20))
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{unit.PlayerName}</td>");
                sb.AppendLine($"<td>{unit.UnitName}</td>");
                sb.AppendLine($"<td>{unit.CurrentProtection}</td>");
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");

            sb.AppendLine("<b>Speed</b>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("<th>Toon</th>");
            sb.AppendLine("<th>Total Speed</th>");
            sb.AppendLine("</tr>");
            foreach (UnitData unit in m_dataBuilder.UnitData.OrderByDescending(a => a.CurrentSpeed).Take(20))
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{unit.PlayerName}</td>");
                sb.AppendLine($"<td>{unit.UnitName}</td>");
                sb.AppendLine($"<td>{unit.CurrentSpeed}</td>");
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");

            sb.AppendLine("<b>Physical Offense</b>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("<th>Toon</th>");
            sb.AppendLine("<th>Total Physical Offense</th>");
            sb.AppendLine("</tr>");
            foreach (UnitData unit in m_dataBuilder.UnitData.OrderByDescending(a => a.CurrentPhysicalOffense).Take(20))
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{unit.PlayerName}</td>");
                sb.AppendLine($"<td>{unit.UnitName}</td>");
                sb.AppendLine($"<td>{unit.CurrentPhysicalOffense}</td>");
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");

            sb.AppendLine("<b>Special Offense</b>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("<th>Toon</th>");
            sb.AppendLine("<th>Total Special Offense</th>");
            sb.AppendLine("</tr>");
            foreach (UnitData unit in m_dataBuilder.UnitData.OrderByDescending(a => a.CurrentSpecialOffense).Take(20))
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{unit.PlayerName}</td>");
                sb.AppendLine($"<td>{unit.UnitName}</td>");
                sb.AppendLine($"<td>{unit.CurrentSpecialOffense}</td>");
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");

            sb.AppendLine("<b>Physical Defense</b>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("<th>Toon</th>");
            sb.AppendLine("<th>Total Physical Defense</th>");
            sb.AppendLine("</tr>");
            foreach (UnitData unit in m_dataBuilder.UnitData.OrderByDescending(a => a.CurrentPhysicalDefense).Take(20))
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{unit.PlayerName}</td>");
                sb.AppendLine($"<td>{unit.UnitName}</td>");
                sb.AppendLine($"<td>{unit.CurrentPhysicalDefense}</td>");
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");

            sb.AppendLine("<b>Special Defense</b>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("<th>Toon</th>");
            sb.AppendLine("<th>Total Special Defense</th>");
            sb.AppendLine("</tr>");
            foreach (UnitData unit in m_dataBuilder.UnitData.OrderByDescending(a => a.CurrentSpecialDefense).Take(20))
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{unit.PlayerName}</td>");
                sb.AppendLine($"<td>{unit.UnitName}</td>");
                sb.AppendLine($"<td>{unit.CurrentSpecialDefense}</td>");
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");

            sb.AppendLine("<b>Physical Crit Chance</b>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("<th>Toon</th>");
            sb.AppendLine("<th>Total Physical Crit Chance</th>");
            sb.AppendLine("</tr>");
            foreach (UnitData unit in m_dataBuilder.UnitData.OrderByDescending(a => a.CurrentPhysicalCritChance).Take(20))
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{unit.PlayerName}</td>");
                sb.AppendLine($"<td>{unit.UnitName}</td>");
                sb.AppendLine($"<td>{unit.CurrentPhysicalCritChance}</td>");
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");

            sb.AppendLine("<b>Special Crit Chance</b>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("<th>Toon</th>");
            sb.AppendLine("<th>Total Special Crit Chance</th>");
            sb.AppendLine("</tr>");
            foreach (UnitData unit in m_dataBuilder.UnitData.OrderByDescending(a => a.CurrentSpecialCritChance).Take(20))
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{unit.PlayerName}</td>");
                sb.AppendLine($"<td>{unit.UnitName}</td>");
                sb.AppendLine($"<td>{unit.CurrentSpecialCritChance}</td>");
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");

            sb.AppendLine("<b>Potency</b>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("<th>Toon</th>");
            sb.AppendLine("<th>Total Potency</th>");
            sb.AppendLine("</tr>");
            foreach (UnitData unit in m_dataBuilder.UnitData.OrderByDescending(a => a.CurrentPotency).Take(20))
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{unit.PlayerName}</td>");
                sb.AppendLine($"<td>{unit.UnitName}</td>");
                sb.AppendLine($"<td>{unit.CurrentPotency}</td>");
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");

            sb.AppendLine("<b>Tenacity</b>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("<th>Toon</th>");
            sb.AppendLine("<th>Total Tenacity</th>");
            sb.AppendLine("</tr>");
            foreach (UnitData unit in m_dataBuilder.UnitData.OrderByDescending(a => a.CurrentTenacity).Take(20))
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{unit.PlayerName}</td>");
                sb.AppendLine($"<td>{unit.UnitName}</td>");
                sb.AppendLine($"<td>{unit.CurrentTenacity}</td>");
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");

            m_topTwentySection = sb.ToString();

            await Task.CompletedTask;
        }

        private async Task SevenStarSection()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("This section highlights all of the toons that have been 7*'ed since the last snapshot.");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("<th>Toon</th>");
            sb.AppendLine("</tr>");
            foreach (UnitData unit in m_dataBuilder.UnitData.OrderBy(a => a.PlayerName))
            {
                if (unit.OldRarity < 7 && unit.NewRarity == 7)
                {
                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td>{unit.PlayerName}</td>");
                    sb.AppendLine($"<td>{unit.UnitName}</td>");
                    sb.AppendLine("</tr>");
                }
            }

            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");

            sb.AppendLine("This section highlights all of the ships that have been 7*'ed since the last snapshot.");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("<th>Toon</th>");
            sb.AppendLine("</tr>");
            foreach (ShipData ship in m_dataBuilder.ShipData.OrderBy(a => a.PlayerName))
            {
                if (ship.OldRarity < 7 && ship.NewRarity == 7)
                {
                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td>{ship.PlayerName}</td>");
                    sb.AppendLine($"<td>{ship.ShipName}</td>");
                    sb.AppendLine("</tr>");
                }
            }

            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");

            m_sevenStarSection = sb.ToString();

            await Task.CompletedTask;
        }

        private async Task FormatPlayerGPDifferences()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("This section goes over the Galatic Power (GP) differences for players between snapshots.  Here is the top ten players who have gained the most Galatic Power by total and by percentage from the previous snapshot.");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("<th>Previous Galatic Power</th>");
            sb.AppendLine("<th>New Galatic Power</th>");
            sb.AppendLine("<th>Galatic Power Increase</th>");
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
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("<th>Previous Galatic Power</th>");
            sb.AppendLine("<th>New Galatic Power</th>");
            sb.AppendLine("<th>Galatic Power % Increase</th>");
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

            m_playerGPDifferences = sb.ToString();

            await Task.CompletedTask;
        }

        private async Task DetailedData()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("For those who are interested, here is some full table data that the stats refer to.");
            sb.AppendLine("Here is the full list of players and their Galatic Power differences.");

            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("<th>Previous Galatic Power</th>");
            sb.AppendLine("<th>New Galatic Power</th>");
            sb.AppendLine("<th>Galatic Power Increase</th>");
            sb.AppendLine("<th>Galatic Power % Increase</th>");
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

            m_detailedData = sb.ToString();

            await Task.CompletedTask;
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
