using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SWGOHDBInterface
{
    public class PlayerDifference
    {
        public PlayerDifference()
        {
            Units = new List<UnitDifference>();
        }

        public int AllyCode { get; set; }
        public string Name { get; set; }
        public int OldGP { get; set; }
        public int NewGP { get; set; }
        public int GPDifference => NewGP - OldGP;
        public decimal GPPercentDifference => Math.Round(Decimal.Divide(GPDifference, OldGP) * 100, 3);
        public List<UnitDifference> Units { get; set; }
    }
}
