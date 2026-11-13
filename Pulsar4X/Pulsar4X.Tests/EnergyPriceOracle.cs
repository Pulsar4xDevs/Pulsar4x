using System;
using Pulsar4X.Engine;

namespace Pulsar4X.Tests;

/// <summary>
/// Hand-derived embodied energy and prices for tests (energy economy phase 5). Written out from the
/// data files, not from EmbodiedEnergy, so a test that compares the two checks the calculator.
/// </summary>
static class EnergyPriceOracle
{
    /// <summary>Every mineral: the mine's extraction draw, 0.05 kWh per kg (installations.json "Power Draw").</summary>
    public const double Mineral_kWh = 0.05;

    /// <summary>materials.json methalox: 4,250 kWh and 4,250 hydrocarbons per 8,500 units.</summary>
    public const double Methalox_kWh = (4250 + 4250 * Mineral_kWh) / 8500; // 0.525

    /// <summary>materials.json stainless steel: 70 kWh, 88 iron + 11 chromium + 1 hydrocarbons per 100 units.</summary>
    public const double Steel_kWh = (70 + (88 + 11 + 1) * Mineral_kWh) / 100; // 0.75

    /// <summary>E x credits per kWh x (1 + markup), to the cent.</summary>
    public static decimal Ask(Game game, double kWh)
        => Cents(kWh * game.Settings.CreditsPerKWh * (1 + game.Settings.PriceMarkup));

    public static decimal Bid(Game game, double kWh)
        => Cents(kWh * game.Settings.CreditsPerKWh * (1 + game.Settings.PriceMarkup) * game.Settings.SeedBidShare);

    public static decimal FuelAsk(Game game) => Ask(game, Methalox_kWh);
    public static decimal IronAsk(Game game) => Ask(game, Mineral_kWh);
    public static decimal IronBid(Game game) => Bid(game, Mineral_kWh);
    public static decimal WaterAsk(Game game) => Ask(game, Mineral_kWh);

    /// <summary>Half the iron bid, to 4 places.</summary>
    public static decimal LotPrice(Game game) => Math.Max(0.01m, Math.Round(IronBid(game) / 2m, 4, MidpointRounding.AwayFromZero));

    static decimal Cents(double credits)
    {
        if (!(credits > 0))
            return 0;
        decimal d = Math.Round((decimal)credits, 2, MidpointRounding.AwayFromZero);
        return d < 0.01m ? 0.01m : d;
    }
}
