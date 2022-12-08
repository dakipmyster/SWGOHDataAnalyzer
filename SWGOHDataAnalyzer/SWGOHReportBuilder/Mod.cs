﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SWGOHReportBuilder
{
    public class Mod
    {
        public int Id { get; set; }
        public string ModSet { get; set; }

        public string ModPrimaryName { get; set; }

        public string ModRarity { get; set; }

        public string ModShape { get; set; }


        public string ModSecondaryOneName { get; set; }
        public string ModSecondaryTwoName { get; set; }
        public string ModSecondaryThreeName { get; set; }
        public string ModSecondaryFourName { get; set; }

        public decimal ModSecondaryOne { get; set; }
        public decimal ModSecondaryTwo { get; set; }
        public decimal ModSecondaryThree { get; set; }
        public decimal ModSecondaryFour { get; set; }

        public string ModSecondaryOneRoll { get; set; }
        public string ModSecondaryTwoRoll { get; set; }
        public string ModSecondaryThreeRoll { get; set; }
        public string ModSecondaryFourRoll { get; set; }

        public string PlayerName { get; set; }
        public string UnitName { get; set; }
    }
}