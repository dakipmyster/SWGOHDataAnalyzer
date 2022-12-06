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
        string m_gearTwelveToons;
        string m_gearThirteenToons;
        string m_relicTiers;
        string m_zetasApplied;
        string m_omicronsApplied;
        string m_journeyOrLegendaryUnlock;
        string m_journeyPrepared;
        string m_detailedData;
        string m_characterHighlight;
        string m_introduction;
        string m_toonName;
        string m_fileName;
        string m_glProgress;
        string m_guildFocusProgress;
        string m_modStats;
        bool m_isSimpleReport;

        List<GLCharacterProgress> m_glCharacterProgressList;
        ReportSummary m_reportSummary;

        /// <summary>
        /// Constructor
        /// </summary>
        public ReportBuilder()
        {
            m_dataBuilder = new DataBuilder();
            m_isSimpleReport = false;
            m_reportSummary = new ReportSummary();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fileName">Filename of the report</param>
        /// <param name="characterName">Character to highlight</param>
        public ReportBuilder(string fileName, string characterName)
        {
            m_dataBuilder = new DataBuilder();
            m_fileName = fileName;
            m_toonName = characterName;
            m_isSimpleReport = true;
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
            m_toonName = SWGOHMessageSystem.InputMessage("Enter in the toon to highlight for the report. Multiple toons can be added via comma delimited. If there is not a toon you wish to highlight, press enter to continue.");
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
                tasks.Add(Task.Run(() => DetailedData()));
                tasks.Add(Task.Run(() => JourneyOrLegendaryUnlock()));
                tasks.Add(Task.Run(() => GalaticLegenedProgress()));
                tasks.Add(Task.Run(() => GuildFocusProgress()));
                tasks.Add(Task.Run(() => UnitGPDifferences()));
                tasks.Add(Task.Run(() => SevenStarSection()));
                tasks.Add(Task.Run(() => GearTwelveToons()));
                tasks.Add(Task.Run(() => GearThirteenToons()));
                tasks.Add(Task.Run(() => ZetasApplied()));
                tasks.Add(Task.Run(() => OmicronsApplied()));
                tasks.Add(Task.Run(() => PlayerGPDifferences()));
                tasks.Add(Task.Run(() => RelicTierDifferences()));                
            }
                                    
            tasks.Add(Task.Run(() => TopTwentySection()));            
            tasks.Add(Task.Run(() => CharacterHighlight()));
            
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
                pdfString.AppendLine(m_journeyPrepared);
                pdfString.AppendLine(m_characterHighlight);
            }            
            else
            {
                pdfString.AppendLine(m_introduction);
                pdfString.AppendLine(m_playerGPDifferences);
                pdfString.AppendLine(m_UnitGPDifferences);
                pdfString.AppendLine(m_topTwentySection);
                pdfString.AppendLine(m_sevenStarSection);
                pdfString.AppendLine(m_gearTwelveToons);
                pdfString.AppendLine(m_gearThirteenToons);
                pdfString.AppendLine(m_relicTiers);
                pdfString.AppendLine(m_zetasApplied);
                pdfString.AppendLine(m_omicronsApplied);
                pdfString.AppendLine(m_journeyOrLegendaryUnlock);
                pdfString.AppendLine(m_modStats);
                pdfString.AppendLine(m_guildFocusProgress);
                pdfString.AppendLine(m_glProgress);
                pdfString.AppendLine(m_characterHighlight);
                pdfString.AppendLine(m_detailedData);
            }

            pdfString.AppendLine(@"</body></html>");

            SWGOHMessageSystem.OutputMessage("Rendering Report....");

            string folderPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\SWGOHDataAnalyzer\\{m_fileName}.pdf";
            string folderPathHTML = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\SWGOHDataAnalyzer\\{m_fileName}.html";

            using (FileStream fs = File.Open(folderPath, FileMode.OpenOrCreate))
                HtmlConverter.ConvertToPdf(pdfString.ToString(),fs, new ConverterProperties());

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
                    $"{m_dataBuilder.NewGuildData.SnapshotDate.ToString("d")} - {m_dataBuilder.OldGuildData.SnapshotDate.ToString("d")}"
                ));
                sb.AppendLine("<p/>");
                sb.AppendLine("<p/>");
                sb.AppendLine(HTMLConstructor.SectionHeader("Contents"));
                sb.AppendLine("<ol type=\"1\"");
                sb.AppendLine("<li></li>");
                sb.AppendLine("<li><a href=\"#guildsummary\">Guild Summary</a></li>");
                sb.AppendLine("<li><a href=\"#toptwenty\">Top 20 Stats</a></li>");
                if (!String.IsNullOrEmpty(m_toonName)) sb.AppendLine($"<li><a href=\"#highlight\">Character Highlight: {m_toonName}</a></li>");
                sb.AppendLine("</ol></div>");
            }
            else
            {
                sb.AppendLine("<div>");
                sb.AppendLine(HTMLConstructor.ReportTitle(
                    m_dataBuilder.NewGuildData.GuildName,
                    $"{m_dataBuilder.NewGuildData.SnapshotDate.ToString("d")} - {m_dataBuilder.OldGuildData.SnapshotDate.ToString("d")}"
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
                sb.AppendLine("<li><a href=\"#geartwelve\">Gear 12 Toons</a></li>");
                sb.AppendLine("<li><a href=\"#gearthirteen\">Gear 13 Toons</a></li>");
                sb.AppendLine("<li><a href=\"#relictiers\">Relic Tier Upgrades</a></li>");
                sb.AppendLine("<li><a href=\"#zetas\">Zetas Applied</a></li>");
                sb.AppendLine("<li><a href=\"#omicrons\">Omicrons Applied</a></li>");
                sb.AppendLine("<li><a href=\"#toonunlock\">Journey/Legendary/Galactic Legend Unlocks</a></li>");
                sb.AppendLine("<li><a href=\"#modstats\">Top mods per secondary stat</a></li>");
                sb.AppendLine("<li><a href=\"#guildfocus\">Guild Focus Teams</a></li>");
                sb.AppendLine("<li><a href=\"#glprep\">Players prepping for Galatic Legends</a></li>");
                if (!String.IsNullOrEmpty(m_toonName)) sb.AppendLine($"<li><a href=\"#highlight\">Character Highlight: {m_toonName}</a></li>");
                sb.AppendLine("<li><a href=\"#details\">Data Details</a></li>");
                sb.AppendLine("</ol>");
                sb.AppendLine("<p/>");
                sb.AppendLine("<p/>");
                sb.AppendLine(HTMLConstructor.SectionHeader("Summary"));
                sb.AppendLine("Summary of guild progress:");
                sb.AppendLine("<p/>");
                sb.AppendLine($"Guild Power Increase: {m_reportSummary.TotalGuildPowerIncrease}");
                sb.AppendLine("<p/>");
                sb.AppendLine($"Total 7* Toons: {m_reportSummary.TotalSevenStarToons}");
                sb.AppendLine("<p/>");
                sb.AppendLine($"Total 7* Ships: {m_reportSummary.TotalSevenStarShips}");
                sb.AppendLine("<p/>");
                sb.AppendLine($"Total New G12 Toons: {m_reportSummary.TotalGearTwelveToons}");
                sb.AppendLine("<p/>");
                sb.AppendLine($"Total New G13 Toons: {m_reportSummary.TotalGearThirteenToons}");
                sb.AppendLine("<p/>");
                sb.AppendLine($"Sum of Relic Levels Applied: {m_reportSummary.TotalRelicLevelsIncreased}");
                sb.AppendLine("<p/>");
                sb.AppendLine($"Sum of Zeta Abilites Unlocked: {m_reportSummary.TotalZetasApplied}");
                sb.AppendLine("</div>");
                sb.AppendLine($"Sum of Omicron Abilites Unlocked: {m_reportSummary.TotalOmicronsApplied}");
                sb.AppendLine("</div>");

            }

            m_introduction = sb.ToString();

            await Task.CompletedTask;
        }

        /// <summary>
        /// Method to generate the character highlight
        /// </summary>
        /// <returns></returns>
        private async Task CharacterHighlight()
        {
            if (!String.IsNullOrEmpty(m_toonName))
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendLine("<div id=\"highlight\">");
                sb.AppendLine(HTMLConstructor.SectionHeader("Character Highlight"));
                sb.AppendLine($"This section goes over characters to highlight and will rotate every report.  The report takes the top 10 of each stat on the toon from the guild.");

                string[] toonNames = m_toonName.Split(',');
                foreach (string toonName in toonNames)
                {
                    sb.AppendLine($"{toonName.Trim()}");
                    sb.AppendLine("<p/>");

                    sb.AppendLine(HTMLConstructor.TableGroupStart());

                    sb.Append(HTMLConstructor.AddTable(new string[] { "Player Name", "Health" }, TakeTopXOfStatAndReturnTableData(10, "CurrentHealth", new string[] { "PlayerName", "CurrentHealth" }, toonName.Trim()), "Health"));

                    sb.Append(HTMLConstructor.TableGroupNext());

                    sb.Append(HTMLConstructor.AddTable(new string[] { "Player Name", "Protection" }, TakeTopXOfStatAndReturnTableData(10, "CurrentProtection", new string[] { "PlayerName", "CurrentProtection" }, toonName.Trim()), "Protection"));

                    sb.Append(HTMLConstructor.TableGroupNext());

                    sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Tankiest" }, TakeTopXOfStatAndReturnTableData(10, "CurrentTankiest", new string[] { "PlayerName", "CurrentTankiest" }, toonName.Trim()), "Tankiest"));

                    sb.AppendLine(HTMLConstructor.TableGroupEnd());
                    sb.AppendLine(HTMLConstructor.TableGroupStart());

                    sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Speed" }, TakeTopXOfStatAndReturnTableData(10, "CurrentSpeed", new string[] { "PlayerName", "CurrentSpeed" }, toonName.Trim()), "Speed"));

                    sb.Append(HTMLConstructor.TableGroupNext());

                    sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "PO" }, TakeTopXOfStatAndReturnTableData(10, "CurrentPhysicalOffense", new string[] { "PlayerName", "CurrentPhysicalOffense" }, toonName.Trim()), "Physical Offense"));

                    sb.Append(HTMLConstructor.TableGroupNext());

                    sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "SO" }, TakeTopXOfStatAndReturnTableData(10, "CurrentSpecialOffense", new string[] { "PlayerName", "CurrentSpecialOffense" }, toonName.Trim()), "Special Offense"));

                    sb.AppendLine(HTMLConstructor.TableGroupEnd());
                    sb.AppendLine(HTMLConstructor.TableGroupStart());

                    sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "PD" }, TakeTopXOfStatAndReturnTableData(10, "CurrentPhysicalDefense", new string[] { "PlayerName", "CurrentPhysicalDefense" }, toonName.Trim()), "Physical Defense"));

                    sb.Append(HTMLConstructor.TableGroupNext());

                    sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "SD" }, TakeTopXOfStatAndReturnTableData(10, "CurrentSpecialDefense", new string[] { "PlayerName", "CurrentSpecialDefense" }, toonName.Trim()), "Special Defense"));

                    sb.Append(HTMLConstructor.TableGroupNext());

                    sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "PCC" }, TakeTopXOfStatAndReturnTableData(10, "CurrentPhysicalCritChance", new string[] { "PlayerName", "CurrentPhysicalCritChance" }, toonName.Trim()), "Physical CC"));

                    sb.AppendLine(HTMLConstructor.TableGroupEnd());
                    sb.AppendLine(HTMLConstructor.TableGroupStart());

                    sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "SCC" }, TakeTopXOfStatAndReturnTableData(10, "CurrentSpecialCritChance", new string[] { "PlayerName", "CurrentSpecialCritChance" }, toonName.Trim()), "Special CC"));

                    sb.Append(HTMLConstructor.TableGroupNext());

                    sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Potency" }, TakeTopXOfStatAndReturnTableData(10, "CurrentPotency", new string[] { "PlayerName", "CurrentPotency" }, toonName.Trim()), "Potency"));

                    sb.Append(HTMLConstructor.TableGroupNext());

                    sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Tenacity" }, TakeTopXOfStatAndReturnTableData(10, "CurrentTenacity", new string[] { "PlayerName", "CurrentTenacity" }, toonName.Trim()), "Tenacity"));

                    sb.AppendLine(HTMLConstructor.TableGroupEnd());
                    sb.AppendLine(HTMLConstructor.TableGroupStart());

                    sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Galactic Power", "Gear Level", "Relic Tier" }, TakeTopXOfStatAndReturnTableData(10, "NewPower", new string[] { "PlayerName", "NewPower", "NewGearLevel", "NewRelicTier" }, toonName.Trim()), "Highest Galatic Power"));

                    sb.AppendLine(HTMLConstructor.TableGroupEnd());

                    sb.AppendLine("<p/></div>");
                }

                m_characterHighlight = sb.ToString();
            }

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
            
            foreach (UnitDifference unit in m_dataBuilder.DifferencesGuildData.Players.SelectMany(x => x.Units).OrderByDescending(a => a.GPDifference).Take(50))            
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
            StringBuilder prof = new StringBuilder();
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
                exec.AppendLine(GetExecProgressForPlayer(player));
                prof.AppendLine(GetProfProgressForPlayer(player));
                sk.AppendLine(GetStarKillerProgressForPlayer(player));
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
            sb.AppendLine("<b>Executor:</b>");

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Vader", "Piett", "Boba", "Dengar", "IG-88", "Bossk", "TFP", "TIE A", "Bomber", "HT", "SI", "IG2K", "TIEF", "RC" }, exec.ToString()));

            sb.AppendLine("</p>");
            sb.AppendLine("<b>Profundity:</b>");

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Rad", "Cass", "Dash", "Mon", "Bis", "Jyn", "Hera", "Outrider", "Cass U", "Bis U", "Wedge X", "Biggs X", "Rebel Y", "Ghost" }, prof.ToString()));

            sb.AppendLine("</p>");
            sb.AppendLine("<b>Starkiller:</b>");

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Dash", "Kyle", "Talon", "Mara" }, sk.ToString()));

            sb.AppendLine("</p>");
            sb.AppendLine("<b>Overall Progress:</b>");

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Rey", "SLKR", "JML", "SEE", "JMK", "LV", "Jabba", "Exec", "Prof", "SK" }, overall.ToString()));

            sb.AppendLine("<p/></div>");

            m_glProgress = sb.ToString();

            await Task.CompletedTask;
        }

        /// <summary>
        /// Method to collect data towards Guild goals
        /// </summary>
        /// <returns></returns>
        private async Task GuildFocusProgress()
        {
            StringBuilder sb = new StringBuilder();
            StringBuilder KAM = new StringBuilder();
            
            m_glCharacterProgressList = new List<GLCharacterProgress>();

            sb.AppendLine("<div id=\"guildfocus\">");
            sb.AppendLine(HTMLConstructor.SectionHeader("Team Prep"));
            sb.AppendLine("This section goes over all guild members and their progress towards a guild focused team.  100% for each toon indicates the player is at the goal level.");
            sb.AppendLine("<p>Calculations of progress is based on current gear level, gear pieces applied at current gear level, relic level and star level relative to the requirement for the toon.");

            foreach (Player player in m_dataBuilder.NewGuildData.Players.OrderBy(a => a.PlayerData.Name))
            {
                m_glCharacterProgressList.Add(new GLCharacterProgress() { PlayerName = player.PlayerData.Name });
                KAM.AppendLine(GetKAMProgressForPlayer(player));
            }

            sb.AppendLine("</p>");
            sb.AppendLine("<b>KAM Mission (R5 all around):</b>");

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Shaak Ti", "Rex", "5's", "Echo", "Arc" }, KAM.ToString()));

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
                playerProgress.Exec, 
                playerProgress.Profundity,
                playerProgress.StarKiller });
        }


        /// <summary>
        /// Method to determine GL Kylo progress
        /// </summary>
        /// <param name="playerName"></param>
        /// <returns></returns>
        private string GetGLLukeProgressForPlayer(Player player)
        {
            List<decimal> progressList = new List<decimal>();
            string big = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Biggs Darklighter"), GLProgressScore.RelicThree, out progressList, progressList);
            string c3p0 = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "C-3PO"), GLProgressScore.RelicFive, out progressList, progressList);
            string chew = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Chewbacca"), GLProgressScore.RelicSix, out progressList, progressList);
            string han = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Han Solo"), GLProgressScore.RelicSix, out progressList, progressList);
            string yoda = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Hermit Yoda"), GLProgressScore.RelicFive, out progressList, progressList);
            string jkl = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Jedi Knight Luke Skywalker"), GLProgressScore.RelicSeven, out progressList, progressList);
            string land = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Lando Calrissian"), GLProgressScore.RelicFive, out progressList, progressList);
            string mon = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Mon Mothma"), GLProgressScore.RelicFive, out progressList, progressList);
            string obi = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Obi-Wan Kenobi (Old Ben)"), GLProgressScore.RelicFive, out progressList, progressList);
            string leia = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Princess Leia"), GLProgressScore.RelicThree, out progressList, progressList);
            string r2d2 = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "R2-D2"), GLProgressScore.RelicSeven, out progressList, progressList);
            string jtr = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Rey (Jedi Training)"), GLProgressScore.RelicSeven, out progressList, progressList);
            string chwp = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Threepio & Chewie"), GLProgressScore.RelicFive, out progressList, progressList);
            string wed = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Wedge Antilles"), GLProgressScore.RelicThree, out progressList, progressList);
            string ywin = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Rebel Y-wing"), GLProgressScore.SixStar, out progressList, progressList);

            m_glCharacterProgressList.FirstOrDefault(a => a.PlayerName == player.PlayerData.Name).GLLukeOverallProgress = Math.Round(progressList.Average(), 2).ToString();

            return HTMLConstructor.AddTableData(new string[] { player.PlayerData.Name, big, c3p0, chew, han, yoda, jkl, land, mon, obi, leia, r2d2, jtr, chwp, wed, ywin });
        }

        /// <summary>
        /// Method to determine GL Kylo progress
        /// </summary>
        /// <param name="playerName"></param>
        /// <returns></returns>
        private string GetGLPalpProgressForPlayer(Player player)
        {
            List<decimal> progressList = new List<decimal>();
            string pie = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Admiral Piett"), GLProgressScore.RelicFive, out progressList, progressList);
            string star = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Colonel Starck"), GLProgressScore.RelicThree, out progressList, progressList);
            string dook = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Count Dooku"), GLProgressScore.RelicSix, out progressList, progressList);
            string maul = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Darth Maul"), GLProgressScore.RelicFour, out progressList, progressList);
            string sidi = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Darth Sidious"), GLProgressScore.RelicSeven, out progressList, progressList);
            string vad = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Darth Vader"), GLProgressScore.RelicSeven, out progressList, progressList);
            string kren = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Director Krennic"), GLProgressScore.RelicFour, out progressList, progressList);
            string palp = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Emperor Palpatine"), GLProgressScore.RelicSeven, out progressList, progressList);
            string veer = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "General Veers"), GLProgressScore.RelicThree, out progressList, progressList);
            string thra = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Grand Admiral Thrawn"), GLProgressScore.RelicSix, out progressList, progressList);
            string tark = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Grand Moff Tarkin"), GLProgressScore.RelicThree, out progressList, progressList);
            string jka = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Jedi Knight Anakin"), GLProgressScore.RelicSeven, out progressList, progressList);
            string rgua = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Royal Guard"), GLProgressScore.RelicThree, out progressList, progressList);
            string mara = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Sith Marauder"), GLProgressScore.RelicSeven, out progressList, progressList);
            string bomb = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Imperial TIE Bomber"), GLProgressScore.SixStar, out progressList, progressList);

            m_glCharacterProgressList.FirstOrDefault(a => a.PlayerName == player.PlayerData.Name).GLPalpOverallProgress = Math.Round(progressList.Average(), 2).ToString();

            return HTMLConstructor.AddTableData(new string[] { player.PlayerData.Name, pie, star, dook, maul, sidi, vad, kren, palp, veer, thra, tark, jka, rgua, mara, bomb});
        }

        /// <summary>
        /// Method to determine GL Kenobi progress
        /// </summary>
        /// <param name="playerName"></param>
        /// <returns></returns>
        private string GetGLKenobiProgressForPlayer(Player player)
        {
            List<decimal> progressList = new List<decimal>();
            string kenobi = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "General Kenobi"), GLProgressScore.RelicEight, out progressList, progressList);
            string mace = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Mace Windu"), GLProgressScore.RelicThree, out progressList, progressList);
            string aayla = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Aayla Secura"), GLProgressScore.RelicThree, out progressList, progressList);
            string katan = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Bo-Katan Kryze"), GLProgressScore.RelicFive, out progressList, progressList);
            string jinn = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Qui-Gon Jinn"), GLProgressScore.RelicThree, out progressList, progressList);
            string magna = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "IG-100 MagnaGuard"), GLProgressScore.RelicFive, out progressList, progressList);
            string clone = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Clone Sergeant - Phase I"), GLProgressScore.RelicFive, out progressList, progressList);
            string wat = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Wat Tambor"), GLProgressScore.RelicSeven, out progressList, progressList);
            string gg = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "General Grievous"), GLProgressScore.RelicSeven, out progressList, progressList);
            string cadbane = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Cad Bane"), GLProgressScore.RelicFive, out progressList, progressList);
            string cody = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "CC-2224 \"Cody\""), GLProgressScore.RelicFive, out progressList, progressList);
            string jango = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Jango Fett"), GLProgressScore.RelicSeven, out progressList, progressList);
            string shaak = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Shaak Ti"), GLProgressScore.RelicSeven, out progressList, progressList);
            string gmy = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Grand Master Yoda"), GLProgressScore.RelicEight, out progressList, progressList);
            string nego = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Negotiator"), GLProgressScore.SixStar, out progressList, progressList);

            m_glCharacterProgressList.FirstOrDefault(a => a.PlayerName == player.PlayerData.Name).GLKenobiProgress = Math.Round(progressList.Average(), 2).ToString();

            return HTMLConstructor.AddTableData(new string[] { player.PlayerData.Name, kenobi, mace, aayla, katan, jinn, magna, clone, wat, gg, cadbane, cody, jango, shaak, gmy, nego });
        }

        private string GetGLVaderProgressForPlayer(Player player)
        {
            List<decimal> progressList = new List<decimal>();
            string hunter = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Hunter"), GLProgressScore.RelicFive, out progressList, progressList);
            string tech = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Tech"), GLProgressScore.RelicFive, out progressList, progressList);
            string wrecker = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Wrecker"), GLProgressScore.RelicFive, out progressList, progressList);
            string tusken = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Tusken Raider"), GLProgressScore.RelicFive, out progressList, progressList);
            string padme = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Padmé Amidala"), GLProgressScore.RelicEight, out progressList, progressList);
            string embo = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Embo"), GLProgressScore.RelicFive, out progressList, progressList);
            string echo = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "CT-21-0408 \"Echo\""), GLProgressScore.RelicSeven, out progressList, progressList);
            string bbEcho = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Echo"), GLProgressScore.RelicFive, out progressList, progressList);
            string dooku = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Count Dooku"), GLProgressScore.RelicEight, out progressList, progressList);
            string zam = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Zam Wesell"), GLProgressScore.RelicSeven, out progressList, progressList);
            string tarkin = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Grand Moff Tarkin"), GLProgressScore.RelicSeven, out progressList, progressList);
            string arc = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "ARC Trooper"), GLProgressScore.RelicEight, out progressList, progressList);
            string gas = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "General Skywalker"), GLProgressScore.RelicEight, out progressList, progressList);
            string nute = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Nute Gunray"), GLProgressScore.RelicSeven, out progressList, progressList);
            string ywing = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "BTL-B Y-wing Starfighter"), GLProgressScore.SevenStar, out progressList, progressList);

            m_glCharacterProgressList.FirstOrDefault(a => a.PlayerName == player.PlayerData.Name).GLVaderProgress = Math.Round(progressList.Average(), 2).ToString();

            return HTMLConstructor.AddTableData(new string[] { player.PlayerData.Name, hunter, tech, wrecker, tusken, padme, embo, echo, bbEcho, dooku, zam, tarkin, arc, gas, nute, ywing});
        }

        private string GetGLJabbaProgressForPlayer(Player player)
        {
            List<decimal> progressList = new List<decimal>();
            string krrsantan = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Krrsantan"), GLProgressScore.RelicFive, out progressList, progressList);
            string han = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Han Solo"), GLProgressScore.RelicEight, out progressList, progressList);
            string gam = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Gamorrean Guard"), GLProgressScore.RelicThree, out progressList, progressList);
            string greedo = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Greedo"), GLProgressScore.RelicSix, out progressList, progressList);
            string outrider = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Outrider"), GLProgressScore.SevenStar, out progressList, progressList);
            string lando = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Skiff Guard (Lando Calrissian)"), GLProgressScore.RelicFive, out progressList, progressList);
            string luke = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Jedi Knight Luke Skywalker"), GLProgressScore.RelicSeven, out progressList, progressList);
            string jawa = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Jawa"), GLProgressScore.RelicThree, out progressList, progressList);
            string tuskan = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "URoRRuR'R'R"), GLProgressScore.RelicFour, out progressList, progressList);
            string c3p0 = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "C-3PO"), GLProgressScore.RelicSeven, out progressList, progressList);
            string leia = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Boushh (Leia Organa)"), GLProgressScore.RelicFive, out progressList, progressList);
            string sing = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Aurra Sing"), GLProgressScore.RelicSix, out progressList, progressList);
            string fennic = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Fennec Shand"), GLProgressScore.RelicSeven, out progressList, progressList);
            string boba = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Boba Fett"), GLProgressScore.RelicSeven, out progressList, progressList);
            string mob = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Mob Enforcer"), GLProgressScore.RelicThree, out progressList, progressList);

            m_glCharacterProgressList.FirstOrDefault(a => a.PlayerName == player.PlayerData.Name).GLJabbaProgress = Math.Round(progressList.Average(), 2).ToString();

            return HTMLConstructor.AddTableData(new string[] { player.PlayerData.Name, krrsantan, han, gam, greedo, outrider, lando, luke, jawa, tuskan, c3p0, leia, sing, fennic, boba, mob });
        }

        private string GetExecProgressForPlayer(Player player)
        {
            List<decimal> progressList = new List<decimal>();
            string vader = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Darth Vader"), GLProgressScore.RelicSeven, out progressList, progressList);
            string piett = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Admiral Piett"), GLProgressScore.RelicEight, out progressList, progressList);
            string boba = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Boba Fett"), GLProgressScore.RelicEight, out progressList, progressList);
            string dengar = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Dengar"), GLProgressScore.RelicFive, out progressList, progressList);
            string ig88 = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "IG-88"), GLProgressScore.RelicFive, out progressList, progressList);
            string bossk = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Bossk"), GLProgressScore.RelicFive, out progressList, progressList);
            string tfp = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "TIE Fighter Pilot"), GLProgressScore.RelicFive, out progressList, progressList);

            string advanced = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "TIE Advanced x1"), GLProgressScore.FourStar, out progressList, progressList);
            string bomber = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Imperial TIE Bomber"), GLProgressScore.FourStar, out progressList, progressList);
            string ht = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Hound's Tooth"), GLProgressScore.FourStar, out progressList, progressList);
            string slave = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Slave I"), GLProgressScore.FourStar, out progressList, progressList);
            string ig2000 = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "IG-2000"), GLProgressScore.FourStar, out progressList, progressList);
            string tie = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Imperial TIE Fighter"), GLProgressScore.FourStar, out progressList, progressList);
            string rc = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Razor Crest"), GLProgressScore.FiveStar, out progressList, progressList);
            
            m_glCharacterProgressList.FirstOrDefault(a => a.PlayerName == player.PlayerData.Name).Exec = Math.Round(progressList.Average(), 2).ToString();

            return HTMLConstructor.AddTableData(new string[] { player.PlayerData.Name, vader, piett, boba, dengar, ig88, bossk, tfp, advanced, bomber, ht, slave, ig2000, tie, rc });
        }

        private string GetProfProgressForPlayer(Player player)
        {
            List<decimal> progressList = new List<decimal>();
            string raddus = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Admiral Raddus"), GLProgressScore.RelicNine, out progressList, progressList);
            string cassian = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Cassian Andor"), GLProgressScore.RelicEight, out progressList, progressList);
            string dash = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Dash Rendar"), GLProgressScore.RelicSeven, out progressList, progressList);
            string mon = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Mon Mothma"), GLProgressScore.RelicFive, out progressList, progressList);
            string bist = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Bistan"), GLProgressScore.RelicFive, out progressList, progressList);
            string jyn = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Jyn Erso"), GLProgressScore.RelicFive, out progressList, progressList);
            string hera = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Hera Syndulla"), GLProgressScore.RelicFive, out progressList, progressList);

            string outrider = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Outrider"), GLProgressScore.SevenStar, out progressList, progressList);
            string cassU = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Cassian's U-wing"), GLProgressScore.SevenStar, out progressList, progressList);
            string bisU = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Bistan's U-wing"), GLProgressScore.SevenStar, out progressList, progressList);
            string wedgeX = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Wedge Antilles's X-wing"), GLProgressScore.SevenStar, out progressList, progressList);
            string biggsX = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Biggs Darklighter's X-wing"), GLProgressScore.SevenStar, out progressList, progressList);
            string rebelY = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Rebel Y-wing"), GLProgressScore.SevenStar, out progressList, progressList);
            string ghost = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Ghost"), GLProgressScore.SevenStar, out progressList, progressList);

            m_glCharacterProgressList.FirstOrDefault(a => a.PlayerName == player.PlayerData.Name).Profundity = Math.Round(progressList.Average(), 2).ToString();

            return HTMLConstructor.AddTableData(new string[] { player.PlayerData.Name, raddus, cassian, dash, mon, bist, jyn, hera, outrider, cassU, bisU, wedgeX, biggsX, rebelY, ghost });
        }

        private string GetStarKillerProgressForPlayer(Player player)
        {
            List<decimal> progressList = new List<decimal>();
            string dash = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Dash Rendar"), GLProgressScore.RelicFive, out progressList, progressList);
            string kyle = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Kyle Katarn"), GLProgressScore.RelicFive, out progressList, progressList);
            string talon = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Darth Talon"), GLProgressScore.RelicFive, out progressList, progressList);
            string mara = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Mara Jade, The Emperor's Hand"), GLProgressScore.RelicFive, out progressList, progressList);

            m_glCharacterProgressList.FirstOrDefault(a => a.PlayerName == player.PlayerData.Name).StarKiller = Math.Round(progressList.Average(), 2).ToString();

            return HTMLConstructor.AddTableData(new string[] { player.PlayerData.Name, dash, kyle, talon, mara });
        }

        /// <summary>
        /// Method to determine GL Kylo progress
        /// </summary>
        /// <param name="playerName"></param>
        /// <returns></returns>
        private string GetGLKyloProgressForPlayer(Player player)
        {
            List<decimal> progressList = new List<decimal>();
            string kru = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Kylo Ren (Unmasked)"), GLProgressScore.RelicSeven, out progressList, progressList);
            string fos = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "First Order Stormtrooper"), GLProgressScore.RelicFive, out progressList, progressList);
            string foo = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "First Order Officer"), GLProgressScore.RelicFive, out progressList, progressList);
            string kyloRen = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Kylo Ren"), GLProgressScore.RelicSeven, out progressList, progressList);
            string phasma = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Captain Phasma"), GLProgressScore.RelicFive, out progressList, progressList);
            string fox = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "First Order Executioner"), GLProgressScore.RelicFive, out progressList, progressList);
            string vetHan = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Veteran Smuggler Han Solo"), GLProgressScore.RelicThree, out progressList, progressList);
            string sithTroop = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Sith Trooper"), GLProgressScore.RelicFive, out progressList, progressList);
            string fosftp = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "First Order SF TIE Pilot"), GLProgressScore.RelicThree, out progressList, progressList);
            string hux = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "General Hux"), GLProgressScore.RelicFive, out progressList, progressList);
            string fotp = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "First Order TIE Pilot"), GLProgressScore.RelicThree, out progressList, progressList);
            string palp = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Emperor Palpatine"), GLProgressScore.RelicSeven, out progressList, progressList);
            string finalizer = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Finalizer"), GLProgressScore.FiveStar, out progressList, progressList);

            m_glCharacterProgressList.FirstOrDefault(a => a.PlayerName == player.PlayerData.Name).SLKROverallProgress = Math.Round(progressList.Average(), 2).ToString();

            return HTMLConstructor.AddTableData(new string[] { player.PlayerData.Name, kru, fos, foo, kyloRen, phasma, fox, vetHan, sithTroop, fosftp, hux, fotp, palp, finalizer });
        }

        /// <summary>
        /// Method to determine KAM progress for a player
        /// </summary>
        /// <param name="playerName"></param>
        /// <returns></returns>
        private string GetKAMProgressForPlayer(Player player)
        {
            List<decimal> progressList = new List<decimal>();
            string shaak = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Shaak Ti"), GLProgressScore.RelicFive, out progressList, progressList);
            string rex = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "CT-7567 \"Rex\""), GLProgressScore.RelicFive, out progressList, progressList);
            string fives = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "CT-5555 \"Fives\""), GLProgressScore.RelicFive, out progressList, progressList);
            string echo = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "CT-21-0408 \"Echo\""), GLProgressScore.RelicFive, out progressList, progressList);
            string arc = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "ARC Trooper"), GLProgressScore.RelicFive, out progressList, progressList);

            return HTMLConstructor.AddTableData(new string[] { player.PlayerData.Name, shaak, rex, fives, echo, arc });
        }

        /// <summary>
        /// Method to determine GL Rey progress
        /// </summary>
        /// <param name="playerName"></param>
        /// <returns></returns>
        private string GetGLReyProgressForPlayer(Player player)
        {
            List<decimal> progressList = new List<decimal>();
            string scavRey = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Rey (Scavenger)"), GLProgressScore.RelicSeven, out progressList, progressList);
            string jtr = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Rey (Jedi Training)"), GLProgressScore.RelicSeven, out progressList, progressList);
            string finn = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Finn"), GLProgressScore.RelicFive, out progressList, progressList);
            string rhFinn = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Resistance Hero Finn"), GLProgressScore.RelicFive, out progressList, progressList);
            string poe = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Poe Dameron"), GLProgressScore.RelicFive, out progressList, progressList);
            string rhPoe = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Resistance Hero Poe"), GLProgressScore.RelicFive, out progressList, progressList);
            string holdo = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Amilyn Holdo"), GLProgressScore.RelicFive, out progressList, progressList);
            string rose = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Rose Tico"), GLProgressScore.RelicFive, out progressList, progressList);
            string resTrooper = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Resistance Trooper"), GLProgressScore.RelicFive, out progressList, progressList);
            string resPilot = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Resistance Pilot"), GLProgressScore.RelicThree, out progressList, progressList);
            string bbEight = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "BB-8"), GLProgressScore.RelicSeven, out progressList, progressList);
            string vetChewie = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Veteran Smuggler Chewbacca"), GLProgressScore.ThreeStar, out progressList, progressList);
            string raddus = CalculatePercentProgressForGL(player.PlayerUnits.FirstOrDefault(a => a.UnitData.Name == "Raddus"), GLProgressScore.FiveStar, out progressList, progressList);

            m_glCharacterProgressList.FirstOrDefault(a => a.PlayerName == player.PlayerData.Name).ReyOverallProgress = Math.Round(progressList.Average(), 2).ToString();

            return HTMLConstructor.AddTableData(new string[] { player.PlayerData.Name, jtr, finn, resTrooper, scavRey, resPilot, poe, rhFinn, holdo, rose, rhPoe, bbEight, vetChewie, raddus });
        }

        /// <summary>
        /// Method to calculate the percent of progress towards a toon
        /// </summary>
        /// <param name="playerUnit">The toon to calculate against</param>
        /// <param name="maxPoints">Max points of progress for the toon</param>
        /// <param name="progressList">The overall progresss</param>
        /// <param name="currentList">The current snapsho of the overall progress</param>
        /// <returns></returns>
        private string CalculatePercentProgressForGL(PlayerUnit playerUnit, GLProgressScore score, out List<decimal> progressList, List<decimal> currentList)
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

            var maxPoints = (int)score;

            //-1 because we want to count the number of gear pieces equipped not the current gear level ie Gear 1 if not the -1 would award 6 points
            int points = ((playerUnit.UnitData.GearLevel-1) * 6) 
                + relicPoints 
                + playerUnit.UnitData.UnitType == CombatType.Ship ? 0 : playerUnit.UnitData.Gear.Count()
                + playerUnit.UnitData.Rarity;

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
        /// Method to generate g13 toons
        /// </summary>
        /// <returns></returns>
        private async Task GearThirteenToons()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<div id=\"gearthirteen\">");
            sb.AppendLine(HTMLConstructor.SectionHeader("Gear 13 Toons"));
            sb.AppendLine("This section highlights all of the toons that have been geared to 13 since the last snapshot.");
            sb.AppendLine("</p>");

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon" }, GetAllToonsOfGearLevelDifference(13)));

            sb.AppendLine("<p/></div>");
                        
            m_gearThirteenToons = sb.ToString();

            await Task.CompletedTask;
        }

        /// <summary>
        /// Method to generate g12 toons
        /// </summary>
        /// <returns></returns>
        private async Task GearTwelveToons()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<div id=\"geartwelve\">");
            sb.AppendLine(HTMLConstructor.SectionHeader("Gear 12 Toons"));
            sb.AppendLine("This section highlights all of the toons that have been geared to 12 since the last snapshot.");
            sb.AppendLine("</p>");

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon" }, GetAllToonsOfGearLevelDifference(12)));

            sb.AppendLine("<p/></div>");

            m_gearTwelveToons = sb.ToString();

            await Task.CompletedTask;
        }

        /// <summary>
        /// Method to generate the top 20 toons of various stats
        /// </summary>
        /// <returns></returns>
        private async Task TopTwentySection()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<div id=\"toptwenty\">");
            sb.AppendLine(HTMLConstructor.SectionHeader("Top 20 Stats"));
            sb.AppendLine("This section highlights the top 20 toons of various stats.  Only the stats that are affected by mods with multiple primary or secondary capabilities are highlighted here (IE Crit Damage only has a single primary stat increase, so its a easly obtained ceiling).");
            sb.AppendLine("<p/>");

            sb.AppendLine(HTMLConstructor.TableGroupStart());

            sb.Append(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon", "Health" }, TakeTopXOfStatAndReturnTableData(20, "CurrentHealth", new string[] { "PlayerName", "UnitName", "CurrentHealth" }), "Health"));

            sb.Append(HTMLConstructor.TableGroupNext());

            sb.Append(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon", "Protection" }, TakeTopXOfStatAndReturnTableData(20, "CurrentProtection", new string[] { "PlayerName", "UnitName", "CurrentProtection" }), "Protection"));

            sb.AppendLine(HTMLConstructor.TableGroupEnd());
            sb.AppendLine(HTMLConstructor.TableGroupStart());

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon", "Tankiest" }, TakeTopXOfStatAndReturnTableData(20, "CurrentTankiest", new string[] { "PlayerName", "UnitName", "CurrentTankiest" }), "Tankiest"));

            sb.Append(HTMLConstructor.TableGroupNext());

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon", "Speed" }, TakeTopXOfStatAndReturnTableData(20, "CurrentSpeed", new string[] { "PlayerName", "UnitName", "CurrentSpeed" }), "Speed"));

            sb.AppendLine(HTMLConstructor.TableGroupEnd());
            sb.AppendLine(HTMLConstructor.TableGroupStart());

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon", "PO" }, TakeTopXOfStatAndReturnTableData(20, "CurrentPhysicalOffense", new string[] { "PlayerName", "UnitName", "CurrentPhysicalOffense" }), "Physical Offense"));

            sb.Append(HTMLConstructor.TableGroupNext());

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon", "SO" }, TakeTopXOfStatAndReturnTableData(20, "CurrentSpecialOffense", new string[] { "PlayerName", "UnitName", "CurrentSpecialOffense" }), "Special Offense"));

            sb.AppendLine(HTMLConstructor.TableGroupEnd());
            sb.AppendLine(HTMLConstructor.TableGroupStart());

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon", "PD" }, TakeTopXOfStatAndReturnTableData(20, "CurrentPhysicalDefense", new string[] { "PlayerName", "UnitName", "CurrentPhysicalDefense" }), "Physical Defense"));

            sb.Append(HTMLConstructor.TableGroupNext());

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon", "SD" }, TakeTopXOfStatAndReturnTableData(20, "CurrentSpecialDefense", new string[] { "PlayerName", "UnitName", "CurrentSpecialDefense" }), "Special Defense"));

            sb.AppendLine(HTMLConstructor.TableGroupEnd());
            sb.AppendLine(HTMLConstructor.TableGroupStart());

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon", "PCC" }, TakeTopXOfStatAndReturnTableData(20, "CurrentPhysicalCritChance", new string[] { "PlayerName", "UnitName", "CurrentPhysicalCritChance" }), "Physical CC"));

            sb.Append(HTMLConstructor.TableGroupNext());

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon", "SCC" }, TakeTopXOfStatAndReturnTableData(20, "CurrentSpecialCritChance", new string[] { "PlayerName", "UnitName", "CurrentSpecialCritChance" }), "Special CC"));

            sb.AppendLine(HTMLConstructor.TableGroupEnd());
            sb.AppendLine(HTMLConstructor.TableGroupStart());

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon", "Potency" }, TakeTopXOfStatAndReturnTableData(20, "CurrentPotency", new string[] { "PlayerName", "UnitName", "CurrentPotency" }), "Potency"));

            sb.Append(HTMLConstructor.TableGroupNext());

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon", "Tenacity" }, TakeTopXOfStatAndReturnTableData(20, "CurrentTenacity", new string[] { "PlayerName", "UnitName", "CurrentTenacity" }), "Tenacity"));

            sb.AppendLine(HTMLConstructor.TableGroupEnd());
            sb.AppendLine("<p/></div>");

            m_topTwentySection = sb.ToString();

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
                units.AppendLine(HTMLConstructor.AddTableData(new string[] { unit.PlayerName, unit.Name }));
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

            sb.AppendLine(HTMLConstructor.AddTable(headers, GetModListForStat("Critical Chance", 25)));

            sb.AppendLine("</p style=\"page-break-after: always\">");
            sb.AppendLine("<b>Tenacity:</b>");

            sb.AppendLine(HTMLConstructor.AddTable(headers, GetModListForStat("Tenacity", 25)));

            sb.AppendLine("</p style=\"page-break-after: always\">");
            sb.AppendLine("<b>Potency:</b>");

            sb.AppendLine(HTMLConstructor.AddTable(headers, GetModListForStat("Potency", 25)));

            sb.AppendLine("<p/></div>");

            m_modStats = sb.ToString();

            await Task.CompletedTask;
        }

        /// <summary>
        /// Method to generate detailed data
        /// </summary>
        /// <returns></returns>
        private async Task DetailedData()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<div id=\"details\">");
            sb.AppendLine(HTMLConstructor.SectionHeader("Data Details"));
            sb.AppendLine("For those who are interested, here is some full table data that the stats refer to.");
            sb.AppendLine("Here is the full list of players and their Galatic Power differences.");
            sb.AppendLine("<p/>");
            
            StringBuilder detailedPlayerData = new StringBuilder();
            foreach (PlayerDifference player in m_dataBuilder.DifferencesGuildData.Players.OrderBy(a => a.Name).ToList())
                detailedPlayerData.AppendLine(HTMLConstructor.AddTableData(new string[] { player.Name, player.OldGP.ToString(), player.NewGP.ToString(), player.GPDifference.ToString(), player.GPPercentDifference.ToString() }));

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Previous Galatic Power", "New Galatic Power", "Galatic Power Increase", "Galatic Power % Increase" }, detailedPlayerData.ToString()));

            sb.AppendLine("</div><div>");
            sb.AppendLine("Here is the full list of toons within the guild, with their average stat in the guild and max.");

            StringBuilder detailedUnitDataStringBuilder = new StringBuilder();
            StringBuilder detailedUnitDataStatsStringBuilder = new StringBuilder();

            var unitNames = m_dataBuilder.NewGuildData.Players.SelectMany(x => x.PlayerUnits).Select(x => x.UnitData.Name).Distinct().OrderBy(b => b);
            var guildUnits = m_dataBuilder.NewGuildData.Players.SelectMany(x => x.PlayerUnits).Where(x => x.UnitData.UnitType == CombatType.Toon);

            foreach (var unitName in unitNames)
            {                
                detailedUnitDataStringBuilder.AppendLine(HTMLConstructor.AddTableData(new string[] {
                    unitName,
                    guildUnits.Where(b => b.UnitData.Name == unitName).Average(a => a.UnitData.Power).ToString("#."),
                    guildUnits.Where(b => b.UnitData.Name == unitName).Max(a => a.UnitData.Power).ToString(),
                    guildUnits.Where(b => b.UnitData.Name == unitName).Average(a => a.UnitData.GearLevel).ToString("#."),
                    guildUnits.Where(b => b.UnitData.Name == unitName).Max(a => a.UnitData.GearLevel).ToString(),                    
                    String.IsNullOrEmpty(guildUnits.Where(b => b.UnitData.Name == unitName).Average(a => a.UnitData.RelicTier).ToString("#.")) ? "0" : guildUnits.Where(b => b.UnitData.Name == unitName).Average(a => a.UnitData.RelicTier).ToString("#."),
                    guildUnits.Where(b => b.UnitData.Name == unitName).Max(a => a.UnitData.RelicTier).ToString(),
                }));

                detailedUnitDataStatsStringBuilder.AppendLine(HTMLConstructor.AddTableData(new string[]
                {
                    unitName,                    
                    guildUnits.Where(b => b.UnitData.Name == unitName).Average(a => a.UnitData.UnitStats.Health).ToString("#."),
                    guildUnits.Where(b => b.UnitData.Name == unitName).Max(a => a.UnitData.UnitStats.Health).ToString(),
                    guildUnits.Where(b => b.UnitData.Name == unitName).Average(a => a.UnitData.UnitStats.Protection).ToString("#."),
                    guildUnits.Where(b => b.UnitData.Name == unitName).Max(a => a.UnitData.UnitStats.Protection).ToString(),
                    guildUnits.Where(b => b.UnitData.Name == unitName).Average(a => a.UnitData.UnitStats.Speed).ToString("#."),
                    guildUnits.Where(b => b.UnitData.Name == unitName).Max(a => a.UnitData.UnitStats.Speed).ToString(),
                    guildUnits.Where(b => b.UnitData.Name == unitName).Average(a => a.UnitData.UnitStats.PhysicalOffense).ToString("#."),
                    guildUnits.Where(b => b.UnitData.Name == unitName).Max(a => a.UnitData.UnitStats.PhysicalOffense).ToString(),
                    guildUnits.Where(b => b.UnitData.Name == unitName).Average(a => a.UnitData.UnitStats.SpecialOffense).ToString("#."),
                    guildUnits.Where(b => b.UnitData.Name == unitName).Max(a => a.UnitData.UnitStats.SpecialOffense).ToString(),
                }));
            }

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Toon", "Avg GP", "Max GP", "Avg Gear Lvl", "Max Gear Lvl", "Avg Relic Tier", "Max Relic Tier" }, detailedUnitDataStringBuilder.ToString()));
            sb.AppendLine("</div>");
            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Toon", "Avg Health", "Max Health", "Avg Prot.", "Max Prot.", "Avg Speed", "Max Speed", "Avg PO", "Max PO", "Avg SO", "Max SO" }, detailedUnitDataStatsStringBuilder.ToString()));

            sb.AppendLine("</div>");

            m_detailedData = sb.ToString();

            await Task.CompletedTask;
        }

        /// <summary>
        /// Takes the top X toons of a stat and returns the result
        /// </summary>
        /// <param name="amount">Amount of results to return</param>
        /// <param name="stat">Stat to compare against</param>
        /// <param name="properties">Properties of the reflected object to pull data from</param>
        /// <param name="toonName">Character name to find data against</param>
        /// <returns>Table of data found for the passed in params</returns>
        private string TakeTopXOfStatAndReturnTableData(int amount, string stat, string toonName = null)
        {
            StringBuilder sb = new StringBuilder();

            IEnumerable<PlayerUnit> units;

            var guildUnits = m_dataBuilder.NewGuildData.Players.SelectMany(x => x.PlayerUnits);

            if (!String.IsNullOrEmpty(toonName))
                units = guildUnits.Where(b => b.UnitData.Name == toonName).OrderByDescending(a => a.UnitData.UnitStats.GetType().GetProperty(stat).GetValue(a.UnitData.UnitStats, null)).Take(amount);
            else
                units = guildUnits.OrderByDescending(a => a.UnitData.UnitStats.GetType().GetProperty(stat).GetValue(a.UnitData.UnitStats, null)).Take(amount);

            foreach (PlayerUnit unit in units)
            {
                List<string> propertyValues = new List<string>();

                propertyValues.Add(PLAYERNAMEFUCK);

                if(toonName == null)
                    propertyValues.Add(unit.UnitData.Name);

                propertyValues.Add(unit.UnitData.UnitStats.GetType().GetProperty(stat).GetValue(unit.UnitData.UnitStats, null).ToString());

                sb.AppendLine(HTMLConstructor.AddTableData(propertyValues.ToArray()));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets all of the toons of a given gear level
        /// </summary>
        /// <param name="gearLevel">Gear level to search for</param>
        /// <returns>Table of characters at a gear level</returns>
        private string GetAllToonsOfGearLevelDifference(int gearLevel)
        {
            StringBuilder sb = new StringBuilder();

            foreach (UnitDifference unitDifference in m_dataBuilder.DifferencesGuildData.Players.SelectMany(x => x.Units).OrderBy(a => a.PlayerName))
            {
                if (unitDifference.OldGearLevel < gearLevel && unitDifference.NewGearLevel == gearLevel)
                {
                    sb.AppendLine(HTMLConstructor.AddTableData(new string[] { unitDifference.PlayerName, unitDifference.Name }));
                    if (gearLevel == 13)
                        m_reportSummary.TotalGearThirteenToons++;
                    else if (gearLevel == 12)
                        m_reportSummary.TotalGearTwelveToons++;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Searches through a list of players for those who are part of a gold team
        /// </summary>
        /// <param name="potentialPlayers">List of players to go through</param>
        /// <param name="count">Number of characters that meet the requirement</param>
        /// <returns>List of players that meet the requirement of the gold member</returns>
        private List<string> FindGoldTeamPlayers(IEnumerable<IGrouping<string, string>> potentialPlayers, int count)
        {
            List<string> players = new List<string>();

            foreach (var player in potentialPlayers)
                if (player.Count() == count)
                    players.Add(player.Key);

            return players;
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

            var theList = new List<KeyValuePair<int, decimal>>();
            var filteredModList = new List<Mod>();
            var modList = m_dataBuilder.UnitData.SelectMany(b => b.Mods).ToList();

            theList.AddRange(modList.Where(a => a.ModSecondaryOneName == stat).ToDictionary(x => x.Id, x => x.ModSecondaryOne).ToList());
            theList.AddRange(modList.Where(a => a.ModSecondaryTwoName == stat).ToDictionary(x => x.Id, x => x.ModSecondaryTwo).ToList());
            theList.AddRange(modList.Where(a => a.ModSecondaryThreeName == stat).ToDictionary(x => x.Id, x => x.ModSecondaryThree).ToList());
            theList.AddRange(modList.Where(a => a.ModSecondaryFourName == stat).ToDictionary(x => x.Id, x => x.ModSecondaryFour).ToList());

            var topOfTheList = theList.OrderByDescending(a => a.Value).Take(returnCount).Select(b => b.Key);

            foreach (var topItemKey in topOfTheList)
                filteredModList.Add(modList.FirstOrDefault(a => a.Id == topItemKey));
            
            //Bold highlighted stat, add mod slot(shape)
            foreach(var topMod in filteredModList)
                topMods.AppendLine(HTMLConstructor.AddTableData(new string[] 
                { 
                    topMod.PlayerName, 
                    topMod.UnitName, 
                    topMod.ModRarity, 
                    topMod.ModShape,
                    topMod.ModSet, 
                    topMod.ModPrimaryName, 
                    topMod.ModSecondaryOneName == stat ? $"<b>({topMod.ModSecondaryOneRoll}) {topMod.ModSecondaryOneName} {topMod.ModSecondaryOne}</b>" : $"({topMod.ModSecondaryOneRoll}) {topMod.ModSecondaryOneName} {topMod.ModSecondaryOne}",
                    topMod.ModSecondaryTwoName == stat ? $"<b>({topMod.ModSecondaryTwoRoll}) {topMod.ModSecondaryTwoName} {topMod.ModSecondaryTwo}</b>" : $"({topMod.ModSecondaryTwoRoll}) {topMod.ModSecondaryTwoName} {topMod.ModSecondaryTwo}",
                    topMod.ModSecondaryThreeName == stat ? $"<b>({topMod.ModSecondaryThreeRoll}) {topMod.ModSecondaryThreeName} {topMod.ModSecondaryThree}</b>" : $"({topMod.ModSecondaryThreeRoll}) {topMod.ModSecondaryThreeName} {topMod.ModSecondaryThree}",
                    topMod.ModSecondaryFourName == stat ? $"<b>({topMod.ModSecondaryFourRoll}) {topMod.ModSecondaryFourName} {topMod.ModSecondaryFour}</b>" : $"({topMod.ModSecondaryFourRoll}) {topMod.ModSecondaryFourName} {topMod.ModSecondaryFour}",
                }));

            return topMods.ToString();
        }

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