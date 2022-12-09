using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SWGOHDBInterface
{
    public class GuildDifference
    {
        public GuildDifference()
        {
            Players = new List<PlayerDifference>();
        }

        public int OldGP { get; set; }
        public int NewGP { get; set; }
        public int GPDifference => NewGP - OldGP;
        public List<PlayerDifference> Players { get; set; }
    }
}
