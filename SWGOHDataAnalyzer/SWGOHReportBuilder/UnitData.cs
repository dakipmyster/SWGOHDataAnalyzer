using System.Collections.Generic;

namespace SWGOHReportBuilder
{
    public class UnitData : GeneralData
    {
        public string UnitName { get; set; }

        public int OldRarity { get; set; }

        public int NewRarity { get; set; }

        public int OldGearLevel { get; set; }

        public int NewGearLevel { get; set; }

        public int OldRelicTier { get; set; }

        public int NewRelicTier { get; set; }

        public List<string> OldZetas { get; set; }

        public List<string> NewZetas { get; set; }

        public int OldPower { get; set; }

        public int NewPower { get; set; }

        public int OldLevel { get; set; }

        public int NewLevel { get; set; }

        public int PowerDifference { get; set; }

        public decimal CurrentHealth { get; set; }

        public decimal CurrentProtection { get; set; }

        public decimal CurrentTankiest { get; set; }

        public decimal CurrentSpeed { get; set; }

        public decimal CurrentPhysicalOffense { get; set; }

        public decimal CurrentSpecialOffense { get; set; }

        public decimal CurrentPhysicalDefense { get; set; }

        public decimal CurrentSpecialDefense { get; set; }

        public decimal CurrentPhysicalCritChance { get; set; }

        public decimal CurrentSpecialCritChance { get; set; }

        public decimal CurrentPotency { get; set; }

        public decimal CurrentTenacity { get; set; }

        public UnitData()
        {
            OldZetas = new List<string>();
            NewZetas = new List<string>();
        }
    }
}
