using Pulsar4X.Blueprints;
using Pulsar4X.Engine;

namespace Pulsar4X.Names
{
    public class NameFactory
    {
        public static string GetSystemName(Game game)
        {
            var theme = GetTheme(game);
            return theme.SystemNames[game.RNG.Next(0, theme.SystemNames.Count)];
        }

        public static string GetShipName(Game game)
        {
            var theme = GetTheme(game);
            return theme.ShipNames[game.RNG.Next(0, theme.ShipNames.Count)];
        }

        public static string GetFleetName(Game game)
        {
            var theme = GetTheme(game);
            return theme.FleetNames[game.RNG.Next(0, theme.FleetNames.Count)];
        }

        public static string GetCommanderName(Game game)
        {
            var theme = GetTheme(game);
            var rng = game.RNG;
            var name = theme.FirstNames[rng.Next(0, theme.FirstNames.Count)] + " " + theme.LastNames[rng.Next(0, theme.LastNames.Count)];
            return name;
        }

        private static ThemeBlueprint GetTheme(Game game)
        {
            return game.Themes[game.Settings.CurrentTheme];
        }
    }
}