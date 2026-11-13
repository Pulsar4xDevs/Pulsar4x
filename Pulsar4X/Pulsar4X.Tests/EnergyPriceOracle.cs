using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Pulsar4X.Engine;
using Pulsar4X.Orbital;

namespace Pulsar4X.Tests;

/// <summary>
/// Hand-derived embodied energy and prices for tests (energy economy phase 5). Written out from the
/// data files and the delivery settings, not from EmbodiedEnergy or EnergyPricing, so a test that
/// compares the two checks the calculator.
/// </summary>
static class EnergyPriceOracle
{
    static Dictionary<string, double>? _extraction;

    /// <summary>"ExtractionEnergy" of every mineral, kWh/kg, read straight from minerals.json.</summary>
    public static IReadOnlyDictionary<string, double> ExtractionFromData
    {
        get
        {
            if (_extraction != null)
                return _extraction;
            var map = new Dictionary<string, double>();
            using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine("Data", "basemod", "TemplateFiles", "minerals.json")));
            foreach (var entry in doc.RootElement.EnumerateArray())
            {
                if (entry.GetProperty("Type").GetString() != "Mineral")
                    continue;
                var payload = entry.GetProperty("Payload");
                map[payload.GetProperty("UniqueID").GetString()!] = payload.GetProperty("ExtractionEnergy").GetDouble();
            }
            return _extraction = map;
        }
    }

    /// <summary>One mineral's extraction energy, kWh per kg (every mineral is 1 kg a unit).</summary>
    public static double Ext(string mineral) => ExtractionFromData[mineral];

    public static double Iron_kWh => Ext("iron");
    public static double Water_kWh => Ext("water");

    /// <summary>materials.json methalox: 4,250 kWh and 4,250 hydrocarbons per 8,500 units.</summary>
    public static double Methalox_kWh => (4250 + 4250 * Ext("hydrocarbons")) / 8500;

    /// <summary>materials.json stainless steel: 70 kWh, 88 iron + 11 chromium + 1 hydrocarbons per 100 units.</summary>
    public static double Steel_kWh => (70 + 88 * Ext("iron") + 11 * Ext("chromium") + 1 * Ext("hydrocarbons")) / 100;

    /// <summary>
    /// Delivery to the Ceres depot by hand, kWh per kg: the stock miner (dry 47,422 kg with its crew quarters, warp power 2 MW,
    /// bleed 0.5, bubble 4/3 GJ to open, 15 kW to hold) warps the settings' distance along the belt
    /// (radial 0 is priced as falling: bleed-capped at half its drive speed of
    /// int(2e6 / 47,422 x 1000) = 42,174 m/s; 44,058 before crew quarters), back full with 1,000 t and out empty.
    /// Energy per leg = 4e6/3 kJ + 15 kW x distance / speed; over the 1,000,000 kg hold.
    /// </summary>
    /// <summary>
    /// Embodied energy of fissile fuel, kWh/kg, from its recipe in the data (checked by
    /// EnergyPhase6Tests against EmbodiedEnergy).
    /// </summary>
    public const double Fissile_kWhPerKg = 400 + 100 * 10.0 + 10 * 0.3; // EnergyCost + 100 fissionables x 10 + 10 hydrocarbons x 0.3, one 1 kg unit

    public static double Delivery_kWhPerKg(Game game)
    {
        double au = game.Settings.DepotDeliveryDistance_AU;
        if (!(au > 0))
            return 0;
        double speed = 42174 * 0.5;
        double legKJ = 4e6 / 3 + 15 * (au * UniversalConstants.Units.MetersPerAu / speed);
        // Energy phase 6: the turbine's fissile fuel for that energy (50 kW of heat per 15 kW,
        // 40e9 kJ per kg) at its embodied energy.
        double fuelKWh = legKJ * 50 / 40e9 / 15 * Fissile_kWhPerKg;
        int legs = game.Settings.DepotDeliveryRoundTrip ? 2 : 1;
        return legs * (legKJ / 3600 + fuelKWh) / 1_000_000;
    }

    /// <summary>E x credits per kWh x (1 + markup), to the cent.</summary>
    public static decimal Ask(Game game, double kWh)
        => Cents(kWh * game.Settings.CreditsPerKWh * (1 + game.Settings.PriceMarkup));

    public static decimal Bid(Game game, double kWh)
        => Cents(kWh * game.Settings.CreditsPerKWh * (1 + game.Settings.PriceMarkup) * game.Settings.SeedBidShare);

    public static decimal FuelAsk(Game game) => Ask(game, Methalox_kWh + Delivery_kWhPerKg(game));
    public static decimal IronAsk(Game game) => Ask(game, Iron_kWh + Delivery_kWhPerKg(game));
    public static decimal IronBid(Game game) => Bid(game, Iron_kWh + Delivery_kWhPerKg(game));
    public static decimal WaterAsk(Game game) => Ask(game, Water_kWh + Delivery_kWhPerKg(game));

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
