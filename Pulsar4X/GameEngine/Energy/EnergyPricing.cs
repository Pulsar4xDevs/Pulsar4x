using System;
using System.Collections.Generic;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Movement;

namespace Pulsar4X.Energy
{
    /// <summary>
    /// Prices from embodied energy (§5): seed and ask = E x credits per kWh x (1 + markup), a seeded
    /// bid is a share of that; freight is the energy a move spends per kg of cargo, priced the same way.
    /// The anchor, markup and bid share are game settings (<see cref="GameSettings.CreditsPerKWh"/>).
    /// Prices are rounded to the cent, and a good with any energy is at least 0.01.
    /// </summary>
    public static class EnergyPricing
    {
        public const double DefaultCreditsPerKWh = 1.0;
        public const double DefaultMarkup = 4.0;
        public const double DefaultBidShare = 0.5;

        public static double CreditsPerKWh(Game? game) => Positive(game?.Settings?.CreditsPerKWh, DefaultCreditsPerKWh);
        public static double Markup(Game? game) => NonNegative(game?.Settings?.PriceMarkup, DefaultMarkup);
        public static double BidShare(Game? game) => Math.Clamp(NonNegative(game?.Settings?.SeedBidShare, DefaultBidShare), 0, 1);

        /// <summary>Credits for <paramref name="kWh"/> of embodied energy at the game's anchor and markup, unrounded.</summary>
        public static double Credits(Game? game, double kWh) => Math.Max(0, kWh) * CreditsPerKWh(game) * (1 + Markup(game));

        /// <summary>Seed ask for one unit of a good, from its embodied energy.</summary>
        public static decimal SeedAsk(Game? game, FactionDataStore data, string cargoId)
            => Round(Credits(game, EmbodiedEnergy.For(data).PerUnit(cargoId)));

        /// <summary>Seed bid for one unit of a good: <see cref="BidShare"/> of the unrounded ask.</summary>
        public static decimal SeedBid(Game? game, FactionDataStore data, string cargoId)
            => Round(Credits(game, EmbodiedEnergy.For(data).PerUnit(cargoId)) * BidShare(game));

        /// <summary>
        /// Freight energy of a move per kg of cargo, kWh/kg (the move-cost hook): reaction fuel burnt x
        /// that fuel's embodied energy per kg, plus warp energy (kJ, so / 3600), over the cargo mass.
        /// 0 with no cargo.
        /// </summary>
        public static double FreightKWhPerKg(MoveCost cost, double cargo_kg, FactionDataStore data)
        {
            if (!(cargo_kg > 0))
                return 0;
            var energy = EmbodiedEnergy.For(data);
            double kWh = 0;
            if (cost.FuelUsed_kg != null)
                foreach (var (fuelId, kg) in cost.FuelUsed_kg)
                    if (kg > 0 && double.IsFinite(kg))
                        kWh += kg * energy.PerKg(fuelId);
            if (cost.EnergyUsed_kJ > 0 && double.IsFinite(cost.EnergyUsed_kJ))
                kWh += cost.EnergyUsed_kJ / EmbodiedEnergy.KJPerKWh;
            return kWh / cargo_kg;
        }

        /// <summary>Freight price per kg of cargo, credits, unrounded (so it can be added to a delivered price).</summary>
        public static double FreightPerKg(Game? game, MoveCost cost, double cargo_kg, FactionDataStore data)
            => Credits(game, FreightKWhPerKg(cost, cargo_kg, data));

        /// <summary>Freight over several legs (e.g. burns and a warp), per kg of cargo, kWh/kg.</summary>
        public static double FreightKWhPerKg(IEnumerable<MoveCost> legs, double cargo_kg, FactionDataStore data)
        {
            double sum = 0;
            foreach (var leg in legs)
                sum += FreightKWhPerKg(leg, cargo_kg, data);
            return sum;
        }

        /// <summary>To the cent, away from zero; anything above 0 is at least 0.01.</summary>
        public static decimal Round(double credits)
        {
            if (!(credits > 0) || !double.IsFinite(credits))
                return 0;
            decimal d = Math.Round((decimal)credits, 2, MidpointRounding.AwayFromZero);
            return d < 0.01m ? 0.01m : d;
        }

        static double Positive(double? v, double fallback) => v is double d && double.IsFinite(d) && d > 0 ? d : fallback;
        static double NonNegative(double? v, double fallback) => v is double d && double.IsFinite(d) && d >= 0 ? d : fallback;
    }
}
