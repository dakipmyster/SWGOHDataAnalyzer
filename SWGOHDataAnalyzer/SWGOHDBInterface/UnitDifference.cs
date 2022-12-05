using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SWGOHDBInterface
{
    public class UnitDifference
    {
        public UnitDifference()
        {
            NewZetas = new List<string>();
            NewOmicrons = new List<string>();
        }

        public bool IsShip { get; set; }
        public string Name { get; set; }
        public int OldGP { get; set; }
        public int NewGP { get; set; }
        public int GPDifference => NewGP - OldGP;
        public int OldRelicTier { get; set; }
        public int NewRelicTier { get; set; }
        public int RelicTierDifference => NewRelicTier - OldRelicTier;
        public int OldRarity { get; set; }
        public int NewRarity { get; set; }
        public int OldGearLevel { get; set; }
        public int NewGearLevel { get; set; }
        public List<string> NewZetas { get; set; }
        public List<string> NewOmicrons { get; set; }

    }
}
