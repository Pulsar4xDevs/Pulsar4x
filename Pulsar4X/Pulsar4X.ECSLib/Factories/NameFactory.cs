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
            var rng = StaticRefLib.Game.RNG;
            var name = theme.FirstNames[rng.Next(0, theme.FirstNames.Count)] + " " + theme.LastNames[rng.Next(0, theme.LastNames.Count)];
            return name;
        }

        private static ThemeSD GetTheme()
        {
            return StaticRefLib.StaticData.Themes[StaticRefLib.GameSettings.CurrentTheme];
        }
    }
}