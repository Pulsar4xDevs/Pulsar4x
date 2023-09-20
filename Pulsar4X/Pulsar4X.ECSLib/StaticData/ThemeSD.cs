using System.Collections.Generic;
using Pulsar4X.Modding;

namespace Pulsar4X.ECSLib
{
    [StaticData(false)]
    public class ThemeSD
    {
        public string Name { get; set; }
        public List<string> FleetNames { get; set; }
        public List<string> ShipNames { get; set; }
        public List<string> FirstNames { get; set; }
        public List<string> LastNames { get; set; }
        public Dictionary<int, string> NavyRanks { get; set; }
        public Dictionary<int, string> NavyRanksAbbreviations { get; set; }

        public bool Equals(ThemeSD other)
        {
            return string.Equals(Name, other.Name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            return obj is ThemeSD && Equals((ThemeSD)obj);
        }

        public override int GetHashCode()
        {
            return Name?.GetHashCode() ?? 0;
        }
    }
}