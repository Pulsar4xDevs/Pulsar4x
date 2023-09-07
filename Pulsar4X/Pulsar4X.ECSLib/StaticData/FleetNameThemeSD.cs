using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    [StaticData(false)]
    public struct FleetNameThemeSD
    {
        public string ThemeName;
        public List<string> Names;

        public bool Equals(FleetNameThemeSD other)
        {
            return string.Equals(ThemeName, other.ThemeName);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            return obj is FleetNameThemeSD && Equals((FleetNameThemeSD)obj);
        }

        public override int GetHashCode()
        {
            return ThemeName?.GetHashCode() ?? 0;
        }
    }
}