using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    [StaticDataAttribute(false)]
    public struct CommanderNameThemeSD
    {
        public string ThemeName;
        public List<CommanderNameSD> NameList;
    }
}