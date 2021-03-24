using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SWGOHReportBuilder
{
    public class Mod
    {
        public string ModSet { get; set; }

        public string ModPrimaryName { get; set; }

        public string ModRarity { get; set; }

        public string ModSecondaryOneName { get; set; }
        public string ModSecondaryTwoName { get; set; }
        public string ModSecondaryThreeName { get; set; }
        public string ModSecondaryFourName { get; set; }

        public decimal ModSecondaryOne { get; set; }
        public decimal ModSecondaryTwo { get; set; }
        public decimal ModSecondaryThree { get; set; }
        public decimal ModSecondaryFour { get; set; }
    }
}
