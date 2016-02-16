using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    [StaticData(false)]
    public struct CommanderNameThemeSD
    {
        public string ThemeName;
        public List<CommanderNameSD> NameList;

        public bool Equals(CommanderNameThemeSD other)
        {
            return string.Equals(ThemeName, other.ThemeName);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            return obj is CommanderNameThemeSD && Equals((CommanderNameThemeSD)obj);
        }

        public override int GetHashCode()
        {
            return ThemeName?.GetHashCode() ?? 0;
        }
    }
}