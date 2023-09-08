using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    [StaticData(false)]
    public struct ShipNameThemeSD
    {
        public string ThemeName;
        public List<string> Names;

        public bool Equals(ShipNameThemeSD other)
        {
            return string.Equals(ThemeName, other.ThemeName);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            return obj is ShipNameThemeSD && Equals((ShipNameThemeSD)obj);
        }

        public override int GetHashCode()
        {
            return ThemeName?.GetHashCode() ?? 0;
        }
    }
}