using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;
using System.IO;
using SWGOHMessage;
using iText.Html2pdf;
using SWOGHHelper;
using SWGOHInterface;
using System.Windows.Forms.VisualStyles;

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
        //string m_goldMembers;
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
        List<UnitData> m_filteredUnitData;
        List<ShipData> m_filteredShipData;
        List<string> m_filteredPlayerNames;
        ReportSummary m_reportSummary;

        /// <summary>
        /// Constructor
        /// </summary>
        public ReportBuilder()
        {
            m_dataBuilder = new DataBuilder();
            m_filteredShipData = new List<ShipData>();
            m_filteredUnitData = new List<UnitData>();
            m_filteredPlayerNames = new List<string>();
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
            m_filteredShipData = new List<ShipData>();
            m_filteredUnitData = new List<UnitData>();
            m_filteredPlayerNames = new List<string>();
            m_fileName = fileName;
            m_toonName = characterName;
            m_isSimpleReport = true;
        }

        /// <summary>
        /// Grabs the data needed to run the simple report
        /// </summary>
        /// <param name="guild">Guild data pulled from the interface</param>
        /// <returns></returns>
        public async Task CompileSimpleReport(Guild guild)
        {
            await m_dataBuilder.CollectUnitDataFromInterface(guild);

            SWGOHMessageSystem.OutputMessage("Compiling Report Data....");

            await BuildReport();
        }

        /// <summary>
        /// Determines if the report can be ran
        /// </summary>
        public bool CanRunReport()
        {
            return m_dataBuilder.CanRunReport();
        }

        /// <summary>
        /// Grabs the data needed to run the detailed report
        /// </summary>
        /// <returns></returns>
        public async Task CompileReport()
        {
            m_dataBuilder.GetSnapshotNames();
            
            List<Task> tasks = new List<Task>();

            tasks.Add(Task.Run(() => m_dataBuilder.CollectUnitDataFromDB()));
            tasks.Add(Task.Run(() => m_dataBuilder.CollectShipDataFromDB()));
            tasks.Add(Task.Run(() => m_dataBuilder.CollectPlayerGPDifferencesFromDB()));
            tasks.Add(Task.Run(() => m_dataBuilder.CollectSnapshotMetadataFromDB()));

            m_fileName = SWGOHMessageSystem.InputMessage("Enter in the filename for the report");
            m_toonName = SWGOHMessageSystem.InputMessage("Enter in the toon to highlight for the report. Multiple toons can be added via comma delimited. If there is not a toon you wish to highlight, press enter to continue.");
            SWGOHMessageSystem.OutputMessage("Compiling Report Data....");

            await Task.WhenAll(tasks.ToArray());

            m_filteredPlayerNames = m_dataBuilder.PlayerData.Select(a => a.PlayerName).ToList();
            m_filteredUnitData = m_dataBuilder.UnitData.Where(a => m_filteredPlayerNames.Contains(a.PlayerName)).ToList();            
            m_filteredShipData = m_dataBuilder.ShipData.Where(a => m_filteredPlayerNames.Contains(a.PlayerName)).ToList();
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
            
            //tasks.Add(Task.Run(() => GoldMembers()));            
            tasks.Add(Task.Run(() => JourneyPrepared()));                          
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
                //pdfString.AppendLine(m_goldMembers);
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
                pdfString.AppendLine(m_journeyPrepared);
                pdfString.AppendLine(m_glProgress);
                pdfString.AppendLine(m_characterHighlight);
                //pdfString.AppendLine(m_goldMembers);
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
                sb.AppendLine(HTMLConstructor.ReportTitle(m_dataBuilder.GuildName, m_dataBuilder.DateRange));
                sb.AppendLine("<p/>");
                sb.AppendLine("<p/>");
                sb.AppendLine(HTMLConstructor.SectionHeader("Contents"));
                sb.AppendLine("<ol type=\"1\"");
                sb.AppendLine("<li></li>");
                sb.AppendLine("<li><a href=\"#guildsummary\">Guild Summary</a></li>");
                sb.AppendLine("<li><a href=\"#toptwenty\">Top 20 Stats</a></li>");
                sb.AppendLine("<li><a href=\"#toonprep\">Players prepped for Journey Toons</a></li>");
                if (!String.IsNullOrEmpty(m_toonName)) sb.AppendLine($"<li><a href=\"#highlight\">Character Highlight: {m_toonName}</a></li>");
                //sb.AppendLine("<li><a href=\"#goldmembers\">Gold Teams</a></li>");
                sb.AppendLine("</ol></div>");
            }
            else
            {
                sb.AppendLine("<div>");
                sb.AppendLine(HTMLConstructor.ReportTitle(m_dataBuilder.GuildName, m_dataBuilder.DateRange));
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
                sb.AppendLine("<li><a href=\"#toonprep\">Players prepped for Journey Toons</a></li>");
                if (!String.IsNullOrEmpty(m_toonName)) sb.AppendLine($"<li><a href=\"#highlight\">Character Highlight: {m_toonName}</a></li>");
                //sb.AppendLine("<li><a href=\"#goldmembers\">Gold Teams</a></li>");
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
            
            foreach (UnitData unit in m_filteredUnitData.OrderByDescending(a => a.PowerDifference).Take(50))            
                unitGPDiff.AppendLine(HTMLConstructor.AddTableData(new string[] { unit.PlayerName, unit.UnitName, unit.OldPower.ToString(), unit.NewPower.ToString(), unit.PowerDifference.ToString() }));

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon", "Old Power", "New Power", "Power Increase" }, unitGPDiff.ToString()));
            sb.AppendLine("<p/></div>");

            m_UnitGPDifferences = sb.ToString();

            await Task.CompletedTask;            
        }

        /// <summary>
        /// Method to generate gold members
        /// </summary>
        /// <returns></returns>
        private async Task GoldMembers()
        {
            StringBuilder sb = new StringBuilder();
            Dictionary<string, List<string>> goldTeams = new Dictionary<string, List<string>>();
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

            sb.AppendLine("<div id=\"goldmembers\">");
            sb.AppendLine(HTMLConstructor.SectionHeader("Gold Members"));
            sb.AppendLine("This section is to showcase players who have invested the gear and zetas for 'meta' or key toons of factions");            
            sb.AppendLine("<p/>");
            sb.AppendLine("<b>Rebels Team.</b> Commander Luke Skywalker(lead, binds all things), Han Solo(Shoots First), Chewie(all), R2-D2(Number Crunch), C-3PO(Oh My Goodness!)");
            sb.AppendLine("<p/>");
            sb.AppendLine("<b>JKR Team.</b> Grand Master Yoda(Battle Meditation), Jolee Bindo(That Looks Pretty Bad), Bastila Shan, General Kenobi, Jedi Knight Revan(all)");
            sb.AppendLine("<p/>");
            sb.AppendLine("<b>DR Team.</b> Darth Revan(all), Bastila Shan (Fallen)(Sith Apprentice) HK-47(Self-Reconstruction), Darth Malak(all)");
            sb.AppendLine("<p/>");
            sb.AppendLine("<b>Trimitive Team.</b> Darth Traya(all), Darth Sion(Lord of Pain), Darth Nihilus(Lord of Hunger)");
            sb.AppendLine("<p/>");
            sb.AppendLine("<b>Resistance Team.</b> Rey (Jedi Training)(Inspirational Presence), Finn, BB-8(Roll with the Punches), Amilyn Holdo");
            sb.AppendLine("<p/>");
            sb.AppendLine("<b>Bounty Hunter Team.</b> Bossk(On The Hunt), Jango Fett, Boba Fett, Embo, Dengar");
            sb.AppendLine("<p/>");
            sb.AppendLine("<b>Nightsister Team.</b> Mother Talzin(The Great Mother) Asajj Ventress, Old Daka, Nightsister Zombie");
            sb.AppendLine("<p/>");
            sb.AppendLine("<b>Troopers Team.</b> General Veers(Aggressive Tactician), Colonel Starck, Range Trooper");
            sb.AppendLine("<p/>");
            sb.AppendLine("<b>Old Republic Team.</b> Juhani, Carth Onasi(lead), Zaalbar(Mission's Guardian), Mission Vao(Me and Big Z Forever), Canderous Ordo");
            sb.AppendLine("<p/>");
            sb.AppendLine("<b>Sepratist Droid Team.</b> General Grievous(all), B1 Battle Droid(Droid Battalion), B2 Super Battle Droid, Droideka, IG-100 MagnaGuard");
            sb.AppendLine("<p/>");
            sb.AppendLine("<b>Bugs Team.</b> Geonosian Brood Alpha(all), Geonosian Soldier, Geonosian Spy, Poggle the Lesser, Sun Fac");
            sb.AppendLine("<p/>");
            sb.AppendLine("<b>Galatic Republic Team.</b> Padmé Amidala(all), Jedi Knight Anakin, Ahsoka Tano, General Kenobi");
            sb.AppendLine("<p/>");
            sb.AppendLine("<b>Ewok Team.</b> Chief Chirpa(Simple Tactics), Wicket, Logray, Paploo");
            sb.AppendLine("<p/>");
            sb.AppendLine("<b>First Order Team.</b> Kylo Ren (Unmasked)(all), Kylo Ren, First Order Officer, First Order Executioner");

            sb.AppendLine("</div><div>");

            rebels.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Commander Luke Skywalker" && a.NewGearLevel > 11 && a.NewZetas.Contains("It Binds All Things")));
            rebels.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Han Solo" && a.NewGearLevel > 11 && a.NewZetas.Contains("Shoots First")));
            rebels.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Chewbacca" && a.NewGearLevel > 11 && a.NewZetas.Contains("Loyal Friend") && a.NewZetas.Contains("Raging Wookiee")));
            rebels.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "R2-D2" && a.NewGearLevel > 11 && a.NewZetas.Contains("Number Crunch")));
            rebels.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "C-3PO" && a.NewGearLevel > 11 && a.NewZetas.Contains("Oh My Goodness!")));
            
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Jolee Bindo" && a.NewGearLevel > 11 && a.NewZetas.Contains("That Looks Pretty Bad")));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Grand Master Yoda" && a.NewGearLevel > 11 && a.NewZetas.Contains("Battle Meditation")));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Bastila Shan" && a.NewGearLevel > 11));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "General Kenobi" && a.NewGearLevel > 11));
            jkr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Jedi Knight Revan" && a.NewGearLevel > 11 && a.NewZetas.Contains("Direct Focus") && a.NewZetas.Contains("Hero") && a.NewZetas.Contains("General")));

            dr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "HK-47" && a.NewGearLevel > 11 && a.NewZetas.Contains("Self-Reconstruction")));
            dr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Bastila Shan (Fallen)" && a.NewGearLevel > 11 && a.NewZetas.Contains("Sith Apprentice")));
            dr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Darth Malak" && a.NewGearLevel > 11 && a.NewZetas.Contains("Gnawing Terror") && a.NewZetas.Contains("Jaws of Life")));
            dr.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Darth Revan" && a.NewGearLevel > 11 && a.NewZetas.Contains("Lord of the Sith") && a.NewZetas.Contains("Conqueror") && a.NewZetas.Contains("Villain")));

            trimitive.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Darth Sion" && a.NewGearLevel > 11 && a.NewZetas.Contains("Lord of Pain")));
            trimitive.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Darth Nihilus" && a.NewGearLevel > 11 && a.NewZetas.Contains("Lord of Hunger")));
            trimitive.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Darth Traya" && a.NewGearLevel > 11 && a.NewZetas.Contains("Lord of Betrayal") && a.NewZetas.Contains("Compassion is Weakness")));

            resistance.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "BB-8" && a.NewGearLevel > 11 && a.NewZetas.Contains("Roll with the Punches")));
            resistance.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Rey (Jedi Training)" && a.NewGearLevel > 11 && a.NewZetas.Contains("Inspirational Presence")));
            resistance.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Finn" && a.NewGearLevel > 11));
            resistance.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Amilyn Holdo" && a.NewGearLevel > 11));
            
            bountyHunter.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Bossk" && a.NewGearLevel > 11 && a.NewZetas.Contains("On The Hunt")));
            bountyHunter.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Jango Fett" && a.NewGearLevel > 11));
            bountyHunter.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Boba Fett" && a.NewGearLevel > 11));
            bountyHunter.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Embo" && a.NewGearLevel > 11));
            bountyHunter.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Dengar" && a.NewGearLevel > 11));

            nightsister.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Mother Talzin" && a.NewGearLevel > 11 && a.NewZetas.Contains("The Great Mother")));
            nightsister.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Nightsister Zombie" && a.NewGearLevel > 11));
            nightsister.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Old Daka" && a.NewGearLevel > 11));
            nightsister.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Asajj Ventress" && a.NewGearLevel > 11));

            trooper.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "General Veers" && a.NewGearLevel > 11 && a.NewZetas.Contains("Aggressive Tactician")));
            trooper.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Colonel Starck" && a.NewGearLevel > 11));
            trooper.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Range Trooper" && a.NewGearLevel > 11));
            
            oldRepublic.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Carth Onasi" && a.NewGearLevel > 11 && a.NewZetas.Contains("Soldier of the Old Republic")));
            oldRepublic.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Mission Vao" && a.NewGearLevel > 11 && a.NewZetas.Contains("Me and Big Z Forever")));
            oldRepublic.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Canderous Ordo" && a.NewGearLevel > 11));
            oldRepublic.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Juhani" && a.NewGearLevel > 11));
            oldRepublic.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Zaalbar" && a.NewGearLevel > 11 && a.NewZetas.Contains("Mission's Guardian")));

            sepratistDroid.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "B1 Battle Droid" && a.NewGearLevel > 11 && a.NewZetas.Contains("Droid Battalion")));
            sepratistDroid.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "B2 Super Battle Droid" && a.NewGearLevel > 11));
            sepratistDroid.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Droideka" && a.NewGearLevel > 11));
            sepratistDroid.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "IG-100 MagnaGuard" && a.NewGearLevel > 11));
            sepratistDroid.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "General Grievous" && a.NewGearLevel > 11 && a.NewZetas.Contains("Daunting Presence") && a.NewZetas.Contains("Metalloid Monstrosity")));

            bugs.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Geonosian Soldier" && a.NewGearLevel > 11));
            bugs.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Geonosian Spy" && a.NewGearLevel > 11));
            bugs.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Poggle the Lesser" && a.NewGearLevel > 11));
            bugs.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Sun Fac" && a.NewGearLevel > 11));
            bugs.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Geonosian Brood Alpha" && a.NewGearLevel > 11 && a.NewZetas.Contains("Queen's Will") && a.NewZetas.Contains("Geonosian Swarm")));

            galaticRepublic.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Jedi Knight Anakin" && a.NewGearLevel > 11));
            galaticRepublic.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Ahsoka Tano" && a.NewGearLevel > 11));
            galaticRepublic.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "General Kenobi" && a.NewGearLevel > 11));
            galaticRepublic.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Padmé Amidala" && a.NewGearLevel > 11 && a.NewZetas.Contains("Always a Choice") && a.NewZetas.Contains("Unwavering Courage")));

            ewok.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Chief Chirpa" && a.NewGearLevel > 11 && a.NewZetas.Contains("Simple Tactics")));
            ewok.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Logray" && a.NewGearLevel > 11));
            ewok.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Paploo" && a.NewGearLevel > 11));
            ewok.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Wicket" && a.NewGearLevel > 11));

            firstOrder.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "First Order Officer" && a.NewGearLevel > 11));
            firstOrder.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "First Order Executioner" && a.NewGearLevel > 11));
            firstOrder.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Kylo Ren" && a.NewGearLevel > 11));
            firstOrder.AddRange(m_dataBuilder.UnitData.Where(a => a.UnitName == "Kylo Ren (Unmasked)" && a.NewGearLevel > 11 && a.NewZetas.Contains("Scarred") && a.NewZetas.Contains("Merciless Pursuit")));

            goldTeams.Add("Rebels Team", FindGoldTeamPlayers(rebels.Select(a => a.PlayerName).ToList().GroupBy(b => b), 5));
            goldTeams.Add("JKR Team", FindGoldTeamPlayers(jkr.Select(a => a.PlayerName).ToList().GroupBy(b => b), 5));
            goldTeams.Add("DR Team", FindGoldTeamPlayers(dr.Select(a => a.PlayerName).ToList().GroupBy(b => b), 4));
            goldTeams.Add("Trimitive Team", FindGoldTeamPlayers(trimitive.Select(a => a.PlayerName).ToList().GroupBy(b => b), 3));
            goldTeams.Add("Resistance Team", FindGoldTeamPlayers(resistance.Select(a => a.PlayerName).ToList().GroupBy(b => b), 4));
            goldTeams.Add("Bounty Hunter Team", FindGoldTeamPlayers(bountyHunter.Select(a => a.PlayerName).ToList().GroupBy(b => b), 5));
            goldTeams.Add("Nightsister Team", FindGoldTeamPlayers(nightsister.Select(a => a.PlayerName).ToList().GroupBy(b => b), 4));
            goldTeams.Add("Troopers Team", FindGoldTeamPlayers(trooper.Select(a => a.PlayerName).ToList().GroupBy(b => b), 3));
            goldTeams.Add("Old Republic Team", FindGoldTeamPlayers(oldRepublic.Select(a => a.PlayerName).ToList().GroupBy(b => b), 5));
            goldTeams.Add("Sepratist Droid Team", FindGoldTeamPlayers(sepratistDroid.Select(a => a.PlayerName).ToList().GroupBy(b => b), 5));
            goldTeams.Add("Bugs Team", FindGoldTeamPlayers(bugs.Select(a => a.PlayerName).ToList().GroupBy(b => b), 5));
            goldTeams.Add("Galatic Republic Team", FindGoldTeamPlayers(galaticRepublic.Select(a => a.PlayerName).ToList().GroupBy(b => b), 4));
            goldTeams.Add("Ewok Team", FindGoldTeamPlayers(ewok.Select(a => a.PlayerName).ToList().GroupBy(b => b), 4));
            goldTeams.Add("First Order Team", FindGoldTeamPlayers(firstOrder.Select(a => a.PlayerName).ToList().GroupBy(b => b), 4));

            var goldTeamsList = goldTeams.ToList();

            goldTeamsList.Sort((pair1, pair2) => pair2.Value.Count.CompareTo(pair1.Value.Count));
            int teamCount = 0;

            foreach(var goldTeam in goldTeamsList)
            {
                teamCount++;
                StringBuilder goldTeamBuilder = new StringBuilder();

                if (teamCount == 1)
                    sb.AppendLine(HTMLConstructor.TableGroupStart());

                foreach(var player in goldTeam.Value)
                    goldTeamBuilder.AppendLine(HTMLConstructor.AddTableData(new string[] { player }));

                sb.AppendLine(HTMLConstructor.AddTable(new string[] { goldTeam.Key }, goldTeamBuilder.ToString()));

                if (teamCount == 5)
                {
                    sb.AppendLine(HTMLConstructor.TableGroupEnd());
                    teamCount = 0;
                }
                else
                    sb.AppendLine(HTMLConstructor.TableGroupNext());
            }

            if(teamCount != 0)
                sb.AppendLine(HTMLConstructor.TableGroupEnd());

            sb.AppendLine("<p/></div>");
            //m_goldMembers = sb.ToString();

            await Task.CompletedTask;
        }

        /// <summary>
        /// Method to generate list of players prepped
        /// </summary>
        /// <returns></returns>
        private async Task JourneyPrepared()
        {
            StringBuilder sb = new StringBuilder();
            List<Unlock> unlocks = new List<Unlock>();
            List<string> generalSkywalkerLocked = new List<string>();
            List<string> malakLocked = new List<string>();
            List<string> jediKnightRevanLocked = new List<string>();
            List<string> darthRevanLocked = new List<string>();
            List<string> commanderLukeSkywalkerLocked = new List<string>();
            List<string> jediTrainingReyLocked = new List<string>();
            List<string> jediKnightLukeLocked = new List<string>();

            sb.AppendLine("<div id=\"toonprep\">");
            sb.AppendLine(HTMLConstructor.SectionHeader("Prepped Journey Players"));
            sb.AppendLine("This section highlights all of the players whom are awaitng the return of Journey Toons. This only factors in if the player meets the min requirement in game to participate in the event to unlock the toon");
            sb.AppendLine("</p>");
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

                if (m_dataBuilder.UnitData.Where(a => a.PlayerName == player.PlayerName && a.UnitName == "General Skywalker").Count() == 0)
                    generalSkywalkerLocked.Add(player.PlayerName);

                if (m_dataBuilder.UnitData.Where(a => a.PlayerName == player.PlayerName && a.UnitName == "Jedi Knight Luke Skywalker").Count() == 0)
                    jediKnightLukeLocked.Add(player.PlayerName);
            }
            
            foreach(string player in commanderLukeSkywalkerLocked)
            {
                if(m_dataBuilder.UnitData.Where(a => a.NewRarity == 7 && a.PlayerName == player &&
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
                if (m_dataBuilder.UnitData.Where(a => a.NewRarity == 7 && a.PlayerName == player &&
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
                if (m_dataBuilder.UnitData.Where(a => a.NewRarity == 7 && a.PlayerName == player &&
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
                if (m_dataBuilder.UnitData.Where(a => a.NewRarity == 7 && a.PlayerName == player &&
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
                if (m_dataBuilder.UnitData.Where(a => a.NewRarity == 7 && a.NewPower > 16999 && a.PlayerName == player &&
                    (a.UnitName == "Bastila Shan (Fallen)" ||
                     a.UnitName == "Canderous Ordo" ||
                     a.UnitName == "Carth Onasi" ||
                     a.UnitName == "HK-47" ||
                     a.UnitName == "Juhani")
                    ).Count() > 3)
                {
                    if (m_dataBuilder.UnitData.Where(a => a.NewRarity == 7 && a.NewPower > 16999 && a.PlayerName == player &&
                        (a.UnitName == "T3-M4" ||
                         a.UnitName == "Mission Vao" ||
                         a.UnitName == "Zaalbar" ||
                         a.UnitName == "Jolee Bindo" ||
                         a.UnitName == "Bastila Shan")
                        ).Count() > 3)
                    {
                        if (m_dataBuilder.UnitData.Where(a => a.NewRarity == 7 && a.NewPower > 16999 && a.PlayerName == player && a.UnitName == "Jedi Knight Revan").Count() == 1 &&
                            m_dataBuilder.UnitData.Where(a => a.NewRarity == 7 && a.NewPower > 16999 && a.PlayerName == player && a.UnitName == "Darth Revan").Count() == 1)
                        {
                            unlocks.Add(new Unlock(player, "Darth Malak"));
                        }
                    }
                }
            }

            foreach (string player in generalSkywalkerLocked)
            {
                if (m_dataBuilder.UnitData.Where(a => a.NewRarity == 7 && a.NewPower > 16999 && a.PlayerName == player &&
                    (a.UnitName == "C-3PO" ||
                     a.UnitName == "General Kenobi" ||
                     a.UnitName == "Shaak Ti" ||
                     a.UnitName == "Ahsoka Tano" ||
                     a.UnitName == "Padmé Amidala" ||
                     a.UnitName == "Asajj Ventress" ||
                     a.UnitName == "B1 Battle Droid" ||
                     a.UnitName == "B2 Super Battle Droid" ||
                     a.UnitName == "IG-100 MagnaGuard" ||
                     a.UnitName == "Droideka")
                    ).Count() == 10)
                {
                    if (m_dataBuilder.ShipData.Where(a => a.NewRarity == 7 && a.NewPower > 39999 && a.PlayerName == player &&
                        (a.ShipName == "Anakin's Eta-2 Starfighter")
                        ).Count() == 1)
                    {
                        if (m_dataBuilder.ShipData.Where(a => a.NewRarity == 7 && a.NewPower > 39999 && a.PlayerName == player &&
                            (a.ShipName == "Endurance" ||
                             a.ShipName == "Negotiator")
                            ).Count() > 0)
                        {
                            unlocks.Add(new Unlock(player, "General Skywalker"));
                        }
                    }
                }
            }

            foreach (string player in jediKnightLukeLocked)
            {
                if (m_dataBuilder.UnitData.Where(a => a.NewRarity == 7 && a.NewRelicTier >= 3 && a.PlayerName == player &&
                     (a.UnitName == "Commander Luke Skywalker" ||
                     a.UnitName == "Rebel Officedr Leia Organa" ||
                     a.UnitName == "Captain Han Solo" ||
                     a.UnitName == "Chewbacca" ||
                     a.UnitName == "Lando Calrissian" ||
                     a.UnitName == "Hermit Yoda" ||
                     a.UnitName == "Darth Vader" ||
                     a.UnitName == "Wampa" ||
                     a.UnitName == "C-3PO")
                    ).Count() == 9)
                {
                    if (m_dataBuilder.ShipData.Where(a => a.NewRarity == 7 && a.NewPower > 39999 && a.PlayerName == player &&
                           (a.ShipName == "Han's Millennium Falcon" ||
                            a.ShipName == "Wedge Antilles's X-wing")
                           ).Count() > 0)
                    {
                        unlocks.Add(new Unlock(player, "Jedi Knight Luke Skywalker"));
                    }
                }
            }

            StringBuilder prepaired = new StringBuilder();
            foreach (Unlock unlock in unlocks.OrderBy(a => a.PlayerName))
                prepaired.AppendLine(HTMLConstructor.AddTableData(new string[] { unlock.PlayerName, unlock.UnitOrShipName }));

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon" }, prepaired.ToString()));

            sb.AppendLine("<p/></div>");

            m_journeyPrepared = sb.ToString();

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
            StringBuilder overall = new StringBuilder();
            m_glCharacterProgressList = new List<GLCharacterProgress>();

            sb.AppendLine("<div id=\"glprep\">");
            sb.AppendLine(HTMLConstructor.SectionHeader("Galatic Legend Prep"));
            sb.AppendLine("This section goes over all guild members and their progress towards a GL toon.  100% for each toon indicates the player is currently in progress or has unlocked the toon.");
            sb.AppendLine("<p>Calculations of progress is based on current gear level, gear pieces applied at current gear level, relic level and star level relative to the requirement for the toon.");
                        
            foreach (string playerName in m_dataBuilder.UnitData.Where(b => b.NewGalaticPower != 0).Select(a => a.PlayerName).Distinct().OrderBy(a => a))
            {
                m_glCharacterProgressList.Add(new GLCharacterProgress() { PlayerName = playerName });
                rey.AppendLine(GetGLReyProgressForPlayer(playerName));
                slkr.AppendLine(GetGLKyloProgressForPlayer(playerName));
                luke.AppendLine(GetGLLukeProgressForPlayer(playerName));
                palp.AppendLine(GetGLPalpProgressForPlayer(playerName));
                kenobi.AppendLine(GetGLKenobiProgressForPlayer(playerName));
                lv.AppendLine(GetGLVaderProgressForPlayer(playerName));
                overall.AppendLine(GetOverallGLProgressForPlayer(playerName));
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
            sb.AppendLine("<b>Overall Progress:</b>");

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Rey", "SLKR", "JML", "SEE", "GLOW", "LV" }, overall.ToString()));

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

            foreach (string playerName in m_dataBuilder.UnitData.Where(b => b.NewGalaticPower != 0).Select(a => a.PlayerName).Distinct().OrderBy(a => a))
            {
                m_glCharacterProgressList.Add(new GLCharacterProgress() { PlayerName = playerName });
                KAM.AppendLine(GetKAMProgressForPlayer(playerName));
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
        private string GetOverallGLProgressForPlayer(string playerName)
        {
            GLCharacterProgress playerProgress = m_glCharacterProgressList.FirstOrDefault(a => a.PlayerName == playerName);
            return HTMLConstructor.AddTableData(new string[] { playerName, playerProgress.ReyOverallProgress, playerProgress.SLKROverallProgress, playerProgress.GLLukeOverallProgress, playerProgress.GLPalpOverallProgress, playerProgress.GLKenobiProgress, playerProgress.GLVaderProgress });
        }


        /// <summary>
        /// Method to determine GL Kylo progress
        /// </summary>
        /// <param name="playerName"></param>
        /// <returns></returns>
        private string GetGLLukeProgressForPlayer(string playerName)
        {
            List<decimal> progressList = new List<decimal>();
            string big = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Biggs Darklighter"), 85, out progressList, progressList);
            string c3p0 = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "C-3PO"), 94, out progressList, progressList);
            string chew = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Chewbacca"), 100, out progressList, progressList);
            string han = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Han Solo"), 100, out progressList, progressList);
            string yoda = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Hermit Yoda"), 94, out progressList, progressList);
            string jkl = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Jedi Knight Luke Skywalker"), 107, out progressList, progressList);
            string land = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Lando Calrissian"), 94, out progressList, progressList);
            string mon = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Mon Mothma"), 94, out progressList, progressList);
            string obi = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Obi-Wan Kenobi (Old Ben)"), 94, out progressList, progressList);
            string leia = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Princess Leia"), 85, out progressList, progressList);
            string r2d2 = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "R2-D2"), 107, out progressList, progressList);
            string jtr = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Rey (Jedi Training)"), 107, out progressList, progressList);
            string chwp = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Threepio & Chewie"), 94, out progressList, progressList);
            string wed = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Wedge Antilles"), 85, out progressList, progressList);
            string ywin = CalculatePercentProgressForGL(m_dataBuilder.ShipData.FirstOrDefault(a => a.PlayerName == playerName && a.ShipName == "Rebel Y-wing"), 6, out progressList, progressList);

            m_glCharacterProgressList.FirstOrDefault(a => a.PlayerName == playerName).GLLukeOverallProgress = Math.Round(progressList.Average(), 2).ToString();

            return HTMLConstructor.AddTableData(new string[] { playerName, big, c3p0, chew, han, yoda, jkl, land, mon, obi, leia, r2d2, jtr, chwp, wed, ywin });
        }

        /// <summary>
        /// Method to determine GL Kylo progress
        /// </summary>
        /// <param name="playerName"></param>
        /// <returns></returns>
        private string GetGLPalpProgressForPlayer(string playerName)
        {
            List<decimal> progressList = new List<decimal>();
            string pie = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Admiral Piett"), 94, out progressList, progressList);
            string star = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Colonel Starck"), 85, out progressList, progressList);
            string dook = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Count Dooku"), 100, out progressList, progressList);
            string maul = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Darth Maul"), 89, out progressList, progressList);
            string sidi = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Darth Sidious"), 107, out progressList, progressList);
            string vad = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Darth Vader"), 107, out progressList, progressList);
            string kren = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Director Krennic"), 89, out progressList, progressList);
            string palp = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Emperor Palpatine"), 107, out progressList, progressList);
            string veer = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "General Veers"), 85, out progressList, progressList);
            string thra = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Grand Admiral Thrawn"), 100, out progressList, progressList);
            string tark = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Grand Moff Tarkin"), 85, out progressList, progressList);
            string jka = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Jedi Knight Anakin"), 107, out progressList, progressList);
            string rgua = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Royal Guard"), 85, out progressList, progressList);
            string mara = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Sith Marauder"), 107, out progressList, progressList);
            string bomb = CalculatePercentProgressForGL(m_dataBuilder.ShipData.FirstOrDefault(a => a.PlayerName == playerName && a.ShipName == "Imperial TIE Bomber"), 6, out progressList, progressList);

            m_glCharacterProgressList.FirstOrDefault(a => a.PlayerName == playerName).GLPalpOverallProgress = Math.Round(progressList.Average(), 2).ToString();

            return HTMLConstructor.AddTableData(new string[] { playerName, pie, star, dook, maul, sidi, vad, kren, palp, veer, thra, tark, jka, rgua, mara, bomb});
        }

        /// <summary>
        /// Method to determine GL Kenobi progress
        /// </summary>
        /// <param name="playerName"></param>
        /// <returns></returns>
        private string GetGLKenobiProgressForPlayer(string playerName)
        {
            List<decimal> progressList = new List<decimal>();
            string kenobi = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "General Kenobi"), 115, out progressList, progressList);
            string mace = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Mace Windu"), 85, out progressList, progressList);
            string aayla = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Aayla Secura"), 85, out progressList, progressList);
            string katan = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Bo-Katan Kryze"), 94, out progressList, progressList);
            string jinn = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Qui-Gon Jinn"), 85, out progressList, progressList);
            string magna = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "IG-100 MagnaGuard"), 94, out progressList, progressList);
            string clone = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Clone Sergeant - Phase I"), 94, out progressList, progressList);
            string wat = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Wat Tambor"), 107, out progressList, progressList);
            string gg = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "General Grievous"), 107, out progressList, progressList);
            string cadbane = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Cad Bane"), 94, out progressList, progressList);
            string cody = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "CC-2224 \"Cody\""), 94, out progressList, progressList);
            string jango = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Jango Fett"), 107, out progressList, progressList);
            string shaak = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Shaak Ti"), 107, out progressList, progressList);
            string gmy = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Grand Master Yoda"), 115, out progressList, progressList);
            string nego = CalculatePercentProgressForGL(m_dataBuilder.ShipData.FirstOrDefault(a => a.PlayerName == playerName && a.ShipName == "Negotiator"), 6, out progressList, progressList);

            m_glCharacterProgressList.FirstOrDefault(a => a.PlayerName == playerName).GLKenobiProgress = Math.Round(progressList.Average(), 2).ToString();

            return HTMLConstructor.AddTableData(new string[] { playerName, kenobi, mace, aayla, katan, jinn, magna, clone, wat, gg, cadbane, cody, jango, shaak, gmy, nego });
        }

        private string GetGLVaderProgressForPlayer(string playerName)
        {
            List<decimal> progressList = new List<decimal>();
            string hunter = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Hunter"), 94, out progressList, progressList);
            string tech = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Tech"), 94, out progressList, progressList);
            string wrecker = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Wrecker"), 94, out progressList, progressList);
            string tusken = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Tusken Raider"), 94, out progressList, progressList);
            string padme = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Padmé Amidala"), 115, out progressList, progressList);
            string embo = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Embo"), 94, out progressList, progressList);
            string echo = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "CT-21-0408 \"Echo\""), 107, out progressList, progressList);
            string bbEcho = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Echo"), 94, out progressList, progressList);
            string dooku = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Count Dooku"), 115, out progressList, progressList);
            string zam = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Zam Wesell"), 107, out progressList, progressList);
            string tarkin = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Grand Moff Tarkin"), 107, out progressList, progressList);
            string arc = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "ARC Trooper"), 115, out progressList, progressList);
            string gas = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "General Skywalker"), 115, out progressList, progressList);
            string nute = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Nute Gunray"), 107, out progressList, progressList);
            string ywing = CalculatePercentProgressForGL(m_dataBuilder.ShipData.FirstOrDefault(a => a.PlayerName == playerName && a.ShipName == "BTL-B Y-wing Starfighter"), 7, out progressList, progressList);

            m_glCharacterProgressList.FirstOrDefault(a => a.PlayerName == playerName).GLVaderProgress = Math.Round(progressList.Average(), 2).ToString();

            return HTMLConstructor.AddTableData(new string[] { playerName, hunter, tech, wrecker, tusken, padme, embo, echo, bbEcho, dooku, zam, tarkin, arc, gas, nute, ywing});
        }

        /// <summary>
        /// Method to determine GL Kylo progress
        /// </summary>
        /// <param name="playerName"></param>
        /// <returns></returns>
        private string GetGLKyloProgressForPlayer(string playerName)
        {
            List<decimal> progressList = new List<decimal>();
            string kru = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Kylo Ren (Unmasked)"), 107, out progressList, progressList);
            string fos = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "First Order Stormtrooper"), 94, out progressList, progressList);
            string foo = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "First Order Officer"), 94, out progressList, progressList);
            string kyloRen = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Kylo Ren"), 107, out progressList, progressList);
            string phasma = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Captain Phasma"), 94, out progressList, progressList);
            string fox = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "First Order Executioner"), 94, out progressList, progressList);
            string vetHan = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Veteran Smuggler Han Solo"), 85, out progressList, progressList);
            string sithTroop = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Sith Trooper"), 94, out progressList, progressList);
            string fosftp = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "First Order SF TIE Pilot"), 85, out progressList, progressList);
            string hux = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "General Hux"), 94, out progressList, progressList);
            string fotp = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "First Order TIE Pilot"), 85, out progressList, progressList);
            string palp = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Emperor Palpatine"), 107, out progressList, progressList);
            string finalizer = CalculatePercentProgressForGL(m_dataBuilder.ShipData.FirstOrDefault(a => a.PlayerName == playerName && a.ShipName == "Finalizer"), 5, out progressList, progressList);

            m_glCharacterProgressList.FirstOrDefault(a => a.PlayerName == playerName).SLKROverallProgress = Math.Round(progressList.Average(), 2).ToString();

            return HTMLConstructor.AddTableData(new string[] { playerName, kru, fos, foo, kyloRen, phasma, fox, vetHan, sithTroop, fosftp, hux, fotp, palp, finalizer });
        }

        /// <summary>
        /// Method to determine KAM progress for a player
        /// </summary>
        /// <param name="playerName"></param>
        /// <returns></returns>
        private string GetKAMProgressForPlayer(string playerName)
        {
            List<decimal> progressList = new List<decimal>();
            string shaak = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Shaak Ti"), 94, out progressList, progressList);
            string rex = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "CT-7567 \"Rex\""), 94, out progressList, progressList);
            string fives = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "CT-5555 \"Fives\""), 94, out progressList, progressList);
            string echo = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "CT-21-0408 \"Echo\""), 94, out progressList, progressList);
            string arc = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "ARC Trooper"), 94, out progressList, progressList);

            return HTMLConstructor.AddTableData(new string[] { playerName, shaak, rex, fives, echo, arc });
        }

        /// <summary>
        /// Method to determine GL Rey progress
        /// </summary>
        /// <param name="playerName"></param>
        /// <returns></returns>
        private string GetGLReyProgressForPlayer(string playerName)
        {
            List<decimal> progressList = new List<decimal>();
            string scavRey = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Rey (Scavenger)"), 107, out progressList, progressList);
            string jtr = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Rey (Jedi Training)"), 107, out progressList, progressList);
            string finn = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Finn"), 94, out progressList, progressList);
            string rhFinn = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Resistance Hero Finn"), 94, out progressList, progressList);
            string poe = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Poe Dameron"), 94, out progressList, progressList);
            string rhPoe = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Resistance Hero Poe"), 94, out progressList, progressList);
            string holdo = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Amilyn Holdo"), 94, out progressList, progressList);
            string rose = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Rose Tico"), 94, out progressList, progressList);
            string resTrooper = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Resistance Trooper"), 94, out progressList, progressList);
            string resPilot = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Resistance Pilot"), 85, out progressList, progressList);
            string bbEight = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "BB-8"), 107, out progressList, progressList);
            string vetChewie = CalculatePercentProgressForGL(m_dataBuilder.UnitData.FirstOrDefault(a => a.PlayerName == playerName && a.UnitName == "Veteran Smuggler Chewbacca"), 85, out progressList, progressList);
            string raddus = CalculatePercentProgressForGL(m_dataBuilder.ShipData.FirstOrDefault(a => a.PlayerName == playerName && a.ShipName == "Raddus"), 5, out progressList, progressList);

            m_glCharacterProgressList.FirstOrDefault(a => a.PlayerName == playerName).ReyOverallProgress = Math.Round(progressList.Average(), 2).ToString();

            return HTMLConstructor.AddTableData(new string[] { playerName, jtr, finn, resTrooper, scavRey, resPilot, poe, rhFinn, holdo, rose, rhPoe, bbEight, vetChewie, raddus });
        }

        /// <summary>
        /// Method to calculate the percent of progress towards a toon
        /// </summary>
        /// <param name="unitData">The toon to calculate against</param>
        /// <param name="maxPoints">Max points of progress for the toon</param>
        /// <param name="progressList">The overall progresss</param>
        /// <param name="currentList">The current snapsho of the overall progress</param>
        /// <returns></returns>
        private string CalculatePercentProgressForGL(UnitData unitData, int maxPoints, out List<decimal> progressList, List<decimal> currentList)
        {
            progressList = currentList;            
            
            if (unitData == null)
            {
                progressList.Add(Convert.ToDecimal(0.0));
                return "0";
            }

            int relicPoints = 0;
            switch (unitData.NewRelicTier)
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

            //-1 because we want to count the number of gear pieces equipped not the current gear level ie Gear 1 if not the -1 would award 6 points
            int points = ((unitData.NewGearLevel-1) * 6) + relicPoints + unitData.HasGearSlotOne + unitData.HasGearSlotTwo + unitData.HasGearSlotThree + unitData.HasGearSlotFour + unitData.HasGearSlotFive + unitData.HasGearSlotSix + unitData.NewRarity;

            progressList.Add(Math.Round(Decimal.Divide(points, maxPoints) * 100, 2) > 100 ? 100 : Math.Round(Decimal.Divide(points, maxPoints) * 100, 2));
            return Math.Round(Decimal.Divide(points, maxPoints) * 100, 2) > 100 ? "100" : Math.Round(Decimal.Divide(points, maxPoints) * 100, 0).ToString();
        }

        /// <summary>
        /// Method to calculate the percent of progress towards a ship
        /// </summary>
        /// <param name="unitData">The ship to calculate against</param>
        /// <param name="maxPoints">Max points of progress for the toon</param>
        /// <param name="progressList">The overall progresss</param>
        /// <param name="currentList">The current snapsho of the overall progress</param>
        /// <returns></returns>
        private string CalculatePercentProgressForGL(ShipData shipData, int maxPoints, out List<decimal> progressList, List<decimal> currentList)
        {
            progressList = currentList;

            if (shipData == null)
            {
                progressList.Add(Convert.ToDecimal(0.0));
                return "0";
            }
            
            progressList.Add(Math.Round(Decimal.Divide(shipData.NewRarity, maxPoints) * 100, 2) > 100 ? 100 : Math.Round(Decimal.Divide(shipData.NewRarity, maxPoints) * 100, 2));
            return Math.Round(Decimal.Divide(shipData.NewRarity, maxPoints) * 100, 2) > 100 ? "100" : Math.Round(Decimal.Divide(shipData.NewRarity, maxPoints) * 100, 0).ToString();
        }

        /// <summary>
        /// Method to generate list of players that unlocked
        /// </summary>
        /// <returns></returns>
        private async Task JourneyOrLegendaryUnlock()
        {
            StringBuilder sb = new StringBuilder();
            List<Unlock> unlocks = new List<Unlock>();

            sb.AppendLine("<div id=\"toonunlock\">");
            sb.AppendLine(HTMLConstructor.SectionHeader("Journey/Legendary/Galactic Legend Unlock"));
            sb.AppendLine("This section highlights all players who have unlocked a Legendary, Journey or Galactic Legend toon/ship.");
            sb.AppendLine("</p>");

            var filteredUnitList = m_dataBuilder.UnitData.Where(a => a.OldRarity == 0 && a.NewRarity != 0 && m_filteredPlayerNames.Contains(a.PlayerName) && (
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
                a.UnitName == "Darth Malak" ||
                a.UnitName == "General Skywalker" ||
                a.UnitName == "Supreme Leader Kylo Ren" ||
                a.UnitName == "Rey" ||
                a.UnitName == "Jedi Master Luke Skywalker" ||
                a.UnitName == "Sith Eternal Emperor" ||
                a.UnitName == "Jedi Knight Luke Skywalker" ||
                a.UnitName == "The Mandalorian (Beskar Armor)" ||
                a.UnitName == "Jedi Master Kenobi" ||
                a.UnitName == "Lord Vader"
            )).ToList();

            foreach (UnitData unit in filteredUnitList.OrderBy(a => a.PlayerName))
                unlocks.Add(new Unlock(unit.PlayerName, unit.UnitName));

            var filteredShipList = m_dataBuilder.ShipData.Where(a => a.OldRarity == 0 && a.NewRarity != 0 && m_filteredPlayerNames.Contains(a.PlayerName) && (
                a.ShipName == "Chimaera" ||
                a.ShipName == "Han's Millennium Falcon" ||
                a.ShipName == "Executor"
            )).ToList();

            foreach (ShipData ship in filteredShipList.OrderBy(a => a.PlayerName))
                unlocks.Add(new Unlock(ship.PlayerName, ship.ShipName));

            StringBuilder unlockedRows = new StringBuilder();
            foreach(Unlock unlock in unlocks.OrderBy(a => a.PlayerName))
                unlockedRows.AppendLine(HTMLConstructor.AddTableData(new string[] { unlock.PlayerName, unlock.UnitOrShipName }));

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

            foreach (UnitData unit in m_filteredUnitData.OrderBy(a => a.PlayerName))
            {
                if (unit.OldZetas.Count < unit.NewZetas.Count)
                {
                    zetas.AppendLine(HTMLConstructor.AddTableData(new string[] { unit.PlayerName, unit.UnitName, string.Join(",", unit.NewZetas.Except(unit.OldZetas).ToArray()) }));
                    m_reportSummary.TotalZetasApplied++;
                }
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

            foreach (UnitData unit in m_filteredUnitData.OrderBy(a => a.PlayerName))
            {
                if (unit.OldOmicrons.Count < unit.NewOmicrons.Count)
                {
                    omicrons.AppendLine(HTMLConstructor.AddTableData(new string[] { unit.PlayerName, unit.UnitName, string.Join(",", unit.NewOmicrons.Except(unit.OldOmicrons).ToArray()) }));
                    m_reportSummary.TotalOmicronsApplied++;
                }
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

            foreach (UnitData unit in m_filteredUnitData.OrderBy(a => a.PlayerName))
            {
                if (unit.OldRelicTier < unit.NewRelicTier)
                {
                    relicTiers.AppendLine(HTMLConstructor.AddTableData(new string[] { unit.PlayerName, unit.UnitName, $"{unit.OldRelicTier} > {unit.NewRelicTier}" }));
                    m_reportSummary.TotalRelicLevelsIncreased = m_reportSummary.TotalRelicLevelsIncreased + (unit.NewRelicTier - unit.OldRelicTier);
                }
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

            foreach (UnitData unit in m_filteredUnitData.OrderBy(a => a.PlayerName))
            {
                if (unit.OldRarity < 7 && unit.NewRarity == 7)
                {
                    units.AppendLine(HTMLConstructor.AddTableData(new string[] { unit.PlayerName, unit.UnitName }));
                    m_reportSummary.TotalSevenStarToons++;
                }
            }

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon" }, units.ToString()));
                        
            sb.AppendLine("<p/></div><div>");

            sb.AppendLine("This section highlights all of the ships that have been 7*'ed since the last snapshot.");

            StringBuilder ships = new StringBuilder();
            foreach (ShipData ship in m_filteredShipData.OrderBy(a => a.PlayerName))
            {
                if (ship.OldRarity < 7 && ship.NewRarity == 7)
                {
                    ships.AppendLine(HTMLConstructor.AddTableData(new string[] { ship.PlayerName, ship.ShipName }));
                    m_reportSummary.TotalSevenStarShips++;
                }
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

            m_reportSummary.TotalGuildPowerIncrease = m_dataBuilder.PlayerData.Sum(a => a.GalaticPowerDifference);            

            StringBuilder playerGPDiff = new StringBuilder();
            foreach (PlayerData player in m_dataBuilder.PlayerData.OrderByDescending(a => a.GalaticPowerDifference).Take(10))
                playerGPDiff.AppendLine(HTMLConstructor.AddTableData(new string[] { player.PlayerName, player.OldGalaticPower.ToString(), player.NewGalaticPower.ToString(), player.GalaticPowerDifference.ToString() }));

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Previous Galatic Power", "New Galatic Power", "Galatic Power Increase" }, playerGPDiff.ToString()));

            sb.AppendLine("<p/>");
                        
            StringBuilder playerGPPercentDiff = new StringBuilder();
            foreach (PlayerData player in m_dataBuilder.PlayerData.OrderByDescending(a => a.GalaticPowerPercentageDifference).Take(10))
                playerGPPercentDiff.AppendLine(HTMLConstructor.AddTableData(new string[] { player.PlayerName, player.OldGalaticPower.ToString(), player.NewGalaticPower.ToString(), player.GalaticPowerPercentageDifference.ToString() }));

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
            foreach (PlayerData player in m_dataBuilder.PlayerData.OrderBy(a => a.PlayerName).ToList())
                detailedPlayerData.AppendLine(HTMLConstructor.AddTableData(new string[] { player.PlayerName, player.OldGalaticPower.ToString(), player.NewGalaticPower.ToString(), player.GalaticPowerDifference.ToString(), player.GalaticPowerPercentageDifference.ToString() }));

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Previous Galatic Power", "New Galatic Power", "Galatic Power Increase", "Galatic Power % Increase" }, detailedPlayerData.ToString()));

            sb.AppendLine("</div><div>");
            sb.AppendLine("Here is the full list of toons within the guild, with their average stat in the guild and max.");

            StringBuilder detailedUnitData = new StringBuilder();
            StringBuilder detailedUnitDataStats = new StringBuilder();

            foreach (var unit in m_dataBuilder.UnitData.OrderBy(b => b.UnitName).GroupBy(a => a.UnitName).ToList())
            {                
                detailedUnitData.AppendLine(HTMLConstructor.AddTableData(new string[] {
                    unit.Key,
                    m_dataBuilder.UnitData.Where(b => b.UnitName == unit.Key).Average(a => a.NewPower).ToString("#."),
                    m_dataBuilder.UnitData.Where(b => b.UnitName == unit.Key).Max(a => a.NewPower).ToString(),
                    m_dataBuilder.UnitData.Where(b => b.UnitName == unit.Key).Average(a => a.NewGearLevel).ToString("#."),
                    m_dataBuilder.UnitData.Where(b => b.UnitName == unit.Key).Max(a => a.NewGearLevel).ToString(),                    
                    String.IsNullOrEmpty(m_dataBuilder.UnitData.Where(b => b.UnitName == unit.Key).Average(a => a.NewRelicTier).ToString("#.")) ? "0" : m_dataBuilder.UnitData.Where(b => b.UnitName == unit.Key).Average(a => a.NewRelicTier).ToString("#."),
                    m_dataBuilder.UnitData.Where(b => b.UnitName == unit.Key).Max(a => a.NewRelicTier).ToString(),
                }));

                detailedUnitDataStats.AppendLine(HTMLConstructor.AddTableData(new string[]
                {
                    unit.Key,                    
                    m_dataBuilder.UnitData.Where(b => b.UnitName == unit.Key).Average(a => a.CurrentHealth).ToString("#."),
                    m_dataBuilder.UnitData.Where(b => b.UnitName == unit.Key).Max(a => a.CurrentHealth).ToString(),
                    m_dataBuilder.UnitData.Where(b => b.UnitName == unit.Key).Average(a => a.CurrentProtection).ToString("#."),
                    m_dataBuilder.UnitData.Where(b => b.UnitName == unit.Key).Max(a => a.CurrentProtection).ToString(),
                    m_dataBuilder.UnitData.Where(b => b.UnitName == unit.Key).Average(a => a.CurrentSpeed).ToString("#."),
                    m_dataBuilder.UnitData.Where(b => b.UnitName == unit.Key).Max(a => a.CurrentSpeed).ToString(),
                    m_dataBuilder.UnitData.Where(b => b.UnitName == unit.Key).Average(a => a.CurrentPhysicalOffense).ToString("#."),
                    m_dataBuilder.UnitData.Where(b => b.UnitName == unit.Key).Max(a => a.CurrentPhysicalOffense).ToString(),
                    m_dataBuilder.UnitData.Where(b => b.UnitName == unit.Key).Average(a => a.CurrentSpecialOffense).ToString("#."),
                    m_dataBuilder.UnitData.Where(b => b.UnitName == unit.Key).Max(a => a.CurrentSpecialOffense).ToString(),
                }));
            }

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Toon", "Avg GP", "Max GP", "Avg Gear Lvl", "Max Gear Lvl", "Avg Relic Tier", "Max Relic Tier" }, detailedUnitData.ToString()));
            sb.AppendLine("</div>");
            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Toon", "Avg Health", "Max Health", "Avg Prot.", "Max Prot.", "Avg Speed", "Max Speed", "Avg PO", "Max PO", "Avg SO", "Max SO" }, detailedUnitDataStats.ToString()));

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
        private string TakeTopXOfStatAndReturnTableData(int amount, string stat, string[] properties, string toonName = null)
        {
            StringBuilder sb = new StringBuilder();

            IEnumerable<UnitData> units;

            if (!String.IsNullOrEmpty(toonName))
                units = m_dataBuilder.UnitData.Where(b => b.UnitName == toonName).OrderByDescending(a => a.GetType().GetProperty(stat).GetValue(a, null)).Take(amount);
            else
                units = m_dataBuilder.UnitData.OrderByDescending(a => a.GetType().GetProperty(stat).GetValue(a, null)).Take(amount);

            foreach (UnitData unit in units)
            {
                List<string> propertyValues = new List<string>();

                foreach (string property in properties)
                    propertyValues.Add(unit.GetType().GetProperty(property).GetValue(unit, null).ToString());

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

            foreach (UnitData unit in m_filteredUnitData.OrderBy(a => a.PlayerName))
            {
                if (unit.OldGearLevel < gearLevel && unit.NewGearLevel == gearLevel)
                {
                    sb.AppendLine(HTMLConstructor.AddTableData(new string[] { unit.PlayerName, unit.UnitName }));
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