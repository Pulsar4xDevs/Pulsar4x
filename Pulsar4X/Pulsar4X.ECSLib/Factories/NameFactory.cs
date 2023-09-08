namespace Pulsar4X.ECSLib
{
    public class NameFactory
    {
        public static string GetShipName()
        {
            var theme = GetTheme();
            return theme.ShipNames[StaticRefLib.Game.RNG.Next(0, theme.ShipNames.Count)];
        }

        public static string GetFleetName()
        {
            var theme = GetTheme();
            return theme.FleetNames[StaticRefLib.Game.RNG.Next(0, theme.FleetNames.Count)];
        }

        public static string GetCommanderName()
        {
            var theme = GetTheme();
            return theme.CommanderNames[StaticRefLib.Game.RNG.Next(0, theme.CommanderNames.Count)];
        }

        private static ThemeSD GetTheme()
        {
            return StaticRefLib.StaticData.Themes[StaticRefLib.GameSettings.CurrentTheme];
        }
    }
}