using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SWGOHReportBuilder
{
    public class PlayerData
    {
        public string PlayerName { get; set; }

        public int OldGalaticPower { get; set; }

        public int NewGalaticPower { get; set; }

        public int GalaticPowerDifference { get; set; }

        public decimal GalaticPowerPercentageDifference { get; set; }

        public PlayerData() { }
    }
}
