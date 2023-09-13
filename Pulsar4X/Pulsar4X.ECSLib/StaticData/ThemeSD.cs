using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    [StaticData(false)]
    public struct ThemeSD
    {
        public string Name;
        public List<string> FleetNames;
        public List<string> ShipNames;
        public List<string> FirstNames;
        public List<string> LastNames;
        public Dictionary<int, string> NavyRanks;
        public Dictionary<int, string> NavyRanksAbbreviations;

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