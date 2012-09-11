using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.Entities
{
    public class FactionCommanderTheme
    {
        /// <summary>
        /// Each Commander Theme for the owning faction has a percentage distribution
        /// This List holds the percentage and the Guid that relates to each selection.
        /// All percentages must add up to 100
        /// </summary>
        public List<Tuple<int, Guid>> Distributions { get; set; }

        public FactionCommanderTheme()
        {
            Distributions = new List<Tuple<int, Guid>>();
        }

        public string GetName(bool isFemale)
        {
            throw new NotImplementedException();
        }
    }
}
