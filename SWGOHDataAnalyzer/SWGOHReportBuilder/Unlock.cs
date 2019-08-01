using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SWGOHReportBuilder
{    
    public class Unlock : GeneralData
    {
        public string UnitOrShipName { get; set; }

        public Unlock(string playerName, string name)
        {
            this.UnitOrShipName = name;
            this.PlayerName = playerName;
        }

    }
}
