using iText.Html2pdf;
using SWGOHDBInterface;
using SWGOHInterface;
using SWGOHMessage;
using SWOGHHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SWGOHReportBuilder
{
    //TODO: make comments for new methods made, make a summary page on the first page that says stuff to the effect of like 'the guild increased by x gp, we got y toons to G13, we got z zetas applied
    public class ReportBuilder : ISWGOHAsync
    {        
        DataBuilder m_dataBuilder;
        string m_playerGPDifferences;
        string m_UnitGPDifferences;
        string m_topTwentySection;
        string m_sevenStarSection;
        string m_relicTiers;
        string m_zetasApplied;
        string m_omicronsApplied;
        string m_journeyOrLegendaryUnlock;
        string m_introduction;
        string m_fileName;
        string m_glProgress;
        string m_guildFocusProgress;
        string m_modStats;
        string m_datacronSection;
        string m_shipSpeed;
        string m_relicTierPerToon;
        bool m_isSimpleReport;

        List<GLCharacterProgress> m_glCharacterProgressList;
        ReportSummary m_reportSummary;

        /// <summary>
        /// Constructor
        /// </summary>
        public ReportBuilder(List<Datacron> datacrons)
        {
            m_dataBuilder = new DataBuilder(datacrons);
            m_isSimpleReport = false;
            m_reportSummary = new ReportSummary();
        }

        /// <summary>
        /// Grabs the data needed to run the detailed report
        /// </summary>
        /// <returns></returns>
        public async Task CompileReport()
        {
            m_dataBuilder.GetSnapshotNames();
            
            List<Task> tasks = new List<Task>();

            tasks.Add(Task.Run(() => m_dataBuilder.GetGuildData()));

            m_fileName = SWGOHMessageSystem.InputMessage("Enter in the filename for the report");
            SWGOHMessageSystem.OutputMessage("Compiling Report Data....");

            await Task.WhenAll(tasks.ToArray());

            await BuildReport();            
        }

        /// <summary>
        /// Builds the report and renders it into a PDF format
        /// </summary>
        /// <returns></returns>
        private async Task BuildReport()
        {
            StringBuilder pdfString = new StringBuilder();

            pdfString.Append(@"<html><head><style>
table {
  border-collapse: collapse;
  page-break-inside:auto
}
tr:nth-child(even) {background-color: #f2f2f2;}
th, td{
  padding: 2px;
}
div {
  page-break-after: always;
}
</style></head><body>");
            List<Task> tasks = new List<Task>();

            //This section can process in any order
            if(!m_isSimpleReport)
            {
                tasks.Add(Task.Run(() => CompileModStats()));
                tasks.Add(Task.Run(() => CompileDatacronStats()));
                tasks.Add(Task.Run(() => JourneyOrLegendaryUnlock()));
                tasks.Add(Task.Run(() => GalaticLegenedProgress()));
                tasks.Add(Task.Run(() => GuildFocusProgress()));
                tasks.Add(Task.Run(() => UnitGPDifferences()));
                tasks.Add(Task.Run(() => SevenStarSection()));
                tasks.Add(Task.Run(() => ZetasApplied()));
                tasks.Add(Task.Run(() => OmicronsApplied()));
                tasks.Add(Task.Run(() => PlayerGPDifferences()));
                tasks.Add(Task.Run(() => RelicTierDifferences()));
                tasks.Add(Task.Run(() => ShipSpeeds()));
                tasks.Add(Task.Run(() => RelicTiersPerToon()));
            }
                                    
            tasks.Add(Task.Run(() => TopTwentySection()));  
            
            /* For testing processing times
            tasks.Add(Task.Run(() => InvokeAsyncTask("IntroductionPage")));
            */

            await Task.WhenAll(tasks.ToArray());

            await IntroductionPage();

            //This section needs to be in order
            if (m_isSimpleReport)
            {
                pdfString.AppendLine(m_introduction);
                pdfString.AppendLine(m_topTwentySection);
            }            
            else
            {
                pdfString.AppendLine(m_introduction);
                pdfString.AppendLine(m_playerGPDifferences);
                pdfString.AppendLine(m_UnitGPDifferences);
                pdfString.AppendLine(m_topTwentySection);
                pdfString.AppendLine(m_sevenStarSection);
                pdfString.AppendLine(m_relicTiers);
                pdfString.AppendLine(m_relicTierPerToon);
                pdfString.AppendLine(m_zetasApplied);
                pdfString.AppendLine(m_omicronsApplied);
                pdfString.AppendLine(m_journeyOrLegendaryUnlock);
                pdfString.AppendLine(m_guildFocusProgress);
                pdfString.AppendLine(m_shipSpeed);
                pdfString.AppendLine(m_glProgress);                
                pdfString.AppendLine(m_modStats);
                pdfString.AppendLine(m_datacronSection);
            }

            pdfString.AppendLine(@"</body></html>");

            SWGOHMessageSystem.OutputMessage("Rendering Report....");

            string folderPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\SWGOHDataAnalyzer\\{m_fileName}.pdf";
            string folderPathHTML = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\SWGOHDataAnalyzer\\{m_fileName}.html";
            
            using (FileStream fs = File.Open(folderPath, FileMode.OpenOrCreate))
                HtmlConverter.ConvertToPdf(pdfString.ToString(),fs, new ConverterProperties().SetCharset("Unicode"));

#if DEBUG
            using (StreamWriter sw = new StreamWriter(folderPathHTML))
                sw.Write(pdfString.ToString());
#endif
            SWGOHMessageSystem.OutputMessage($"Report saved at {folderPath}");
        }

        /// <summary>
        /// Method to generate the Intro page on the report
        /// </summary>
        /// <returns></returns>
        private async Task IntroductionPage()
        {
            StringBuilder sb = new StringBuilder();

            if (m_isSimpleReport)
            {
                sb.AppendLine("<div>");
                sb.AppendLine(HTMLConstructor.ReportTitle(
                    m_dataBuilder.NewGuildData.GuildName, 
                    $"{m_dataBuilder.OldGuildData.SnapshotDate.ToString("d")} - {m_dataBuilder.NewGuildData.SnapshotDate.ToString("d")}"
                ));
                sb.AppendLine("<p/>");
                sb.AppendLine("<p/>");
                sb.AppendLine(HTMLConstructor.SectionHeader("Contents"));
                sb.AppendLine("<ol type=\"1\"");
                sb.AppendLine("<li></li>");
                sb.AppendLine("<li><a href=\"#guildsummary\">Guild Summary</a></li>");
                sb.AppendLine("<li><a href=\"#toptwenty\">Top 20 Stats</a></li>");
                sb.AppendLine("</ol></div>");
            }
            else
            {
                sb.AppendLine("<div>");
                sb.AppendLine(HTMLConstructor.ReportTitle(
                    m_dataBuilder.NewGuildData.GuildName,
                    $"{m_dataBuilder.OldGuildData.SnapshotDate.ToString("d")} - {m_dataBuilder.NewGuildData.SnapshotDate.ToString("d")}"
                ));
                sb.AppendLine("<p/>");
                sb.AppendLine("<p/>");
                sb.AppendLine(HTMLConstructor.SectionHeader("Contents"));
                sb.AppendLine("<ol type=\"1\"");
                sb.AppendLine("<li></li>");
                sb.AppendLine("<li><a href=\"#playergpdiff\">Player GP Differences</a></li>");
                sb.AppendLine("<li><a href=\"#unitgpdiff\">Toon GP Differences</a></li>");
                sb.AppendLine("<li><a href=\"#toptwenty\">Top 20 Stats</a></li>");
                sb.AppendLine("<li><a href=\"#sevenstar\">Seven Stars</a></li>");
                sb.AppendLine("<li><a href=\"#relictiers\">Relic Tier Upgrades</a></li>");
                sb.AppendLine("<li><a href=\"#relictierpertoon\">Relic Tiers Per Toon</a></li>");
                sb.AppendLine("<li><a href=\"#zetas\">Zetas Applied</a></li>");
                sb.AppendLine("<li><a href=\"#omicrons\">Omicrons Applied</a></li>");
                sb.AppendLine("<li><a href=\"#toonunlock\">Journey/Legendary/Galactic Legend Unlocks</a></li>");
                sb.AppendLine("<li><a href=\"#guildfocus\">Guild Focus Teams</a></li>");
                sb.AppendLine("<li><a href=\"#shipspeed\">Ship speeds</a></li>");
                sb.AppendLine("<li><a href=\"#glprep\">Players prepping for Galatic Legends</a></li>");                
                sb.AppendLine("<li><a href=\"#modstats\">Top mods per secondary stat</a></li>");
                sb.AppendLine("<li><a href=\"#datacronstats\">Top datacron stats</a></li>");
                sb.AppendLine("</ol>");
                sb.AppendLine("<p/>");
                sb.AppendLine("<p/>");
                sb.AppendLine(HTMLConstructor.SectionHeader("Summary"));
                sb.AppendLine("Summary of guild progress:");
                sb.AppendLine("<p/>");
                sb.AppendLine($"Guild Power Increase: {m_reportSummary.TotalGuildPowerIncrease}");
                sb.AppendLine("<p/>");
                sb.AppendLine($"Total 7* Toons gained: {m_reportSummary.TotalSevenStarToons}");
                sb.AppendLine("<p/>");
                sb.AppendLine($"Total 7* Ships gained: {m_reportSummary.TotalSevenStarShips}");
                sb.AppendLine("<p/>");
                sb.AppendLine($"Sum of Relic Levels Applied: {m_reportSummary.TotalRelicLevelsIncreased}");
                sb.AppendLine("<p/>");
                sb.AppendLine($"Sum of Zeta Abilites Unlocked: {m_reportSummary.TotalZetasApplied}");
                sb.AppendLine("<p/>");
                sb.AppendLine($"Sum of Omicron Abilites Unlocked: {m_reportSummary.TotalOmicronsApplied}");
                sb.AppendLine("<p/>");
                sb.AppendLine("</div>");

            }

            m_introduction = sb.ToString();

            await Task.CompletedTask;
        }

        /// <summary>
        /// Method to generate unit gp differences
        /// </summary>
        /// <returns></returns>
        private async Task UnitGPDifferences()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<div id=\"unitgpdiff\">");
            sb.AppendLine(HTMLConstructor.SectionHeader("Toon GP Differences"));
            sb.AppendLine("This section highlights the top 50 toons who have had the greatest GP increase.");
            sb.AppendLine("<p/>");

            StringBuilder unitGPDiff = new StringBuilder();
            
            foreach (UnitDifference unit in m_dataBuilder.DifferencesGuildData.Players
                .SelectMany(x => x.Units)
                .Where(x => x.UnitType == CombatType.Toon)
                .OrderByDescending(a => a.GPDifference)
                .Take(50))            
                unitGPDiff.AppendLine(HTMLConstructor.AddTableData(new string[] { unit.PlayerName, unit.Name, unit.OldGP.ToString(), unit.NewGP.ToString(), unit.GPDifference.ToString() }));

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon", "Old Power", "New Power", "Power Increase" }, unitGPDiff.ToString()));
            sb.AppendLine("<p/></div>");

            m_UnitGPDifferences = sb.ToString();

            await Task.CompletedTask;            
        }

        /// <summary>
        /// Method to collect data towards unlocking GLs
        /// </summary>
        /// <returns></returns>
        private async Task GalaticLegenedProgress()
        {
            StringBuilder sb = new StringBuilder();
            StringBuilder rey = new StringBuilder();
            StringBuilder slkr = new StringBuilder();
            StringBuilder luke = new StringBuilder();
            StringBuilder palp = new StringBuilder();
            StringBuilder kenobi = new StringBuilder();
            StringBuilder lv = new StringBuilder();
            StringBuilder exec = new StringBuilder();
            StringBuilder sk = new StringBuilder();
            StringBuilder jabba = new StringBuilder();
            StringBuilder leia = new StringBuilder();
            StringBuilder prof = new StringBuilder();
            StringBuilder lev = new StringBuilder();
            StringBuilder dr = new StringBuilder();
            StringBuilder overall = new StringBuilder();            
            m_glCharacterProgressList = new List<GLCharacterProgress>();

            sb.AppendLine("<div id=\"glprep\">");
            sb.AppendLine(HTMLConstructor.SectionHeader("Galatic Legend / Journey Prep"));
            sb.AppendLine("This section goes over all guild members and their progress towards a GL toon or critical Journey character.  100% for each toon indicates the player is currently in progress or has unlocked the toon.");
            sb.AppendLine("<p>Calculations of progress is based on current gear level, gear pieces applied at current gear level, relic level and star level relative to the requirement for the toon.");
                        
            foreach (Player player in m_dataBuilder.NewGuildData.Players.OrderBy(a => a.PlayerData.Name))
            {
                m_glCharacterProgressList.Add(new GLCharacterProgress() { PlayerName = player.PlayerData.Name });
                rey.AppendLine(GetGLReyProgressForPlayer(player));
                slkr.AppendLine(GetGLKyloProgressForPlayer(player));
                luke.AppendLine(GetGLLukeProgressForPlayer(player));
                palp.AppendLine(GetGLPalpProgressForPlayer(player));
                kenobi.AppendLine(GetGLKenobiProgressForPlayer(player));
                lv.AppendLine(GetGLVaderProgressForPlayer(player));
                jabba.AppendLine(GetGLJabbaProgressForPlayer(player));
                leia.AppendLine(GetGLLeiaProgressForPlayer(player));
                exec.AppendLine(GetExecProgressForPlayer(player));
                prof.AppendLine(GetProfProgressForPlayer(player));
                lev.AppendLine(GetLevProgressForPlayer(player));
                sk.AppendLine(GetStarKillerProgressForPlayer(player));
                dr.AppendLine(GetAphraProgressForPlayer(player));
                overall.AppendLine(GetOverallGLProgressForPlayer(player));
            }

            sb.AppendLine("</p>");
            sb.AppendLine("<b>Galatic Legend Rey:</b>");

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "JTR", "Finn", "Res Troop", "Scav Rey", "Res Pilot", "Poe", "RH Finn", "Holdo", "Rose", "RH Poe", "BB8", "Vet Chewie", "Raddus" }, rey.ToString()));

            sb.AppendLine("</p>");
            sb.AppendLine("<b>Supreme Leader Kylo Ren:</b>");

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "KRU", "FOS", "FOO", "Kylo Ren", "Phasma", "FOX", "Vet Han", "Sith Troop", "FOS FTP", "Hux", "FOTP", "Palp", "Finalizer" }, slkr.ToString()));

            sb.AppendLine("</p>");
            sb.AppendLine("<b>Grand Master Luke:</b>");

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Big", "C3P0", "Chew", "Han", "Yoda", "JKL", "Land", "Mon", "Obi", "Leia", "R2D2", "JTR", "CHWP", "Wed", "Y Wing" }, luke.ToString()));

            sb.AppendLine("</p>");
            sb.AppendLine("<b>Sith Eternal Palpatine:</b>");

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Pie", "Star", "Dook", "Maul", "Sidi", "Vad", "Kren", "Palp", "Veer", "Thra", "Tark", "JKA", "RGua", "Mara", "Bomb" }, palp.ToString()));

            sb.AppendLine("</p>");
            sb.AppendLine("<b>Jedi Master Kenobi:</b>");

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "GK", "Mace", "Aayla", "Katan", "Jinn", "Magna", "Sgt", "Wat", "GG", "Cad", "Cody", "Jango", "Shaak", "GMY", "Nego" }, kenobi.ToString()));

            sb.AppendLine("</p>");
            sb.AppendLine("<b>Lord Vader:</b>");

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Hunt", "Tech", "Wrek", "Tusk", "Padme", "Embo", "Echo", "BB Echo", "CD", "Zam", "GMT", "ARC", "GAS", "Nute", "Y Wing" }, lv.ToString()));

            sb.AppendLine("</p>");
            sb.AppendLine("<b>Jabba:</b>");

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Krrs", "Han", "Gredo", "Pig", "Outr", "Lando", "Luke", "Jawa", "URR", "C3P0", "Leia", "Sing", "Fenn", "Boba", "Mob" }, jabba.ToString()));

            sb.AppendLine("</p>");
            sb.AppendLine("<b>Leia:</b>");

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Rex", "Kne", "Wik", "Lando", "Acbr", "Scout", "R2", "CSolo", "ROLO", "Chrp", "Leia", "Drog", "CLS", "CHWP", "Lobot" }, leia.ToString()));

            sb.AppendLine("</p>");
            sb.AppendLine("<b>Executor:</b>");

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Vader", "Piett", "Boba", "Dengar", "IG-88", "Bossk", "TFP", "TIE A", "Bomber", "HT", "SI", "IG2K", "TIEF", "RC" }, exec.ToString()));

            sb.AppendLine("</p>");
            sb.AppendLine("<b>Profundity:</b>");

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Rad", "Cass", "Dash", "Mon", "Bis", "Jyn", "Hera", "Outrider", "Cass U", "Bis U", "Wedge X", "Biggs X", "Rebel Y", "Ghost" }, prof.ToString()));

            sb.AppendLine("</p>");
            sb.AppendLine("<b>Leviathan:</b>");

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Rev", "Malk", "SET", "ST", "Maul", "HK", "Bast", "50RT", "SAss", "Fury", "MK4", "Dag", "Bomb", "Scim", "Sfigt", "Ebon" }, lev.ToString()));

            sb.AppendLine("</p>");
            sb.AppendLine("<b>Starkiller:</b>");

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Dash", "Kyle", "Talon", "Mara" }, sk.ToString()));

            sb.AppendLine("</p>");
            sb.AppendLine("<b>Aphra:</b>");

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Hondo", "0-0-0", "BT-1", "Sana" }, dr.ToString()));

            sb.AppendLine("</p>");
            sb.AppendLine("<b>Overall Progress:</b>");

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Rey", "SLKR", "JML", "SEE", "JMK", "LV", "Jabba", "Leia", "Exec", "Prof", "Levi", "SK", "Aphra" }, overall.ToString()));

            sb.AppendLine("<p/></div>");

            m_glProgress = sb.ToString();

            await Task.CompletedTask;
        }

        private async Task RelicTiersPerToon()
        {
            StringBuilder sb = new StringBuilder();

            StringBuilder unitRelicCount = new StringBuilder();

            sb.AppendLine("<div id=\"relictierpertoon\">");
            sb.AppendLine(HTMLConstructor.SectionHeader("Relic tier per toon"));
            sb.AppendLine("This section goes over all toons in the game and sums the total number of toons the guild has per relic level.");
            sb.AppendLine("With recent game content pushing R5+, toons R4 or less will be grouped into a single group.");

            var units = m_dataBuilder.NewGuildData.Players
                .SelectMany(x => x.PlayerUnits)
                .Select(x => x.UnitData)
                .Where(x => x.UnitType == CombatType.Toon);

            var totalLowRelic = 0;
            var totalRelic5 = 0;
            var totalRelic6 = 0;
            var totalRelic7 = 0;
            var totalRelic8 = 0;
            var totalRelic9 = 0;

            var unitNames = units.Select(x => x.Name).Distinct().OrderBy(x => x);

            foreach (var unitName in unitNames)
            {
                var filteredUnits = units.Where(x => x.Name == unitName);

                var lowRelic = filteredUnits.Where(x => x.RelicTier < 5).Count();
                var relic5 = filteredUnits.Where(x => x.RelicTier == 5).Count();
                var relic6 = filteredUnits.Where(x => x.RelicTier == 6).Count();
                var relic7 = filteredUnits.Where(x => x.RelicTier == 7).Count();
                var relic8 = filteredUnits.Where(x => x.RelicTier == 8).Count();
                var relic9 = filteredUnits.Where(x => x.RelicTier == 9).Count();

                totalLowRelic = totalLowRelic + lowRelic;
                totalRelic5 = totalRelic5 + relic5;
                totalRelic6 = totalRelic6 + relic6;
                totalRelic7 = totalRelic7 + relic7;
                totalRelic8 = totalRelic8 + relic8;
                totalRelic9 = totalRelic9 + relic9;

                unitRelicCount.AppendLine(HTMLConstructor.AddTableData(new string[] {
                    unitName,
                    lowRelic.ToString(),
                    relic5.ToString(),
                    relic6.ToString(),
                    relic7.ToString(),
                    relic8.ToString(),
                    relic9.ToString()
                }));
            }

            unitRelicCount.AppendLine(HTMLConstructor.AddTableData(new string[] {
                    "Grand Total",
                    totalLowRelic.ToString(),
                    totalRelic5.ToString(),
                    totalRelic6.ToString(),
                    totalRelic7.ToString(),
                    totalRelic8.ToString(),
                    totalRelic9.ToString()
                }));

            sb.AppendLine("</p>");
            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Toon Name", "<5", "5", "6", "7", "8", "9" }, unitRelicCount.ToString()));

            sb.AppendLine("<p/></div>");

            m_relicTierPerToon = sb.ToString();

            await Task.CompletedTask;
        }

        private async Task ShipSpeeds()
        {
            StringBuilder sb = new StringBuilder();

            StringBuilder ships = new StringBuilder();

            sb.AppendLine("<div id=\"shipspeed\">");
            sb.AppendLine(HTMLConstructor.SectionHeader("Ship speeds"));
            sb.AppendLine("This section goes over a list of critical ships and their speeds per player.");

            foreach (Player player in m_dataBuilder.NewGuildData.Players.OrderBy(a => a.PlayerData.Name))
            {
                var profund = GetSpeedForUnit(player.PlayerUnits.Select(x => x.UnitData).FirstOrDefault(x => x.Name == "Profundity"));
                var scythe = GetSpeedForUnit(player.PlayerUnits.Select(x => x.UnitData).FirstOrDefault(x => x.Name == "Scythe"));
                var interceptor = GetSpeedForUnit(player.PlayerUnits.Select(x => x.UnitData).FirstOrDefault(x => x.Name == "TIE/IN Interceptor Prototype"));
                var outrider = GetSpeedForUnit(player.PlayerUnits.Select(x => x.UnitData).FirstOrDefault(x => x.Name == "Outrider"));
                var razorCrest = GetSpeedForUnit(player.PlayerUnits.Select(x => x.UnitData).FirstOrDefault(x => x.Name == "Razor Crest"));
                var xanadu = GetSpeedForUnit(player.PlayerUnits.Select(x => x.UnitData).FirstOrDefault(x => x.Name == "Xanadu Blood"));
                var executor = GetSpeedForUnit(player.PlayerUnits.Select(x => x.UnitData).FirstOrDefault(x => x.Name == "Executor"));
                var milf = GetSpeedForUnit(player.PlayerUnits.Select(x => x.UnitData).FirstOrDefault(x => x.Name == "Han's Millennium Falcon"));

                ships.AppendLine(HTMLConstructor.AddTableData(new string[] { 
                    player.PlayerData.Name,
                    profund,
                    milf,
                    outrider,
                    scythe,
                    interceptor,
                    razorCrest,
                    xanadu,
                    executor
                }));
            }

            sb.AppendLine("</p>");
            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Prof", "MILF", "OutR", "Scy", "Inter", "RC", "Xan", "Exec",  }, ships.ToString()));

            sb.AppendLine("<p/></div>");

            m_shipSpeed = sb.ToString();

            await Task.CompletedTask;
        }


        /// <summary>
        /// Method to collect data towards Guild goals
        /// </summary>
        /// <returns></returns>
        private async Task GuildFocusProgress()
        {
            StringBuilder sb = new StringBuilder();
            StringBuilder reva = new StringBuilder();
            StringBuilder tbFirstHalfToons = new StringBuilder();
            StringBuilder tbShips = new StringBuilder();

            m_glCharacterProgressList = new List<GLCharacterProgress>();

            sb.AppendLine("<div id=\"guildfocus\">");
            sb.AppendLine(HTMLConstructor.SectionHeader("Team Prep"));
            sb.AppendLine("This section goes over all guild members and their progress towards a guild focused team.  100% for each toon indicates the player is at the goal level.");
            sb.AppendLine("<p>Calculations of progress is based on current gear level, gear pieces applied at current gear level, relic level and star level relative to the requirement for the toon.");

            int revaReadyCount = 0;

            foreach (Player player in m_dataBuilder.NewGuildData.Players.OrderBy(a => a.PlayerData.Name))
            {
                reva.AppendLine(GetRevaProgressForPlayer(player, out bool ready));

                if (ready)
                    revaReadyCount++;
            }

            sb.AppendLine("</p>");
            sb.AppendLine("<b>Reva Mission (R7 Grand Inquisitor + 4 others at R7):</b>");
            sb.AppendLine($"Ready: {revaReadyCount} In progress: {m_dataBuilder.NewGuildData.Players.Count() - revaReadyCount}. NOTE: 91% progress represents R5");

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Second", "Fifth", "Seventh", "Eighth", "Ninth", "GI" }, reva.ToString()));

            sb.AppendLine("<p/>");

            foreach (Player player in m_dataBuilder.NewGuildData.Players.OrderBy(a => a.PlayerData.Name))
            {
                tbFirstHalfToons.AppendLine(GetTBFirstHalfForPlayer(player));
            }

            sb.AppendLine("</p>");
            sb.AppendLine("<b>Specific toons for TB Missions:</b>");
            sb.AppendLine($"NOTE: Bugs is all 5 Geos (average progress to R7), Wookies is Light side wookies (6 in game, drops lowest progressed one for a team of 5, then averages between the 5 to R7)");

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Mace R5", "Kit R5", "LV R5", "Qi'ra R5", "Young Han R5", "Young L R6", "Hondo R6", "Bugs R7", "Wookies R7", "Doctor Aphra", "Fennic", "Jabba" }, tbFirstHalfToons.ToString()));

            sb.AppendLine("<p/>");

            foreach (Player player in m_dataBuilder.NewGuildData.Players.OrderBy(a => a.PlayerData.Name))
            {
                tbShips.AppendLine(GetTBShipsForPlayer(player));
            }

            sb.AppendLine("</p>");
            sb.AppendLine("<b>Specific ships for TB Missions:</b>");

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Outrider", "Scythe", "Lando MC", "Prof", "Exec", "Ghost", "Imp Tie" }, tbShips.ToString()));

            sb.AppendLine("<p/></div>");

            m_guildFocusProgress = sb.ToString();

            await Task.CompletedTask;
        }

        /// <summary>
        /// Method to display a summary of a players GL progress
        /// </summary>
        /// <param name="playerName">The playername</param>
        /// <returns></returns>
        private string GetOverallGLProgressForPlayer(Player player)
        {
            GLCharacterProgress playerProgress = m_glCharacterProgressList.FirstOrDefault(a => a.PlayerName == player.PlayerData.Name);
            return HTMLConstructor.AddTableData(new string[] { player.PlayerData.Name, 
                playerProgress.ReyOverallProgress, 
                playerProgress.SLKROverallProgress, 
                playerProgress.GLLukeOverallProgress, 
                playerProgress.GLPalpOverallProgress, 
                playerProgress.GLKenobiProgress, 
                playerProgress.GLVaderProgress, 
                playerProgress.GLJabbaProgress,
                playerProgress.GLLeiaProgress,
                playerProgress.Exec, 
                playerProgress.Profundity,
                playerProgress.Leviathan,
                playerProgress.StarKiller,
                playerProgress.Aphra
            });
        }


        /// <summary>
        /// Method to determine GL Kylo progress
        /// </summary>
        /// <param name="playerName"></param>
        /// <returns></returns>
        private string GetGLLukeProgressForPlayer(Player player)
        {
            List<decimal> progressList = new List<decimal>();
            string big = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Biggs Darklighter"), GLProgressScore.RelicThree, out progressList, progressList);
            string c3p0 = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "C-3PO"), GLProgressScore.RelicFive, out progressList, progressList);
            string chew = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Chewbacca"), GLProgressScore.RelicSix, out progressList, progressList);
            string han = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Han Solo"), GLProgressScore.RelicSix, out progressList, progressList);
            string yoda = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Hermit Yoda"), GLProgressScore.RelicFive, out progressList, progressList);
            string jkl = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Jedi Knight Luke Skywalker"), GLProgressScore.RelicSeven, out progressList, progressList);
            string land = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Lando Calrissian"), GLProgressScore.RelicFive, out progressList, progressList);
            string mon = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Mon Mothma"), GLProgressScore.RelicFive, out progressList, progressList);
            string obi = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Obi-Wan Kenobi (Old Ben)"), GLProgressScore.RelicFive, out progressList, progressList);
            string leia = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Princess Leia"), GLProgressScore.RelicThree, out progressList, progressList);
            string r2d2 = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "R2-D2"), GLProgressScore.RelicSeven, out progressList, progressList);
            string jtr = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Rey (Jedi Training)"), GLProgressScore.RelicSeven, out progressList, progressList);
            string chwp = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Threepio & Chewie"), GLProgressScore.RelicFive, out progressList, progressList);
            string wed = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Wedge Antilles"), GLProgressScore.RelicThree, out progressList, progressList);
            string ywin = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Rebel Y-wing"), GLProgressScore.SixStar, out progressList, progressList);

            var averageProgress = Math.Round(progressList.Average(), 2);

            m_glCharacterProgressList.FirstOrDefault(a => a.PlayerName == player.PlayerData.Name).GLLukeOverallProgress = averageProgress.ToString();

            if (averageProgress != 100)
                return HTMLConstructor.AddTableData(new string[] { player.PlayerData.Name, big, c3p0, chew, han, yoda, jkl, land, mon, obi, leia, r2d2, jtr, chwp, wed, ywin });
            
            return "";
        }

        /// <summary>
        /// Method to determine GL Kylo progress
        /// </summary>
        /// <param name="playerName"></param>
        /// <returns></returns>
        private string GetGLPalpProgressForPlayer(Player player)
        {
            List<decimal> progressList = new List<decimal>();
            string pie = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Admiral Piett"), GLProgressScore.RelicFive, out progressList, progressList);
            string star = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Colonel Starck"), GLProgressScore.RelicThree, out progressList, progressList);
            string dook = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Count Dooku"), GLProgressScore.RelicSix, out progressList, progressList);
            string maul = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Darth Maul"), GLProgressScore.RelicFour, out progressList, progressList);
            string sidi = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Darth Sidious"), GLProgressScore.RelicSeven, out progressList, progressList);
            string vad = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Darth Vader"), GLProgressScore.RelicSeven, out progressList, progressList);
            string kren = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Director Krennic"), GLProgressScore.RelicFour, out progressList, progressList);
            string palp = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Emperor Palpatine"), GLProgressScore.RelicSeven, out progressList, progressList);
            string veer = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "General Veers"), GLProgressScore.RelicThree, out progressList, progressList);
            string thra = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Grand Admiral Thrawn"), GLProgressScore.RelicSix, out progressList, progressList);
            string tark = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Grand Moff Tarkin"), GLProgressScore.RelicThree, out progressList, progressList);
            string jka = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Jedi Knight Anakin"), GLProgressScore.RelicSeven, out progressList, progressList);
            string rgua = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Royal Guard"), GLProgressScore.RelicThree, out progressList, progressList);
            string mara = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Sith Marauder"), GLProgressScore.RelicSeven, out progressList, progressList);
            string bomb = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Imperial TIE Bomber"), GLProgressScore.SixStar, out progressList, progressList);

            var averageProgress = Math.Round(progressList.Average(), 2);

            m_glCharacterProgressList.FirstOrDefault(a => a.PlayerName == player.PlayerData.Name).GLPalpOverallProgress = averageProgress.ToString();

            if (averageProgress != 100)
                return HTMLConstructor.AddTableData(new string[] { player.PlayerData.Name, pie, star, dook, maul, sidi, vad, kren, palp, veer, thra, tark, jka, rgua, mara, bomb });
            
            return "";
        }

        /// <summary>
        /// Method to determine GL Kenobi progress
        /// </summary>
        /// <param name="playerName"></param>
        /// <returns></returns>
        private string GetGLKenobiProgressForPlayer(Player player)
        {
            List<decimal> progressList = new List<decimal>();
            string kenobi = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "General Kenobi"), GLProgressScore.RelicEight, out progressList, progressList);
            string mace = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Mace Windu"), GLProgressScore.RelicThree, out progressList, progressList);
            string aayla = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Aayla Secura"), GLProgressScore.RelicThree, out progressList, progressList);
            string katan = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Bo-Katan Kryze"), GLProgressScore.RelicFive, out progressList, progressList);
            string jinn = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Qui-Gon Jinn"), GLProgressScore.RelicThree, out progressList, progressList);
            string magna = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "IG-100 MagnaGuard"), GLProgressScore.RelicFive, out progressList, progressList);
            string clone = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Clone Sergeant - Phase I"), GLProgressScore.RelicFive, out progressList, progressList);
            string wat = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Wat Tambor"), GLProgressScore.RelicSeven, out progressList, progressList);
            string gg = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "General Grievous"), GLProgressScore.RelicSeven, out progressList, progressList);
            string cadbane = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Cad Bane"), GLProgressScore.RelicFive, out progressList, progressList);
            string cody = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "CC-2224 \"Cody\""), GLProgressScore.RelicFive, out progressList, progressList);
            string jango = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Jango Fett"), GLProgressScore.RelicSeven, out progressList, progressList);
            string shaak = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Shaak Ti"), GLProgressScore.RelicSeven, out progressList, progressList);
            string gmy = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Grand Master Yoda"), GLProgressScore.RelicEight, out progressList, progressList);
            string nego = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Negotiator"), GLProgressScore.SixStar, out progressList, progressList);

            var averageProgress = Math.Round(progressList.Average(), 2);

            m_glCharacterProgressList.FirstOrDefault(a => a.PlayerName == player.PlayerData.Name).GLKenobiProgress = averageProgress.ToString();

            if (averageProgress != 100)
                return HTMLConstructor.AddTableData(new string[] { player.PlayerData.Name, kenobi, mace, aayla, katan, jinn, magna, clone, wat, gg, cadbane, cody, jango, shaak, gmy, nego });
            
            return "";
        }

        private string GetGLVaderProgressForPlayer(Player player)
        {
            List<decimal> progressList = new List<decimal>();
            string hunter = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Hunter"), GLProgressScore.RelicFive, out progressList, progressList);
            string tech = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Tech"), GLProgressScore.RelicFive, out progressList, progressList);
            string wrecker = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Wrecker"), GLProgressScore.RelicFive, out progressList, progressList);
            string tusken = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Tusken Raider"), GLProgressScore.RelicFive, out progressList, progressList);
            string padme = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Padmé Amidala"), GLProgressScore.RelicEight, out progressList, progressList);
            string embo = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Embo"), GLProgressScore.RelicFive, out progressList, progressList);
            string echo = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "CT-21-0408 \"Echo\""), GLProgressScore.RelicSeven, out progressList, progressList);
            string bbEcho = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Echo"), GLProgressScore.RelicFive, out progressList, progressList);
            string dooku = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Count Dooku"), GLProgressScore.RelicEight, out progressList, progressList);
            string zam = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Zam Wesell"), GLProgressScore.RelicSeven, out progressList, progressList);
            string tarkin = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Grand Moff Tarkin"), GLProgressScore.RelicSeven, out progressList, progressList);
            string arc = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "ARC Trooper"), GLProgressScore.RelicEight, out progressList, progressList);
            string gas = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "General Skywalker"), GLProgressScore.RelicEight, out progressList, progressList);
            string nute = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Nute Gunray"), GLProgressScore.RelicSeven, out progressList, progressList);
            string ywing = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "BTL-B Y-wing Starfighter"), GLProgressScore.SevenStar, out progressList, progressList);

            var averageProgress = Math.Round(progressList.Average(), 2);

            m_glCharacterProgressList.FirstOrDefault(a => a.PlayerName == player.PlayerData.Name).GLVaderProgress = averageProgress.ToString();

            if(averageProgress != 100)
                return HTMLConstructor.AddTableData(new string[] { player.PlayerData.Name, hunter, tech, wrecker, tusken, padme, embo, echo, bbEcho, dooku, zam, tarkin, arc, gas, nute, ywing});

            return "";
        }

        private string GetGLLeiaProgressForPlayer(Player player)
        {
            List<decimal> progressList = new List<decimal>();
            string rex = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Captain Rex"), GLProgressScore.RelicSeven, out progressList, progressList);
            string knee = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Princess Kneesaa"), GLProgressScore.RelicSeven, out progressList, progressList);
            string wic = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Wicket"), GLProgressScore.RelicThree, out progressList, progressList);
            string lando = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Lando Calrissian"), GLProgressScore.RelicFive, out progressList, progressList);
            string ackbar = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Admiral Ackbar"), GLProgressScore.RelicThree, out progressList, progressList);
            string scout = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Scout Trooper"), GLProgressScore.SevenStar, out progressList, progressList);
            string r2 = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "R2-D2"), GLProgressScore.RelicEight, out progressList, progressList);
            string captsolo = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Captain Han Solo"), GLProgressScore.RelicFive, out progressList, progressList);
            string rolo = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Rebel Officer Leia Organa"), GLProgressScore.RelicFive, out progressList, progressList);
            string chirpa = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Chief Chirpa"), GLProgressScore.RelicThree, out progressList, progressList);
            string leia = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Boushh (Leia Organa)"), GLProgressScore.RelicFive, out progressList, progressList);
            string drogan = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Captain Drogan"), GLProgressScore.RelicSeven, out progressList, progressList);
            string cls = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Commander Luke Skywalker"), GLProgressScore.RelicSeven, out progressList, progressList);
            string chewpio = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Threepio & Chewie"), GLProgressScore.RelicSeven, out progressList, progressList);
            string lobot = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Lobot"), GLProgressScore.RelicThree, out progressList, progressList);

            var averageProgress = Math.Round(progressList.Average(), 2);

            m_glCharacterProgressList.FirstOrDefault(a => a.PlayerName == player.PlayerData.Name).GLLeiaProgress = averageProgress.ToString();

            if (averageProgress != 100)
                return HTMLConstructor.AddTableData(new string[] { player.PlayerData.Name, rex, knee, wic, lando, ackbar, scout, r2, captsolo, rolo, chirpa, leia, drogan, cls, chewpio, lobot });

            return "";
        }

        private string GetGLJabbaProgressForPlayer(Player player)
        {
            List<decimal> progressList = new List<decimal>();
            string krrsantan = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Krrsantan"), GLProgressScore.RelicFive, out progressList, progressList);
            string han = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Han Solo"), GLProgressScore.RelicEight, out progressList, progressList);
            string gam = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Gamorrean Guard"), GLProgressScore.RelicThree, out progressList, progressList);
            string greedo = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Greedo"), GLProgressScore.RelicSix, out progressList, progressList);
            string outrider = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Outrider"), GLProgressScore.SevenStar, out progressList, progressList);
            string lando = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Skiff Guard (Lando Calrissian)"), GLProgressScore.RelicFive, out progressList, progressList);
            string luke = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Jedi Knight Luke Skywalker"), GLProgressScore.RelicSeven, out progressList, progressList);
            string jawa = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Jawa"), GLProgressScore.RelicThree, out progressList, progressList);
            string tuskan = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "URoRRuR'R'R"), GLProgressScore.RelicFour, out progressList, progressList);
            string c3p0 = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "C-3PO"), GLProgressScore.RelicSeven, out progressList, progressList);
            string leia = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Boushh (Leia Organa)"), GLProgressScore.RelicFive, out progressList, progressList);
            string sing = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Aurra Sing"), GLProgressScore.RelicSix, out progressList, progressList);
            string fennic = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Fennec Shand"), GLProgressScore.RelicSeven, out progressList, progressList);
            string boba = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Boba Fett"), GLProgressScore.RelicSeven, out progressList, progressList);
            string mob = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Mob Enforcer"), GLProgressScore.RelicThree, out progressList, progressList);

            var averageProgress = Math.Round(progressList.Average(), 2);

            m_glCharacterProgressList.FirstOrDefault(a => a.PlayerName == player.PlayerData.Name).GLJabbaProgress = averageProgress.ToString();

            if(averageProgress != 100)
                return HTMLConstructor.AddTableData(new string[] { player.PlayerData.Name, krrsantan, han, gam, greedo, outrider, lando, luke, jawa, tuskan, c3p0, leia, sing, fennic, boba, mob });

            return "";
        }

        private string GetExecProgressForPlayer(Player player)
        {
            List<decimal> progressList = new List<decimal>();
            string vader = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Darth Vader"), GLProgressScore.RelicSeven, out progressList, progressList);
            string piett = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Admiral Piett"), GLProgressScore.RelicEight, out progressList, progressList);
            string boba = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Boba Fett"), GLProgressScore.RelicEight, out progressList, progressList);
            string dengar = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Dengar"), GLProgressScore.RelicFive, out progressList, progressList);
            string ig88 = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "IG-88"), GLProgressScore.RelicFive, out progressList, progressList);
            string bossk = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Bossk"), GLProgressScore.RelicFive, out progressList, progressList);
            string tfp = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "TIE Fighter Pilot"), GLProgressScore.RelicFive, out progressList, progressList);

            string advanced = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "TIE Advanced x1"), GLProgressScore.FourStar, out progressList, progressList);
            string bomber = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Imperial TIE Bomber"), GLProgressScore.FourStar, out progressList, progressList);
            string ht = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Hound's Tooth"), GLProgressScore.FourStar, out progressList, progressList);
            string slave = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Slave I"), GLProgressScore.FourStar, out progressList, progressList);
            string ig2000 = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "IG-2000"), GLProgressScore.FourStar, out progressList, progressList);
            string tie = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Imperial TIE Fighter"), GLProgressScore.FourStar, out progressList, progressList);
            string rc = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Razor Crest"), GLProgressScore.FiveStar, out progressList, progressList);

            var averageProgress = Math.Round(progressList.Average(), 2);

            m_glCharacterProgressList.FirstOrDefault(a => a.PlayerName == player.PlayerData.Name).Exec = averageProgress.ToString();

            if(averageProgress != 100)
                return HTMLConstructor.AddTableData(new string[] { player.PlayerData.Name, vader, piett, boba, dengar, ig88, bossk, tfp, advanced, bomber, ht, slave, ig2000, tie, rc });

            return "";
        }

        private string GetLevProgressForPlayer(Player player)
        {
            List<decimal> progressList = new List<decimal>();
            string revan = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Darth Revan"), GLProgressScore.RelicNine, out progressList, progressList);
            string malak = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Darth Malak"), GLProgressScore.RelicNine, out progressList, progressList);
            string set = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Sith Empire Trooper"), GLProgressScore.RelicSeven, out progressList, progressList);
            string trooper = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Sith Trooper"), GLProgressScore.RelicSeven, out progressList, progressList);
            string maul = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Darth Maul"), GLProgressScore.RelicSeven, out progressList, progressList);
            string hk47 = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "HK-47"), GLProgressScore.RelicSeven, out progressList, progressList);
            string bastilla = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Bastila Shan"), GLProgressScore.RelicSeven, out progressList, progressList);
            string sort = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "50R-T"), GLProgressScore.RelicFive, out progressList, progressList);
            string ass = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Sith Assassin"), GLProgressScore.RelicFive, out progressList, progressList);

            string fury = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Fury-class Interceptor"), GLProgressScore.SevenStar, out progressList, progressList);
            string mk4 = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Mark VI Interceptor"), GLProgressScore.SevenStar, out progressList, progressList);
            string dagger = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "TIE Dagger"), GLProgressScore.SevenStar, out progressList, progressList);
            string bomber = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "B-28 Extinction-class Bomber"), GLProgressScore.SevenStar, out progressList, progressList);
            string scimitar = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Scimitar"), GLProgressScore.SevenStar, out progressList, progressList);
            string sithf = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Sith Fighter"), GLProgressScore.SevenStar, out progressList, progressList);
            string ebon = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Ebon Hawk"), GLProgressScore.SevenStar, out progressList, progressList);

            var averageProgress = Math.Round(progressList.Average(), 2);

            m_glCharacterProgressList.FirstOrDefault(a => a.PlayerName == player.PlayerData.Name).Leviathan = averageProgress.ToString();

            if (averageProgress != 100)
                return HTMLConstructor.AddTableData(new string[] { player.PlayerData.Name, revan, malak, set, trooper, maul, hk47, bastilla, sort, ass, fury, mk4, dagger, bomber, scimitar, sithf, ebon });

            return "";
        }

        private string GetProfProgressForPlayer(Player player)
        {
            List<decimal> progressList = new List<decimal>();
            string raddus = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Admiral Raddus"), GLProgressScore.RelicNine, out progressList, progressList);
            string cassian = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Cassian Andor"), GLProgressScore.RelicEight, out progressList, progressList);
            string dash = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Dash Rendar"), GLProgressScore.RelicSeven, out progressList, progressList);
            string mon = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Mon Mothma"), GLProgressScore.RelicFive, out progressList, progressList);
            string bist = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Bistan"), GLProgressScore.RelicFive, out progressList, progressList);
            string jyn = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Jyn Erso"), GLProgressScore.RelicFive, out progressList, progressList);
            string hera = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Hera Syndulla"), GLProgressScore.RelicFive, out progressList, progressList);

            string outrider = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Outrider"), GLProgressScore.SevenStar, out progressList, progressList);
            string cassU = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Cassian's U-wing"), GLProgressScore.SevenStar, out progressList, progressList);
            string bisU = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Bistan's U-wing"), GLProgressScore.SevenStar, out progressList, progressList);
            string wedgeX = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Wedge Antilles's X-wing"), GLProgressScore.SevenStar, out progressList, progressList);
            string biggsX = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Biggs Darklighter's X-wing"), GLProgressScore.SevenStar, out progressList, progressList);
            string rebelY = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Rebel Y-wing"), GLProgressScore.SevenStar, out progressList, progressList);
            string ghost = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Ghost"), GLProgressScore.SevenStar, out progressList, progressList);

            var averageProgress = Math.Round(progressList.Average(), 2);

            m_glCharacterProgressList.FirstOrDefault(a => a.PlayerName == player.PlayerData.Name).Profundity = averageProgress.ToString();

            if(averageProgress != 100)
                return HTMLConstructor.AddTableData(new string[] { player.PlayerData.Name, raddus, cassian, dash, mon, bist, jyn, hera, outrider, cassU, bisU, wedgeX, biggsX, rebelY, ghost });

            return "";
        }

        private string GetStarKillerProgressForPlayer(Player player)
        {
            List<decimal> progressList = new List<decimal>();
            string dash = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Dash Rendar"), GLProgressScore.RelicFive, out progressList, progressList);
            string kyle = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Kyle Katarn"), GLProgressScore.RelicFive, out progressList, progressList);
            string talon = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Darth Talon"), GLProgressScore.RelicFive, out progressList, progressList);
            string mara = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Mara Jade, The Emperor's Hand"), GLProgressScore.RelicFive, out progressList, progressList);

            var averageProgress = Math.Round(progressList.Average(), 2);

            m_glCharacterProgressList.FirstOrDefault(a => a.PlayerName == player.PlayerData.Name).StarKiller = averageProgress.ToString();

            if(averageProgress != 100)
                return HTMLConstructor.AddTableData(new string[] { player.PlayerData.Name, dash, kyle, talon, mara });

            return "";
        }

        private string GetAphraProgressForPlayer(Player player)
        {
            List<decimal> progressList = new List<decimal>();
            string hondo = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Hondo Ohnaka"), GLProgressScore.RelicFive, out progressList, progressList);
            string threeZero = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "0-0-0"), GLProgressScore.RelicFive, out progressList, progressList);
            string bt = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "BT-1"), GLProgressScore.RelicFive, out progressList, progressList);
            string sana = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Sana Starros"), GLProgressScore.RelicFive, out progressList, progressList);

            var averageProgress = Math.Round(progressList.Average(), 2);

            m_glCharacterProgressList.FirstOrDefault(a => a.PlayerName == player.PlayerData.Name).Aphra = averageProgress.ToString();

            if (averageProgress != 100)
                return HTMLConstructor.AddTableData(new string[] { player.PlayerData.Name, hondo, threeZero, bt, sana });

            return "";
        }

        /// <summary>
        /// Method to determine GL Kylo progress
        /// </summary>
        /// <param name="playerName"></param>
        /// <returns></returns>
        private string GetGLKyloProgressForPlayer(Player player)
        {
            List<decimal> progressList = new List<decimal>();
            string kru = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Kylo Ren (Unmasked)"), GLProgressScore.RelicSeven, out progressList, progressList);
            string fos = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "First Order Stormtrooper"), GLProgressScore.RelicFive, out progressList, progressList);
            string foo = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "First Order Officer"), GLProgressScore.RelicFive, out progressList, progressList);
            string kyloRen = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Kylo Ren"), GLProgressScore.RelicSeven, out progressList, progressList);
            string phasma = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Captain Phasma"), GLProgressScore.RelicFive, out progressList, progressList);
            string fox = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "First Order Executioner"), GLProgressScore.RelicFive, out progressList, progressList);
            string vetHan = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Veteran Smuggler Han Solo"), GLProgressScore.RelicThree, out progressList, progressList);
            string sithTroop = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Sith Trooper"), GLProgressScore.RelicFive, out progressList, progressList);
            string fosftp = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "First Order SF TIE Pilot"), GLProgressScore.RelicThree, out progressList, progressList);
            string hux = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "General Hux"), GLProgressScore.RelicFive, out progressList, progressList);
            string fotp = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "First Order TIE Pilot"), GLProgressScore.RelicThree, out progressList, progressList);
            string palp = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Emperor Palpatine"), GLProgressScore.RelicSeven, out progressList, progressList);
            string finalizer = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Finalizer"), GLProgressScore.FiveStar, out progressList, progressList);

            var averageProgress = Math.Round(progressList.Average(), 2);

            m_glCharacterProgressList.FirstOrDefault(a => a.PlayerName == player.PlayerData.Name).SLKROverallProgress = averageProgress.ToString();

            if(averageProgress != 100)
                return HTMLConstructor.AddTableData(new string[] { player.PlayerData.Name, kru, fos, foo, kyloRen, phasma, fox, vetHan, sithTroop, fosftp, hux, fotp, palp, finalizer });

            return "";
        }

        private string GetTBShipsForPlayer(Player player)
        {
            List<decimal> progressList = new List<decimal>();
            string outrider = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Outrider"), GLProgressScore.SevenStar, out progressList, progressList);
            string scythe = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Scythe"), GLProgressScore.SevenStar, out progressList, progressList);
            string lando = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Lando's Millennium Falcon"), GLProgressScore.SevenStar, out progressList, progressList);
            string profundity = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Profundity"), GLProgressScore.SevenStar, out progressList, progressList);
            string executor = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Executor"), GLProgressScore.SevenStar, out progressList, progressList);
            string ghost = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Ghost"), GLProgressScore.SevenStar, out progressList, progressList);
            string tie = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Imperial TIE Fighter"), GLProgressScore.SevenStar, out progressList, progressList);

            return HTMLConstructor.AddTableData(new string[] { player.PlayerData.Name, outrider, scythe, lando, profundity, executor, ghost, tie });
        }

        private string GetTBFirstHalfForPlayer(Player player)
        {
            List<decimal> progressList = new List<decimal>();
            string mace = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Mace Windu"), GLProgressScore.RelicFive, out progressList, progressList);
            string kit = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Kit Fisto"), GLProgressScore.RelicFive, out progressList, progressList);
            string lordVader = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Lord Vader"), GLProgressScore.RelicFive, out progressList, progressList);
            string qira = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Qi'ra"), GLProgressScore.RelicFive, out progressList, progressList);
            string youngHan = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Young Han Solo"), GLProgressScore.RelicFive, out progressList, progressList);
            string youngLando = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Young Lando Calrissian"), GLProgressScore.RelicSix, out progressList, progressList);
            string hondo = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Hondo Ohnaka"), GLProgressScore.RelicSix, out progressList, progressList);
            string broodAlpha = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Geonosian Brood Alpha"), GLProgressScore.RelicSeven, out progressList, progressList);
            string spy = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Geonosian Spy"), GLProgressScore.RelicSeven, out progressList, progressList);
            string solder = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Geonosian Soldier"), GLProgressScore.RelicSeven, out progressList, progressList);
            string poggle = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Poggle the Lesser"), GLProgressScore.RelicSeven, out progressList, progressList);
            string sunFac = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Sun Fac"), GLProgressScore.RelicSeven, out progressList, progressList);
            string chewie = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Chewbacca"), GLProgressScore.RelicSeven, out progressList, progressList);
            string chewpio = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Threepio & Chewie"), GLProgressScore.RelicSeven, out progressList, progressList);
            string cloneChewie = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Clone Wars Chewbacca"), GLProgressScore.RelicSeven, out progressList, progressList);
            string vetChewie = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Veteran Smuggler Chewbacca"), GLProgressScore.RelicSeven, out progressList, progressList);
            string vandorChewie = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Vandor Chewbacca"), GLProgressScore.RelicSeven, out progressList, progressList);
            string zaalbar = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Zaalbar"), GLProgressScore.RelicSeven, out progressList, progressList);
            string aphra = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Doctor Aphra"), GLProgressScore.RelicSeven, out progressList, progressList);
            string fennic = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Fennec Shand"), GLProgressScore.RelicSeven, out progressList, progressList);
            string jabba = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Jabba the Hutt"), GLProgressScore.RelicSeven, out progressList, progressList);

            var wookies = new List<int>()
            {
                Convert.ToInt32(chewie),
                Convert.ToInt32(chewpio),
                Convert.ToInt32(cloneChewie),
                Convert.ToInt32(vetChewie),
                Convert.ToInt32(vandorChewie),
                Convert.ToInt32(zaalbar)
            }.OrderByDescending(x => x).Take(5).Average().ToString();

            var bugs = new List<int>()
            {
                Convert.ToInt32(broodAlpha),
                Convert.ToInt32(spy),
                Convert.ToInt32(solder),
                Convert.ToInt32(sunFac),
                Convert.ToInt32(poggle)
            }.Average().ToString();

            return HTMLConstructor.AddTableData(new string[] { player.PlayerData.Name, mace, kit, lordVader, qira, youngHan, youngLando, hondo, bugs, wookies, aphra, fennic, jabba });
        }

        /// <summary>
        /// Method to determine Reva progress for a player
        /// </summary>
        /// <param name="playerName"></param>
        /// <returns></returns>
        private string GetRevaProgressForPlayer(Player player, out bool ready)
        {
            ready = false;

            List<decimal> progressList = new List<decimal>();
            string second = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Second Sister"), GLProgressScore.RelicSeven, out progressList, progressList);
            string fifth = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Fifth Brother"), GLProgressScore.RelicSeven, out progressList, progressList);
            string seventh = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Seventh Sister"), GLProgressScore.RelicSeven, out progressList, progressList);
            string eighth = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Eighth Brother"), GLProgressScore.RelicSeven, out progressList, progressList);
            string ninth = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Ninth Sister"), GLProgressScore.RelicSeven, out progressList, progressList);
            string gi = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Grand Inquisitor"), GLProgressScore.RelicSeven, out progressList, progressList);

            List<string> nonSpecificRequirement = new List<string>()
            {
                second,
                fifth,
                seventh,
                eighth,
                ninth
            };

            if (gi == "100" && nonSpecificRequirement.Where(x => x == "100").Count() > 3)
                ready = true;

            return HTMLConstructor.AddTableData(new string[] { player.PlayerData.Name, second, fifth, seventh, eighth, ninth, gi });
        }

        /// <summary>
        /// Method to determine GL Rey progress
        /// </summary>
        /// <param name="playerName"></param>
        /// <returns></returns>
        private string GetGLReyProgressForPlayer(Player player)
        {
            List<decimal> progressList = new List<decimal>();
            string scavRey = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Rey (Scavenger)"), GLProgressScore.RelicSeven, out progressList, progressList);
            string jtr = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Rey (Jedi Training)"), GLProgressScore.RelicSeven, out progressList, progressList);
            string finn = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Finn"), GLProgressScore.RelicFive, out progressList, progressList);
            string rhFinn = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Resistance Hero Finn"), GLProgressScore.RelicFive, out progressList, progressList);
            string poe = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Poe Dameron"), GLProgressScore.RelicFive, out progressList, progressList);
            string rhPoe = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Resistance Hero Poe"), GLProgressScore.RelicFive, out progressList, progressList);
            string holdo = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Amilyn Holdo"), GLProgressScore.RelicFive, out progressList, progressList);
            string rose = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Rose Tico"), GLProgressScore.RelicFive, out progressList, progressList);
            string resTrooper = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Resistance Trooper"), GLProgressScore.RelicFive, out progressList, progressList);
            string resPilot = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Resistance Pilot"), GLProgressScore.RelicThree, out progressList, progressList);
            string bbEight = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "BB-8"), GLProgressScore.RelicSeven, out progressList, progressList);
            string vetChewie = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Veteran Smuggler Chewbacca"), GLProgressScore.ThreeStar, out progressList, progressList);
            string raddus = CalculatePercentProgressForToon(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Raddus"), GLProgressScore.FiveStar, out progressList, progressList);

            var averageProgress = Math.Round(progressList.Average(), 2);

            m_glCharacterProgressList.FirstOrDefault(a => a.PlayerName == player.PlayerData.Name).ReyOverallProgress = averageProgress.ToString();

            if (averageProgress != 100)
                return HTMLConstructor.AddTableData(new string[] { player.PlayerData.Name, jtr, finn, resTrooper, scavRey, resPilot, poe, rhFinn, holdo, rose, rhPoe, bbEight, vetChewie, raddus });
            else
                return "";
        }

        /// <summary>
        /// Method to calculate the percent of progress towards a toon
        /// </summary>
        /// <param name="playerUnit">The toon to calculate against</param>
        /// <param name="maxPoints">Max points of progress for the toon</param>
        /// <param name="progressList">The overall progresss</param>
        /// <param name="currentList">The current snapsho of the overall progress</param>
        /// <returns></returns>
        private string CalculatePercentProgressForToon(PlayerUnit playerUnit, GLProgressScore score, out List<decimal> progressList, List<decimal> currentList)
        {
            progressList = currentList;            
            
            if (playerUnit == null)
            {
                progressList.Add(Convert.ToDecimal(0.0));
                return "0";
            }

            int relicPoints = 0;
            if (playerUnit.UnitData.UnitType == CombatType.Toon)
            {
                switch (playerUnit.UnitData.RelicTier)
                {
                    case 1:
                        relicPoints = 1;
                        break;
                    case 2:
                        relicPoints = 3;
                        break;
                    case 3:
                        relicPoints = 6;
                        break;
                    case 4:
                        relicPoints = 10;
                        break;
                    case 5:
                        relicPoints = 15;
                        break;
                    case 6:
                        relicPoints = 21;
                        break;
                    case 7:
                        relicPoints = 28;
                        break;
                    case 8:
                        relicPoints = 36;
                        break;
                    case 9:
                        relicPoints = 45;
                        break;
                }
            }

            int rarityPoints = 0;
            switch (playerUnit.UnitData.Rarity)
            {
                case 1:
                    rarityPoints = 1;
                    break;
                case 2:
                    rarityPoints = 3;
                    break;
                case 3:
                    rarityPoints = 6;
                    break;
                case 4:
                    rarityPoints = 10;
                    break;
                case 5:
                    rarityPoints = 15;
                    break;
                case 6:
                    rarityPoints = 21;
                    break;
                case 7:
                    rarityPoints = 35;
                    break;
            }

            var maxPoints = (int)score;

            //-1 because we want to count the number of gear pieces equipped not the current gear level ie Gear 1 if not the -1 would award 6 points
            int points = ((playerUnit.UnitData.GearLevel-1) * 6) 
                + (playerUnit.UnitData.GearLevel == 13 ? 6 : 0) //adjusts for all G12 equipped gear
                + (relicPoints)
                + (playerUnit.UnitData.UnitType == CombatType.Ship ? 0 : playerUnit.UnitData.Gear.Where(x => x.IsObtained && x.Id != "9999").Count())
                + (rarityPoints);

            progressList.Add(Math.Round(Decimal.Divide(points, maxPoints) * 100, 2) > 100 ? 100 : Math.Round(Decimal.Divide(points, maxPoints) * 100, 2));
            return Math.Round(Decimal.Divide(points, maxPoints) * 100, 2) > 100 ? "100" : Math.Round(Decimal.Divide(points, maxPoints) * 100, 0).ToString();
        }

        /// <summary>
        /// Method to generate list of players that unlocked
        /// </summary>
        /// <returns></returns>
        private async Task JourneyOrLegendaryUnlock()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<div id=\"toonunlock\">");
            sb.AppendLine(HTMLConstructor.SectionHeader("Journey/Legendary/Galactic Legend Unlock"));
            sb.AppendLine("This section highlights all players who have unlocked a Legendary, Journey or Galactic Legend toon/ship.");
            sb.AppendLine("</p>");

            var filteredUnlockList = m_dataBuilder.DifferencesGuildData.Players.SelectMany(x => x.Units)
                .Where(a => a.OldRarity == 0 && a.NewRarity != 0 && (
                a.Name == "Jedi Knight Revan" || 
                a.Name == "Darth Revan" ||
                a.Name == "Grand Master Yoda" ||
                a.Name == "C-3PO" ||
                a.Name == "Commander Luke Skywalker" ||
                a.Name == "Rey (Jedi Training)" ||
                a.Name == "Chewbacca" ||
                a.Name == "Grand Admiral Thrawn" ||
                a.Name == "Emperor Palpatine" ||
                a.Name == "BB-8" ||
                a.Name == "R2-D2" ||
                a.Name == "Padmé Amidala" ||
                a.Name == "Darth Malak" ||
                a.Name == "General Skywalker" ||
                a.Name == "Supreme Leader Kylo Ren" ||
                a.Name == "Rey" ||
                a.Name == "Jedi Master Luke Skywalker" ||
                a.Name == "Sith Eternal Emperor" ||
                a.Name == "Jedi Knight Luke Skywalker" ||
                a.Name == "The Mandalorian (Beskar Armor)" ||
                a.Name == "Jedi Master Kenobi" ||
                a.Name == "Lord Vader" ||
                a.Name == "Chimaera" ||
                a.Name == "Han's Millennium Falcon" ||
                a.Name == "Profundity" ||
                a.Name == "Leviathan" ||
                a.Name == "Starkiller" ||
                a.Name == "Jabba" ||
                a.Name == "Jedi Knight Cal Kestis" ||
                a.Name == "Dr. Aphra" ||
                a.Name == "Executor"
            ));

            StringBuilder unlockedRows = new StringBuilder();
            foreach(UnitDifference unitDifference in filteredUnlockList.OrderBy(a => a.PlayerName))
                unlockedRows.AppendLine(HTMLConstructor.AddTableData(new string[] { unitDifference.PlayerName, unitDifference.Name }));

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon" }, unlockedRows.ToString()));
            sb.AppendLine("<p/></div>");

            m_journeyOrLegendaryUnlock = sb.ToString();

            await Task.CompletedTask;
        }

        /// <summary>
        /// Method to generate applied zetas
        /// </summary>
        /// <returns></returns>
        private async Task ZetasApplied()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<div id=\"zetas\">");
            sb.AppendLine(HTMLConstructor.SectionHeader("Zetas Applied"));
            sb.AppendLine("This section highlights all of the toons that have been given zetas since the last snapshot.");
            sb.AppendLine("</p>");
            
            StringBuilder zetas = new StringBuilder();

            foreach (UnitDifference unitDifference in m_dataBuilder.DifferencesGuildData.Players
                .SelectMany(x => x.Units)
                .Where(x => x.NewZetas.Count() > 0)
                .OrderBy(a => a.PlayerName))
            {                
                zetas.AppendLine(HTMLConstructor.AddTableData(new string[] { unitDifference.PlayerName, unitDifference.Name, string.Join(",", unitDifference.NewZetas) }));
                m_reportSummary.TotalZetasApplied++;                
            }
                
            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon", "Zetas" }, zetas.ToString()));

            sb.AppendLine("<p/></div>");
                        
            m_zetasApplied = sb.ToString();

            await Task.CompletedTask;
        }

        /// <summary>
        /// Applied Omicrons to various toons
        /// </summary>
        /// <returns></returns>
        private async Task OmicronsApplied()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<div id=\"omicrons\">");
            sb.AppendLine(HTMLConstructor.SectionHeader("Omicrons Applied"));
            sb.AppendLine("This section highlights all of the toons that have been given omicrons since the last snapshot.");
            sb.AppendLine("</p>");

            StringBuilder omicrons = new StringBuilder();

            foreach (UnitDifference unitDifference in m_dataBuilder.DifferencesGuildData.Players
                .SelectMany(x => x.Units)
                .Where(x => x.NewOmicrons.Count() > 0)
                .OrderBy(a => a.PlayerName))
            {
                omicrons.AppendLine(HTMLConstructor.AddTableData(new string[] { unitDifference.PlayerName, unitDifference.Name, string.Join(",", unitDifference.NewOmicrons) }));
                m_reportSummary.TotalOmicronsApplied++;
            }

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon", "Omicrons" }, omicrons.ToString()));

            sb.AppendLine("<p/></div>");

            m_omicronsApplied = sb.ToString();

            await Task.CompletedTask;
        }

        /// <summary>
        /// Method to generate relic upgrdes
        /// </summary>
        /// <returns></returns>
        private async Task RelicTierDifferences()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<div id=\"relictiers\">");
            sb.AppendLine(HTMLConstructor.SectionHeader("Relic Tier Upgrades"));
            sb.AppendLine("This section highlights all of the toons that have been given upgrades to their relics.");
            sb.AppendLine("</p>");

            StringBuilder relicTiers = new StringBuilder();

            foreach (UnitDifference unitDifference in m_dataBuilder.DifferencesGuildData.Players
                .SelectMany(x => x.Units)
                .Where(x => x.OldRelicTier < x.NewRelicTier)
                .OrderBy(a => a.PlayerName))
            {
                relicTiers.AppendLine(HTMLConstructor.AddTableData(new string[] { unitDifference.PlayerName, unitDifference.Name, $"{unitDifference.OldRelicTier} > {unitDifference.NewRelicTier}" }));
                m_reportSummary.TotalRelicLevelsIncreased = m_reportSummary.TotalRelicLevelsIncreased + (unitDifference.NewRelicTier - unitDifference.OldRelicTier);
            }

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon", "Relic Tier Increase" }, relicTiers.ToString()));

            sb.AppendLine("<p/></div>");

            m_relicTiers = sb.ToString();

            await Task.CompletedTask;
        }

        /// <summary>
        /// Method to generate the top 20 toons of various stats
        /// </summary>
        /// <returns></returns>
        private async Task TopTwentySection()
        {
            StringBuilder sb = new StringBuilder();

            var guildUnitData = m_dataBuilder.NewGuildData.Players.SelectMany(x => x.PlayerUnits).Select(x => x.UnitData).Where(x => x.UnitType == CombatType.Toon);
            var healthUnits = guildUnitData.OrderByDescending(x => x.UnitStats.Health).Take(20);
            var protectionUnits = guildUnitData.OrderByDescending(x => x.UnitStats.Protection).Take(20);
            var tankiestUnits = guildUnitData.OrderByDescending(x => x.UnitStats.Thickness).Take(20);
            var speedUnits = guildUnitData.OrderByDescending(x => x.UnitStats.Speed).Take(20);
            var poUnits = guildUnitData.OrderByDescending(x => x.UnitStats.PhysicalOffense).Take(20);
            var soUnits = guildUnitData.OrderByDescending(x => x.UnitStats.SpecialOffense).Take(20);
            var pdUnits = guildUnitData.OrderByDescending(x => x.UnitStats.PhysicalDefense).Take(20);
            var sdUnits = guildUnitData.OrderByDescending(x => x.UnitStats.SpecialDefense).Take(20);
            var pccUnits = guildUnitData.OrderByDescending(x => x.UnitStats.PhysicalCriticalChance).Take(20);
            var sccUnits = guildUnitData.OrderByDescending(x => x.UnitStats.SpecialCriticalChance).Take(20);
            var potencyUnits = guildUnitData.OrderByDescending(x => x.UnitStats.Potency).Take(20);
            var tenacityUnits = guildUnitData.OrderByDescending(x => x.UnitStats.Tenacity).Take(20);

            sb.AppendLine("<div id=\"toptwenty\">");
            sb.AppendLine(HTMLConstructor.SectionHeader("Top 20 Stats"));
            sb.AppendLine("This section highlights the top 20 toons of various stats.  Only the stats that are affected by mods with multiple primary or secondary capabilities are highlighted here (IE Crit Damage only has a single primary stat increase, so its a easly obtained ceiling).");
            sb.AppendLine("<p/>");

            sb.AppendLine(HTMLConstructor.TableGroupStart());

            sb.Append(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon", "Health" }, GenerateTopStatForUnitsTable("Health", healthUnits), "Health"));

            sb.Append(HTMLConstructor.TableGroupNext());

            sb.Append(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon", "Protection" }, GenerateTopStatForUnitsTable("Protection", protectionUnits), "Protection"));

            sb.AppendLine(HTMLConstructor.TableGroupEnd());
            sb.AppendLine(HTMLConstructor.TableGroupStart());

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon", "Thickness" }, GenerateTopStatForUnitsTable("Thickness", tankiestUnits), "Tankiest"));

            sb.Append(HTMLConstructor.TableGroupNext());

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon", "Speed" }, GenerateTopStatForUnitsTable("Speed", speedUnits), "Speed"));

            sb.AppendLine(HTMLConstructor.TableGroupEnd());
            sb.AppendLine(HTMLConstructor.TableGroupStart());

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon", "PO" }, GenerateTopStatForUnitsTable("PhysicalOffense", poUnits), "Physical Offense"));

            sb.Append(HTMLConstructor.TableGroupNext());

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon", "SO" }, GenerateTopStatForUnitsTable("SpecialOffense", soUnits), "Special Offense"));

            sb.AppendLine(HTMLConstructor.TableGroupEnd());
            sb.AppendLine(HTMLConstructor.TableGroupStart());

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon", "PD" }, GenerateTopStatForUnitsTable("PhysicalDefense", pdUnits), "Physical Defense"));

            sb.Append(HTMLConstructor.TableGroupNext());

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon", "SD" }, GenerateTopStatForUnitsTable("SpecialDefense", sdUnits), "Special Defense"));

            sb.AppendLine(HTMLConstructor.TableGroupEnd());
            sb.AppendLine(HTMLConstructor.TableGroupStart());

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon", "PCC" }, GenerateTopStatForUnitsTable("PhysicalCriticalChance", pccUnits), "Physical Critical Chance"));

            sb.Append(HTMLConstructor.TableGroupNext());

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon", "SCC" }, GenerateTopStatForUnitsTable("SpecialCriticalChance", sccUnits), "Special Critical Chance"));

            sb.AppendLine(HTMLConstructor.TableGroupEnd());
            sb.AppendLine(HTMLConstructor.TableGroupStart());

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon", "Potency" }, GenerateTopStatForUnitsTable("Potency", potencyUnits), "Potency"));

            sb.Append(HTMLConstructor.TableGroupNext());

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon", "Tenacity" }, GenerateTopStatForUnitsTable("Tenacity", tenacityUnits), "Tenacity"));

            sb.AppendLine(HTMLConstructor.TableGroupEnd());
            sb.AppendLine("<p/></div>");

            m_topTwentySection = sb.ToString();

            await Task.CompletedTask;
        }

        private async Task CompileDatacronStats()
        {
            StringBuilder sb = new StringBuilder();

            var guildDatacrons = m_dataBuilder.NewGuildData.Players.SelectMany(x => x.Datacrons);

            //Ability of null means its not a alignment/faction/toon ability granted by the datacron
            var statNames = guildDatacrons.SelectMany(x => x.Tiers).Where(x => x.Ability == null).Select(x => x.StatName).Distinct().OrderBy(x => x);

            sb.AppendLine("<div id=\"datacronstats\">");
            sb.AppendLine(HTMLConstructor.SectionHeader("Top Datacon stats"));
            sb.AppendLine("This section highlights the top 25 of a given stat across all datacrons. This does not assess the unique abilites granted at lvl 3,6 and 9.");
            sb.AppendLine("<p/>");

            sb.AppendLine(HTMLConstructor.TableGroupStart());

            var makeNextGroup = true;
            foreach(var statName in statNames)
            {
                var datacronsWithStat = guildDatacrons
                    .Where(x => x.Tiers.Any(y => y.StatName == statName))
                    .OrderByDescending(x => x.Tiers.Where(y => y.StatName == statName).Sum(y => y.StatValue))
                    .Take(25);

                sb.Append(HTMLConstructor.AddTable(new string[] { "Player Name", "Datacron Name", "Value" }, GenerateTopStatForDatacronsTable(statName, datacronsWithStat), statName));

                if(makeNextGroup)
                {
                    sb.Append(HTMLConstructor.TableGroupNext());
                    makeNextGroup = false;
                }
                else
                {
                    sb.AppendLine(HTMLConstructor.TableGroupEnd());
                    sb.AppendLine(HTMLConstructor.TableGroupStart());
                    makeNextGroup = true;
                }
            }

            sb.AppendLine(HTMLConstructor.TableGroupEnd());
            sb.AppendLine("<p/></div>");

            m_datacronSection = sb.ToString();

            await Task.CompletedTask;
        }

        /// <summary>
        /// Method to generate 7* characters
        /// </summary>
        /// <returns></returns>
        private async Task SevenStarSection()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<div id=\"sevenstar\">");
            sb.AppendLine(HTMLConstructor.SectionHeader("Seven Stars"));
            sb.AppendLine("This section highlights all of the toons that have been 7*'ed since the last snapshot.");
            sb.AppendLine("</p>");
           
            StringBuilder units = new StringBuilder();

            foreach (UnitDifference unit in m_dataBuilder.DifferencesGuildData.Players
                .SelectMany(x => x.Units)
                .Where(x => x.OldRarity < 7 && x.NewRarity == 7 && x.UnitType == CombatType.Toon)
                .OrderBy(a => a.PlayerName))
            {
                units.AppendLine(HTMLConstructor.AddTableData(new string[] { unit.PlayerName, unit.Name }));
                m_reportSummary.TotalSevenStarToons++;
            }

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon" }, units.ToString()));
                        
            sb.AppendLine("<p/></div><div>");

            sb.AppendLine("This section highlights all of the ships that have been 7*'ed since the last snapshot.");

            StringBuilder ships = new StringBuilder();
            foreach (UnitDifference unit in m_dataBuilder.DifferencesGuildData.Players
                .SelectMany(x => x.Units)
                .Where(x => x.OldRarity < 7 && x.NewRarity == 7 && x.UnitType == CombatType.Ship)
                .OrderBy(a => a.PlayerName))
            {
                ships.AppendLine(HTMLConstructor.AddTableData(new string[] { unit.PlayerName, unit.Name }));
                m_reportSummary.TotalSevenStarShips++;
            }
            
            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Ship" }, ships.ToString()));

            sb.AppendLine("<p/></div>");

            m_sevenStarSection = sb.ToString();

            await Task.CompletedTask;
        }

        /// <summary>
        /// Method to generate player gp differences
        /// </summary>
        /// <returns></returns>
        private async Task PlayerGPDifferences()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<div id=\"playergpdiff\">");
            sb.AppendLine(HTMLConstructor.SectionHeader("Player GP Differences"));
            sb.AppendLine("This section goes over the Galatic Power (GP) differences for players between snapshots.  Here is the top ten players who have gained the most Galatic Power by total and by percentage from the previous snapshot.");
            sb.AppendLine("</p>");

            m_reportSummary.TotalGuildPowerIncrease = m_dataBuilder.DifferencesGuildData.GPDifference;

            StringBuilder playerGPDiff = new StringBuilder();
            foreach (PlayerDifference player in m_dataBuilder.DifferencesGuildData.Players.OrderByDescending(a => a.GPDifference).Take(10))
                playerGPDiff.AppendLine(HTMLConstructor.AddTableData(new string[] { player.Name, player.OldGP.ToString(), player.NewGP.ToString(), player.GPDifference.ToString() }));

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Previous Galatic Power", "New Galatic Power", "Galatic Power Increase" }, playerGPDiff.ToString()));

            sb.AppendLine("<p/>");

            StringBuilder playerGPPercentDiff = new StringBuilder();
            foreach (PlayerDifference player in m_dataBuilder.DifferencesGuildData.Players.OrderByDescending(a => a.GPPercentDifference).Take(10))
                playerGPPercentDiff.AppendLine(HTMLConstructor.AddTableData(new string[] { player.Name, player.OldGP.ToString(), player.NewGP.ToString(), player.GPPercentDifference.ToString() }));

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Previous Galatic Power", "New Galatic Power", "Galatic Power % Increase" }, playerGPPercentDiff.ToString()));
            sb.AppendLine("<p/></div>");

            m_playerGPDifferences = sb.ToString();

            await Task.CompletedTask;
        }

        /// <summary>
        /// Method to generate mod stats
        /// </summary>
        /// <returns></returns>
        private async Task CompileModStats()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<div id=\"modstats\">");
            sb.AppendLine(HTMLConstructor.SectionHeader("Top mods per secondary"));
            sb.AppendLine("Mods is where the geeks hang out. Here is the top 25 mods per secondary stat in the guild.");
            sb.AppendLine("<p/>");
                        
            var headers = new string[] { "Player Name", "Toon Name", "Tier", "Shape/Slot", "Set", "Primary Stat", "Slot 1", "Slot 2", "Slot 3", "Slot 4" };

            sb.AppendLine("<b>Speed:</b>");

            sb.AppendLine(HTMLConstructor.AddTable(headers, GetModListForStat("Speed", 25)));

            sb.AppendLine("</p style=\"page-break-after: always\">");
            sb.AppendLine("<b>Health:</b>");

            sb.AppendLine(HTMLConstructor.AddTable(headers, GetModListForStat("Health", 25)));

            sb.AppendLine("</p style=\"page-break-after: always\">");
            sb.AppendLine("<b>Health %:</b>");

            sb.AppendLine(HTMLConstructor.AddTable(headers, GetModListForStat("Health %", 25)));

            sb.AppendLine("</p style=\"page-break-after: always\">");
            sb.AppendLine("<b>Protection:</b>");

            sb.AppendLine(HTMLConstructor.AddTable(headers, GetModListForStat("Protection", 25)));

            sb.AppendLine("</p style=\"page-break-after: always\">");
            sb.AppendLine("<b>Protection %:</b>");

            sb.AppendLine(HTMLConstructor.AddTable(headers, GetModListForStat("Protection %", 25)));

            sb.AppendLine("</p style=\"page-break-after: always\">");
            sb.AppendLine("<b>Defense:</b>");

            sb.AppendLine(HTMLConstructor.AddTable(headers, GetModListForStat("Defense", 25)));

            sb.AppendLine("</p style=\"page-break-after: always\">");
            sb.AppendLine("<b>Defense %:</b>");

            sb.AppendLine(HTMLConstructor.AddTable(headers, GetModListForStat("Defense %", 25)));

            sb.AppendLine("</p style=\"page-break-after: always\">");
            sb.AppendLine("<b>Offense:</b>");

            sb.AppendLine(HTMLConstructor.AddTable(headers, GetModListForStat("Offense", 25)));

            sb.AppendLine("</p style=\"page-break-after: always\">");
            sb.AppendLine("<b>Offense %:</b>");

            sb.AppendLine(HTMLConstructor.AddTable(headers, GetModListForStat("Offense %", 25)));

            sb.AppendLine("</p style=\"page-break-after: always\">");
            sb.AppendLine("<b>Critical Chance:</b>");

            sb.AppendLine(HTMLConstructor.AddTable(headers, GetModListForStat("Critical Chance %", 25)));

            sb.AppendLine("</p style=\"page-break-after: always\">");
            sb.AppendLine("<b>Tenacity:</b>");

            sb.AppendLine(HTMLConstructor.AddTable(headers, GetModListForStat("Tenacity %", 25)));

            sb.AppendLine("</p style=\"page-break-after: always\">");
            sb.AppendLine("<b>Potency:</b>");

            sb.AppendLine(HTMLConstructor.AddTable(headers, GetModListForStat("Potency %", 25)));

            sb.AppendLine("<p/></div>");

            m_modStats = sb.ToString();

            await Task.CompletedTask;
        }

        private string GenerateTopStatForUnitsTable(string statName, IEnumerable<UnitData> unitDatas)
        {
            StringBuilder sb = new StringBuilder();

            foreach (UnitData unit in unitDatas)
            {
                List<string> propertyValues = new List<string>();

                propertyValues.Add(unit.PlayerName);
                propertyValues.Add(unit.Name);
                propertyValues.Add(unit.UnitStats.GetType().GetProperty(statName).GetValue(unit.UnitStats, null).ToString());

                sb.AppendLine(HTMLConstructor.AddTableData(propertyValues.ToArray()));
            }

            return sb.ToString();
        }

        private string GenerateTopStatForDatacronsTable(string statName, IEnumerable<Datacron> datacrons)
        {
            StringBuilder sb = new StringBuilder();

            foreach (Datacron datacron in datacrons)
            {
                List<string> propertyValues = new List<string>();

                propertyValues.Add(datacron.PlayerName);
                propertyValues.Add(datacron.Name);
                propertyValues.Add(datacron.Tiers.Where(x => x.StatName == statName).Sum(x => Math.Round(x.StatValue * 100, 2)).ToString());

                sb.AppendLine(HTMLConstructor.AddTableData(propertyValues.ToArray()));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Compiles a full list of mod data for a specific secondary stat by the top x
        /// </summary>
        /// <param name="stat">Secondary stat to pull</param>
        /// <param name="modList">Full List of mods</param>
        /// <param name="returnCount">The number of results to return</param>
        /// <returns></returns>
        private string GetModListForStat(string stat, int returnCount)
        {
            StringBuilder topMods = new StringBuilder();

            var filteredModList2 = m_dataBuilder.NewGuildData.Players
                .SelectMany(x => x.Mods)
                .SelectMany(x => x.SecondaryStats)
                .Select(x => x.Name).Distinct();

            var filteredModList = m_dataBuilder.NewGuildData.Players
                .SelectMany(x => x.Mods)
                .Where(x => x.SecondaryStats.Any(y => y.Name == stat))
                .OrderByDescending(x => x.SecondaryStats.Where(y => y.Name == stat).Max(y => y.Value))
                .Take(returnCount);

            //Bold highlighted stat, add mod slot(shape)
            foreach (var topMod in filteredModList)
                topMods.AppendLine(HTMLConstructor.AddTableData(new string[] 
                { 
                    topMod.PlayerName, 
                    topMod.UnitName, 
                    topMod.Rarity, 
                    topMod.Slot,
                    topMod.Set, 
                    topMod.PrimaryModData.Name,
                    GetModSecondaryRowData(topMod.SecondaryStats, stat, 0),
                    GetModSecondaryRowData(topMod.SecondaryStats, stat, 1),
                    GetModSecondaryRowData(topMod.SecondaryStats, stat, 2),
                    GetModSecondaryRowData(topMod.SecondaryStats, stat, 3)
                }));

            return topMods.ToString();
        }

        private string GetModSecondaryRowData(List<ModDetails> secondaryStats, string stat, int position)
        {
            if (secondaryStats.Count() < position + 1)
                return "";

            var secondaryStat = secondaryStats[position];

            return secondaryStat.Name == stat ? $"<b>({secondaryStat.Roll}) {secondaryStat.Name} {secondaryStat.DisplayValue}</b>" :
                $"({secondaryStat.Roll}) {secondaryStat.Name} {secondaryStat.DisplayValue}" == stat ?
                    $"<b>({secondaryStat.Roll}) {secondaryStat.Name} {secondaryStat.DisplayValue}</b>" :
                    $"({secondaryStat.Roll}) {secondaryStat.Name} {secondaryStat.DisplayValue}";
        }

        private string GetSpeedForUnit(UnitData unitData) => unitData == null ? "0" : unitData.UnitStats.Speed.ToString();

        /// <summary>
        /// Uitilizing this method to help determine which of the method calls take the longest to process so I can call the longer ones sooner
        /// </summary>
        /// <param name="methodToInvoke"></param>
        /// <param name="classToInvoke"></param>
        /// <returns></returns>
        public async Task InvokeAsyncTask(string methodToInvoke, string classToInvoke = null)
        {
            try
            {
                //Invoke the method passed in
                if(String.IsNullOrEmpty(classToInvoke))
                    Type.GetType(this.ToString()).InvokeMember(methodToInvoke, BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance, null, this, null);
                else
                    Type.GetType(classToInvoke).InvokeMember(methodToInvoke, BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance, null, Type.GetType(classToInvoke), null);
#if DEBUG
                Stopwatch watch = new Stopwatch();
                watch.Start();
#endif

#if DEBUG
                watch.Stop();
                SWGOHMessageSystem.OutputMessage($"Task {methodToInvoke} has completed in {watch.ElapsedMilliseconds} milliseconds and in {watch.ElapsedTicks} ticks");
#endif
            }
            catch(Exception e)
            {
                SWGOHMessageSystem.OutputMessage($"An error has occurred trying to run the task {methodToInvoke} error: {e.Message}");
            }
            await Task.CompletedTask;
        }
    }
}