using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.Entities
{
    /// <summary>
    /// Theme that is chose by player when setting up Race for a game.
    /// Contains names for:
    /// -Ship Classes
    /// -Star Systems
    /// -Commander Ranks
    /// </summary>
    public class Theme
    {
        public int Id { get; set; }
        public string Name { get; set; }

        


        class ThemeRank
        {
            public int Rank { get; set; }
            public string Name { get; set; }
            public RankTypes RankType { get; set; }

            public enum RankTypes
            {
                Navy,
                Ground,
                Civilian
            }
        }

    }


}
