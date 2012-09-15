using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.Entities
{
    public class FactionCommanderTheme
    {
        private List<Tuple<int, Guid>> _distributions;
        /// <summary>
        /// Each Commander Theme for the owning faction has a percentage distribution
        /// This List holds the percentage and the Guid that relates to each selection.
        /// All percentages must add up to 100
        /// </summary>
        public List<Tuple<int, Guid>> Distributions
        {
            get { return _distributions; }
            set
            {
                _distributions = value;
                UpdateDistributions();
            }
        }

        public FactionCommanderTheme()
        {
            Distributions = new List<Tuple<int, Guid>>();
            _spreads = new List<Tuple<int, Guid>>();
        }

        private List<Tuple<int, Guid>> _spreads;
        private void UpdateDistributions()
        {
            _spreads.Clear();
            int totalSpread = 0;
            foreach (var distribution in Distributions)
            {
                totalSpread += distribution.Item1;
                _spreads.Add(new Tuple<int, Guid>(totalSpread, distribution.Item2));
            }
        }

        /// <summary>
        /// Given an Id, position and isFemale flag, will return a randomly chosen name that meets those criteria.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="isFemale">Defaults to false, not needed when position == LastName</param>
        /// <returns></returns>
        public string GetName(NamePosition position, bool isFemale = false)
        {
            var randomInt = MathUtilities.Random.Next(1, 101);

            foreach (var option in Distributions)
            {
                if (randomInt <= option.Item1)
                {
                    var theme = CommanderNameThemes.Instance.NameThemes.FirstOrDefault(x => x.Id == option.Item2);
                    if (theme == null) throw new Exception(string.Format("Commander Name Theme not found. Id: {0}", option.Item2));

                    var query = theme.NameEntries.Where(x => x.NamePosition == position &&
                                               (position != NamePosition.FirstName || x.IsFemale == isFemale)).ToList();
                    var entryCount = query.Count();
                    if (entryCount < 1)
                        throw new ArgumentOutOfRangeException(theme.Name, "No valid entries found in Commander Name Theme");

                    var randomIndex = MathUtilities.Random.Next(0, entryCount);
                    return query[randomIndex].Name;

                }
            }

            throw new Exception("No entry found in Commander Name Theme selections.");
        }
    }
}
