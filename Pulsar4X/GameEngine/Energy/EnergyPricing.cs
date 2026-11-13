using System;
using System.Collections.Generic;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Interfaces;
using Pulsar4X.Movement;
using Pulsar4X.Orbital;
using Pulsar4X.Storage;

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

        /// <summary>Seed ask at a depot: (E + <see cref="DepotDeliveryKWhPerKg"/> x kg per unit) priced, to the cent.</summary>
        public static decimal DepotSeedAsk(Game? game, FactionDataStore data, string cargoId)
            => Round(Credits(game, DeliveredKWh(game, data, cargoId)));

        /// <summary>Seed bid at a depot: <see cref="BidShare"/> of the unrounded delivered ask.</summary>
        public static decimal DepotSeedBid(Game? game, FactionDataStore data, string cargoId)
            => Round(Credits(game, DeliveredKWh(game, data, cargoId)) * BidShare(game));

        /// <summary>Embodied energy of one unit plus its delivery to the depot, kWh.</summary>
        public static double DeliveredKWh(Game? game, FactionDataStore data, string cargoId)
        {
            var energy = EmbodiedEnergy.For(data);
            double e = energy.PerUnit(cargoId);
            double perKg = DepotDeliveryKWhPerKg(game, data);
            if (perKg > 0 && data.CargoGoods.GetAny(cargoId) is ICargoable good && good.MassPerUnit > 0)
                e += perKg * good.MassPerUnit;
            else if (perKg > 0 && data.LockedCargoGoods.GetAny(cargoId) is ICargoable locked && locked.MassPerUnit > 0)
                e += perKg * locked.MassPerUnit;
            return e;
        }

        /// <summary>
        /// The stock asteroid miner (default-ship-design-miner, "Prospector I" in the warp-tech start)
        /// as move-cost numbers, with its starting methalox: what hauls belt ore to a depot. Kept as
        /// numbers because a depot is priced before any ship exists; a test checks them against the
        /// live ship.
        /// </summary>
        public static MoveCostShip ReferenceHauler => new()
        {
            DryMass_kg = 45394,
            FuelMass_kg = 2393601,
            FuelType = "methalox",
            ExhaustVelocity = 3615,
            FuelBurnRate = 600,
            ThrustInNewtons = 2169000,
            HasWarp = true,
            WarpPower = 2000000,
            WarpEnergyType = EmbodiedEnergy.ElectricityId,
            BubbleCreationCost = 4000000.0 / 3,
            BubbleSustainCost = 15,
            BubbleCollapseCost = 0,
            Coupling = 0.0025,
            BleedFraction = 0.5,
            EnergyStored = 0,
            EnergyStoreMax = 3000015,
            EnergyOutputMax = 15,
        };

        /// <summary>The reference hauler's general hold, kg (1,000 t of cargo holds).</summary>
        public const double ReferenceHold_kg = 1_000_000;

        /// <summary>
        /// The legs of the reference delivery (game settings DepotDelivery*): a warp of
        /// <see cref="GameSettings.DepotDeliveryDistance_AU"/> in the Sun's pull at
        /// <see cref="GameSettings.DepotDeliverySunDistance_AU"/>, back with a full hold, and the same
        /// leg out empty when it is a round trip. Empty with no distance.
        /// </summary>
        public static List<MoveCost> DepotDeliveryLegs(Game? game)
        {
            var legs = new List<MoveCost>();
            var settings = game?.Settings;
            double au = NonNegative(settings?.DepotDeliveryDistance_AU, 0.2);
            if (!(au > 0))
                return legs;
            double sunAu = NonNegative(settings?.DepotDeliverySunDistance_AU, 2.77);
            double radial = settings?.DepotDeliveryRadial is double r && double.IsFinite(r) ? Math.Clamp(r, -1, 1) : 0;
            double g = sunAu > 0
                ? GeneralMath.StandardGravitationalParameter(UniversalConstants.Units.SolarMassInKG)
                  / Math.Pow(sunAu * UniversalConstants.Units.MetersPerAu, 2)
                : 0;
            double distance = au * UniversalConstants.Units.MetersPerAu;
            var full = ReferenceHauler with { CargoMass_kg = ReferenceHold_kg };
            legs.Add(MoveCostCalculator.Warp(full, distance, g, radial));
            if (settings?.DepotDeliveryRoundTrip ?? true)
                legs.Add(MoveCostCalculator.Warp(ReferenceHauler, distance, g, radial));
            return legs;
        }

        /// <summary>Delivery to a depot per kg of cargo, kWh/kg: <see cref="FreightKWhPerKg(IEnumerable{MoveCost}, double, FactionDataStore)"/> of <see cref="DepotDeliveryLegs"/> over a full hold.</summary>
        public static double DepotDeliveryKWhPerKg(Game? game, FactionDataStore data)
            => FreightKWhPerKg(DepotDeliveryLegs(game), ReferenceHold_kg, data);

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
