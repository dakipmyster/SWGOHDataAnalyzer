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
        string m_characterHighlight;

        List<UnitData> m_filteredUnitData;
        List<ShipData> m_filteredShipData;
        List<string> m_filteredPlayerNames;

        public ReportBuilder()
        {
            m_dataBuilder = new DataBuilder();
            m_filteredShipData = new List<ShipData>();
            m_filteredUnitData = new List<UnitData>();
            m_filteredPlayerNames = new List<string>();
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

            SWGOHMessageSystem.OutputMessage("Compiling Report Data....");

            tasks.Add(m_dataBuilder.CollectPlayerGPDifferences());
            tasks.Add(m_dataBuilder.CollectShipData());
            tasks.Add(m_dataBuilder.CollectUnitData());

            m_filteredUnitData = m_dataBuilder.UnitData.Where(a => a.OldGalaticPower != 0 && a.NewGalaticPower != 0).ToList();
            m_filteredShipData = m_dataBuilder.ShipData.Where(a => a.OldGalaticPower != 0 && a.NewGalaticPower != 0).ToList();
            m_filteredPlayerNames = m_dataBuilder.PlayerData.Select(a => a.PlayerName).ToList();

            await Task.WhenAll(tasks.ToArray());

            await BuildReport(fileName);
        }

        private async Task BuildReport(string fileName)
        {
            StringBuilder pdfString = new StringBuilder();

            string toonName = SWGOHMessageSystem.InputMessage("Please enter in the name of the toon you wish to highlight in the report.");

            pdfString.Append(@"<html><head><style>
table {
  border-collapse: collapse;
  page-break-inside:avoid
}
tr:nth-child(even) {background-color: #f2f2f2;}
th, td{
  padding: 2px;
}
</style></head><body>");
            List<Task> tasks = new List<Task>();

            pdfString.AppendLine("<a href=\"#testingthis\">Gold Teams</a>");

            tasks.Add(SevenStarSection());
            tasks.Add(GearTwelveToons());
            tasks.Add(GearThirteenToons());
            tasks.Add(ZetasApplied());
            tasks.Add(JourneyOrLegendaryUnlock());
            tasks.Add(JourneyPrepared());
            tasks.Add(GoldMembers());
            tasks.Add(PlayerGPDifferences());
            tasks.Add(UnitGPDifferences());
            tasks.Add(TopTwentySection());
            tasks.Add(DetailedData());
            tasks.Add(CharacterHighlight(toonName));

            await Task.WhenAll(tasks.ToArray());

            SWGOHMessageSystem.OutputMessage("Rendering Report....");

            pdfString.AppendLine(m_playerGPDifferences);
            pdfString.AppendLine(m_UnitGPDifferences);
            pdfString.AppendLine(m_topTwentySection);
            pdfString.AppendLine(m_sevenStarSection);
            pdfString.AppendLine(m_gearTwelveToons);
            pdfString.AppendLine(m_gearThirteenToons);
            pdfString.AppendLine(m_zetasApplied);
            pdfString.AppendLine(m_journeyOrLegendaryUnlock);
            pdfString.AppendLine(m_journeyPrepared);
            pdfString.AppendLine(m_characterHighlight);
            pdfString.AppendLine(m_goldMembers);
            pdfString.AppendLine(m_detailedData);

            pdfString.AppendLine(@"</body></html>");

            string folderPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\SWGOHDataAnalyzer\\{fileName}.pdf";

            using (FileStream fs = File.Open(folderPath, FileMode.OpenOrCreate))
                HtmlConverter.ConvertToPdf(pdfString.ToString(),fs, new ConverterProperties());

            SWGOHMessageSystem.OutputMessage($"Report saved at {folderPath}");
        }

        private async Task CharacterHighlight(string toonName)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"This section goes over a specific character to highlight and will rotate every report.  This iteration is {toonName}.  The report takes the top 10 of {toonName}'s stats");
            sb.AppendLine("<p/>");

            sb.AppendLine(HTMLConstructor.TableGroupStart());           

            sb.Append(HTMLConstructor.AddTable(new string[] { "Player Name", "Health" }, TakeTopXOfStatAndReturnTableData(10, "CurrentHealth", new string[] {"PlayerName", "CurrentHealth" }, toonName), "Health"));

            sb.Append(HTMLConstructor.TableGroupNext());

            sb.Append(HTMLConstructor.AddTable(new string[] { "Player Name", "Protection" }, TakeTopXOfStatAndReturnTableData(10, "CurrentProtection", new string[] { "PlayerName", "CurrentProtection" }, toonName), "Protection"));
            
            sb.Append(HTMLConstructor.TableGroupNext());
           
            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Tankiest" }, TakeTopXOfStatAndReturnTableData(10, "CurrentTankiest", new string[] { "PlayerName", "CurrentTankiest" }, toonName), "Tankiest"));

            sb.AppendLine(HTMLConstructor.TableGroupEnd());
            sb.AppendLine(HTMLConstructor.TableGroupStart());

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Speed" }, TakeTopXOfStatAndReturnTableData(10, "CurrentSpeed", new string[] { "PlayerName", "CurrentSpeed" }, toonName), "Speed"));

            sb.Append(HTMLConstructor.TableGroupNext());

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "PO" }, TakeTopXOfStatAndReturnTableData(10, "CurrentPhysicalOffense", new string[] { "PlayerName", "CurrentPhysicalOffense" }, toonName), "Physical Offense"));

            sb.Append(HTMLConstructor.TableGroupNext());
             
            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "SO" }, TakeTopXOfStatAndReturnTableData(10, "CurrentSpecialOffense", new string[] { "PlayerName", "CurrentSpecialOffense" }, toonName), "Special Offense"));

            sb.AppendLine(HTMLConstructor.TableGroupEnd());
            sb.AppendLine(HTMLConstructor.TableGroupStart());

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "PD" }, TakeTopXOfStatAndReturnTableData(10, "CurrentPhysicalDefense", new string[] { "PlayerName", "CurrentPhysicalDefense" }, toonName), "Physical Defense"));

            sb.Append(HTMLConstructor.TableGroupNext());

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "SD" }, TakeTopXOfStatAndReturnTableData(10, "CurrentSpecialDefense", new string[] { "PlayerName", "CurrentSpecialDefense" }, toonName), "Special Defense"));

            sb.Append(HTMLConstructor.TableGroupNext());

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "PCC" }, TakeTopXOfStatAndReturnTableData(10, "CurrentPhysicalCritChance", new string[] { "PlayerName", "CurrentPhysicalCritChance" }, toonName), "Physical CC"));

            sb.AppendLine(HTMLConstructor.TableGroupEnd());
            sb.AppendLine(HTMLConstructor.TableGroupStart());

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "SCC" }, TakeTopXOfStatAndReturnTableData(10, "CurrentSpecialCritChance", new string[] { "PlayerName", "CurrentSpecialCritChance" }, toonName), "Special CC"));

            sb.Append(HTMLConstructor.TableGroupNext());
             
            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Potency" }, TakeTopXOfStatAndReturnTableData(10, "CurrentPotency", new string[] { "PlayerName", "CurrentPotency" }, toonName), "Potency"));

            sb.Append(HTMLConstructor.TableGroupNext());

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Tenacity" }, TakeTopXOfStatAndReturnTableData(10, "CurrentTenacity", new string[] { "PlayerName", "CurrentTenacity" }, toonName), "Tenacity"));

            sb.AppendLine(HTMLConstructor.TableGroupEnd());
            sb.AppendLine(HTMLConstructor.TableGroupStart());

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Galatic Power", "Gear Level" }, TakeTopXOfStatAndReturnTableData(10, "NewPower", new string[] { "PlayerName", "NewPower", "NewGearLevel" }, toonName), "Highest Galatic Power"));

            sb.AppendLine(HTMLConstructor.TableGroupEnd());

            sb.AppendLine("<p/>");

            m_characterHighlight = sb.ToString();

            await Task.CompletedTask;
        }

        private async Task UnitGPDifferences()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("This section highlights the top 20 toons who have had the greatest GP increase.");
            
            StringBuilder unitGPDiff = new StringBuilder();
            foreach (UnitData unit in m_filteredUnitData.OrderByDescending(a => a.PowerDifference).Take(20))
                unitGPDiff.AppendLine(HTMLConstructor.AddTableData(new string[] { unit.PlayerName, unit.UnitName, unit.OldPower.ToString(), unit.NewPower.ToString(), unit.PowerDifference.ToString() }));

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon", "Old Power", "New Power", "Power Increase" }, unitGPDiff.ToString()));
            sb.AppendLine("<p/>");

            m_UnitGPDifferences = sb.ToString();

            await Task.CompletedTask;            
        }

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

            sb.AppendLine("<div id=\"testingthis\"></div>");

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
                if (m_dataBuilder.UnitData.Where(a => a.NewRarity == 7 && a.NewPower > 17499 && a.PlayerName == player &&
                    (a.UnitName == "Bastila Shan (Fallen)" ||
                     a.UnitName == "Canderous Ordo" ||
                     a.UnitName == "Carth Onasi" ||
                     a.UnitName == "HK-47" ||
                     a.UnitName == "Juhani")
                    ).Count() > 3)
                {
                    if (m_dataBuilder.UnitData.Where(a => a.NewRarity == 7 && a.NewPower > 17499 && a.PlayerName == player &&
                        (a.UnitName == "T3-M4" ||
                         a.UnitName == "Mission Vao" ||
                         a.UnitName == "Zaalbar" ||
                         a.UnitName == "Jolee Bindo" ||
                         a.UnitName == "Bastila Shan")
                        ).Count() > 3)
                    {
                        if (m_dataBuilder.UnitData.Where(a => a.NewRarity == 7 && a.NewPower > 17499 && a.PlayerName == player && a.UnitName == "Jedi Knight Revan").Count() == 1 &&
                            m_dataBuilder.UnitData.Where(a => a.NewRarity == 7 && a.NewPower > 17499 && a.PlayerName == player && a.UnitName == "Darth Revan").Count() == 1)
                        {
                            unlocks.Add(new Unlock(player, "Darth Malak"));
                        }
                    }
                }
            }

            StringBuilder prepaired = new StringBuilder();
            foreach (Unlock unlock in unlocks.OrderBy(a => a.PlayerName))
                prepaired.AppendLine(HTMLConstructor.AddTableData(new string[] { unlock.PlayerName, unlock.UnitOrShipName }));

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon" }, prepaired.ToString()));

            sb.AppendLine("<p/>");

            m_journeyPrepared = sb.ToString();

            await Task.CompletedTask;
        }

        private async Task JourneyOrLegendaryUnlock()
        {
            StringBuilder sb = new StringBuilder();
            List<Unlock> unlocks = new List<Unlock>();

            sb.AppendLine("This section highlights all players who have unlocked a Legendary or Journey toon/ship.");            

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
                a.UnitName == "Darth Malak"
            )).ToList();

            foreach (UnitData unit in filteredUnitList.OrderBy(a => a.PlayerName))
                unlocks.Add(new Unlock(unit.PlayerName, unit.UnitName));

            var filteredShipList = m_dataBuilder.ShipData.Where(a => a.OldRarity == 0 && a.NewRarity != 0 && m_filteredPlayerNames.Contains(a.PlayerName) && (
                a.ShipName == "Chimaera" ||
                a.ShipName == "Han's Millennium Falcon"
            )).ToList();

            foreach (ShipData ship in filteredShipList.OrderBy(a => a.PlayerName))
                unlocks.Add(new Unlock(ship.PlayerName, ship.ShipName));

            StringBuilder unlockedRows = new StringBuilder();
            foreach(Unlock unlock in unlocks.OrderBy(a => a.PlayerName))
                unlockedRows.AppendLine(HTMLConstructor.AddTableData(new string[] { unlock.PlayerName, unlock.UnitOrShipName }));

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon" }, unlockedRows.ToString()));
            sb.AppendLine("<p/>");

            m_journeyOrLegendaryUnlock = sb.ToString();

            await Task.CompletedTask;
        }

        private async Task ZetasApplied()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("This section highlights all of the toons that have been given zetas since the last snapshot.");
            
            StringBuilder zetas = new StringBuilder();
            foreach (UnitData unit in m_filteredUnitData.OrderBy(a => a.PlayerName))
                if (unit.OldZetas.Count < unit.NewZetas.Count)
                    zetas.AppendLine(HTMLConstructor.AddTableData(new string[] { unit.PlayerName, unit.UnitName, string.Join(",", unit.NewZetas.Except(unit.OldZetas).ToArray()) }));
                
            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon", "Zetas" }, zetas.ToString()));

            sb.AppendLine("<p/>");

            m_zetasApplied = sb.ToString();

            await Task.CompletedTask;
        }

        private async Task GearThirteenToons()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("This section highlights all of the toons that have been geared to 13 since the last snapshot.");

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon" }, GetAllToonsOfGearLevelDifference(12)));

            sb.AppendLine("<p/>");
                        
            m_gearThirteenToons = sb.ToString();

            await Task.CompletedTask;
        }

        private async Task GearTwelveToons()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("This section highlights all of the toons that have been geared to 12 since the last snapshot.");            

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon" }, GetAllToonsOfGearLevelDifference(12)));

            sb.AppendLine("<p/>");

            m_gearTwelveToons = sb.ToString();

            await Task.CompletedTask;
        }

        private async Task TopTwentySection()
        {
            StringBuilder sb = new StringBuilder();

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
            sb.AppendLine("<p/>");

            m_topTwentySection = sb.ToString();

            await Task.CompletedTask;
        }

        private async Task SevenStarSection()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("This section highlights all of the toons that have been 7*'ed since the last snapshot.");
           
            StringBuilder units = new StringBuilder();
            foreach (UnitData unit in m_filteredUnitData.OrderBy(a => a.PlayerName))
                if (unit.OldRarity < 7 && unit.NewRarity == 7)
                    units.AppendLine(HTMLConstructor.AddTableData(new string[] { unit.PlayerName, unit.UnitName }));

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon" }, units.ToString()));
                        
            sb.AppendLine("<p/>");

            sb.AppendLine("This section highlights all of the ships that have been 7*'ed since the last snapshot.");

            StringBuilder ships = new StringBuilder();
            foreach (ShipData ship in m_filteredShipData.OrderBy(a => a.PlayerName))
                units.AppendLine(HTMLConstructor.AddTableData(new string[] { ship.PlayerName, ship.ShipName }));
            
            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Toon" }, ships.ToString()));

            sb.AppendLine("<p/>");

            m_sevenStarSection = sb.ToString();

            await Task.CompletedTask;
        }

        private async Task PlayerGPDifferences()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("This section goes over the Galatic Power (GP) differences for players between snapshots.  Here is the top ten players who have gained the most Galatic Power by total and by percentage from the previous snapshot.");
            
            StringBuilder playerGPDiff = new StringBuilder();
            foreach (PlayerData player in m_dataBuilder.PlayerData.OrderByDescending(a => a.GalaticPowerDifference).Take(10))
                playerGPDiff.AppendLine(HTMLConstructor.AddTableData(new string[] { player.PlayerName, player.OldGalaticPower.ToString(), player.NewGalaticPower.ToString(), player.GalaticPowerDifference.ToString() }));

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Previous Galatic Power", "New Galatic Power", "Galatic Power Increase" }, playerGPDiff.ToString()));

            sb.AppendLine("<p/>");
                        
            StringBuilder playerGPPercentDiff = new StringBuilder();
            foreach (PlayerData player in m_dataBuilder.PlayerData.OrderByDescending(a => a.GalaticPowerPercentageDifference).Take(10))
                playerGPPercentDiff.AppendLine(HTMLConstructor.AddTableData(new string[] { player.PlayerName, player.OldGalaticPower.ToString(), player.NewGalaticPower.ToString(), player.GalaticPowerPercentageDifference.ToString() }));

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Previous Galatic Power", "New Galatic Power", "Galatic Power % Increase" }, playerGPPercentDiff.ToString()));
            sb.AppendLine("<p/>");

            m_playerGPDifferences = sb.ToString();

            await Task.CompletedTask;
        }

        private async Task DetailedData()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("For those who are interested, here is some full table data that the stats refer to.");
            sb.AppendLine("Here is the full list of players and their Galatic Power differences.");

            sb.AppendLine("<p/>");
            
            StringBuilder detailedData = new StringBuilder();
            foreach (PlayerData player in m_dataBuilder.PlayerData.OrderBy(a => a.PlayerName).ToList())
                detailedData.AppendLine(HTMLConstructor.AddTableData(new string[] { player.PlayerName, player.OldGalaticPower.ToString(), player.NewGalaticPower.ToString(), player.GalaticPowerDifference.ToString(), player.GalaticPowerPercentageDifference.ToString() }));

            sb.AppendLine(HTMLConstructor.AddTable(new string[] { "Player Name", "Previous Galatic Power", "New Galatic Power", "Galatic Power Increase", "Galatic Power % Increase" }, detailedData.ToString()));

            m_detailedData = sb.ToString();

            await Task.CompletedTask;
        }

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

        private string GetAllToonsOfGearLevelDifference(int gearLevel)
        {
            StringBuilder sb = new StringBuilder();

            foreach (UnitData unit in m_filteredUnitData.OrderBy(a => a.PlayerName))
                if (unit.OldGearLevel < gearLevel && unit.NewGearLevel == gearLevel)
                    sb.AppendLine(HTMLConstructor.AddTableData(new string[] { unit.PlayerName, unit.UnitName }));

            return sb.ToString();
        }

        private List<string> FindGoldTeamPlayers(IEnumerable<IGrouping<string, string>> potentialPlayers, int count)
        {
            List<string> players = new List<string>();

            foreach (var player in potentialPlayers)
                if (player.Count() == count)
                    players.Add(player.Key);

            return players;
        }
    }
}