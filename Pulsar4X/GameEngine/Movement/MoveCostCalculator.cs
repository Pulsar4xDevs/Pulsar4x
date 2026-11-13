using System;
using System.Collections.Generic;
using Pulsar4X.Energy;
using Pulsar4X.Engine;
using Pulsar4X.Galaxy;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;

namespace Pulsar4X.Movement;

/// <summary>
/// What a ship brings to a move, as plain numbers. Build it from a live ship with
/// <see cref="FromEntity"/>, or fill it from a design for a ship that does not exist yet,
/// then change <see cref="CargoMass_kg"/> or <see cref="FuelMass_kg"/> with <c>with</c>.
/// Units follow the blobs it is read from: kg, m/s, kg/s, N, kW and kJ.
/// </summary>
public record struct MoveCostShip
{
    /// <summary><see cref="MassVolumeDB.MassDry"/>.</summary>
    public double DryMass_kg;
    /// <summary>Stored cargo that is not the reaction drive's fuel.</summary>
    public double CargoMass_kg;
    /// <summary>Stored mass of <see cref="FuelType"/>.</summary>
    public double FuelMass_kg;

    // Reaction drive, from NewtonThrustAbilityDB.
    public string FuelType;
    public double ExhaustVelocity;
    public double FuelBurnRate;
    public double ThrustInNewtons;

    // Warp drive, from WarpAbilityDB.
    public bool HasWarp;
    public double WarpPower;
    public string WarpEnergyType;
    public double BubbleCreationCost;
    public double BubbleSustainCost;
    public double BubbleCollapseCost;
    public double Coupling;
    public double BleedFraction;

    // Power, from EnergyGenAbilityDB, for WarpEnergyType.
    public double EnergyStored;
    public double EnergyStoreMax;
    public double EnergyOutputMax;

    // Where that output comes from (energy phase 6), dispatched in this order: free sources
    // (solar, isotope generators), the reactor, then fuel cells. The fuel fields price the
    // warp's energy in kg; zero output or fuel per kJ leaves the move's FuelUsed_kg empty.
    /// <summary>Solar plus isotope output, kW. Burns nothing.</summary>
    public double FreeOutputMax;
    /// <summary>Reactor output while it has fuel, kW.</summary>
    public double ReactorOutputMax;
    /// <summary>Reactor fuel id (EnergyGenAbilityDB.TotalFuelUseAtMax.type).</summary>
    public string ReactorFuelType;
    /// <summary>Reactor fuel per kJ made: fuel use at full output / full output, kg/kJ.</summary>
    public double ReactorFuelPerKJ;
    /// <summary>Fuel-cell output while the hold has their propellant, kW.</summary>
    public double CellOutputMax;
    /// <summary>Fuel-cell propellant id.</summary>
    public string CellFuelType;
    /// <summary>Propellant per kJ the cells make, kg/kJ.</summary>
    public double CellFuelPerKJ;

    /// <summary>Same sum as <see cref="MassVolumeDB.UpdateMassTotal(Pulsar4X.Storage.CargoStorageDB)"/>.</summary>
    public double MassTotal_kg => DryMass_kg + CargoMass_kg + FuelMass_kg;

    /// <summary>Read a live ship. Missing blobs leave their fields at zero.</summary>
    public static MoveCostShip FromEntity(Entity ship)
    {
        var spec = new MoveCostShip();
        double total = 0;
        if (ship.TryGetDataBlob<MassVolumeDB>(out var mass))
        {
            spec.DryMass_kg = mass.MassDry;
            total = mass.MassTotal;
        }

        if (ship.TryGetDataBlob<NewtonThrustAbilityDB>(out var thrust))
        {
            spec.FuelType = thrust.FuelType;
            spec.ExhaustVelocity = thrust.ExhaustVelocity;
            spec.FuelBurnRate = thrust.FuelBurnRate;
            spec.ThrustInNewtons = thrust.ThrustInNewtons;
            spec.FuelMass_kg = thrust.TotalFuel_kg;
        }
        spec.CargoMass_kg = Math.Max(0, total - spec.DryMass_kg - spec.FuelMass_kg);

        if (ship.TryGetDataBlob<WarpAbilityDB>(out var warp))
        {
            spec.HasWarp = true;
            spec.WarpPower = warp.TotalWarpPower;
            spec.WarpEnergyType = warp.EnergyType;
            spec.BubbleCreationCost = warp.BubbleCreationCost;
            spec.BubbleSustainCost = warp.BubbleSustainCost;
            spec.BubbleCollapseCost = warp.BubbleCollapseCost;
            spec.Coupling = warp.Coupling;
            spec.BleedFraction = warp.BleedFraction;
        }

        if (ship.TryGetDataBlob<EnergyGenAbilityDB>(out var power))
        {
            spec.EnergyOutputMax = power.TotalOutputMax;
            spec.FreeOutputMax = power.MaxOutputFromSolar + power.DecayOutputMax;
            spec.ReactorOutputMax = power.ReactorOutputMax;
            spec.ReactorFuelType = power.TotalFuelUseAtMax.type;
            spec.ReactorFuelPerKJ = power.MaxOutputFromReactor > 0 ? power.TotalFuelUseAtMax.maxUse / power.MaxOutputFromReactor : 0;
            spec.CellOutputMax = power.CellsOutputMax;
            spec.CellFuelType = power.CellFuelUseAtMax.type;
            spec.CellFuelPerKJ = power.MaxOutputFromCells > 0 ? power.CellFuelUseAtMax.maxUse / power.MaxOutputFromCells : 0;
            if (!string.IsNullOrEmpty(spec.WarpEnergyType))
            {
                spec.EnergyStored = power.EnergyStored.TryGetValue(spec.WarpEnergyType, out double stored) ? stored : 0;
                spec.EnergyStoreMax = power.EnergyStoreMax.TryGetValue(spec.WarpEnergyType, out double max) ? max : 0;
            }
        }

        return spec;
    }
}

public enum MoveCostMode { Warp, Burn, Hohmann }

/// <summary>
/// What one move costs. Costs are filled in when the move is infeasible too, so a caller can
/// say how short the ship is. <see cref="Reason"/> is only set when the move cannot be made;
/// <see cref="Note"/> carries information such as the warp limit line.
/// </summary>
public readonly record struct MoveCost(
    MoveCostMode Mode,
    bool Feasible,
    string Reason,
    string Note,
    IReadOnlyDictionary<string, double> FuelUsed_kg,
    double EnergyUsed_kJ,
    double DeltaV,
    double Duration_s,
    double BurnSeconds,
    double ChargeWait_s,
    double Speed);

/// <summary>
/// Pure cost of a move order: reaction fuel for burns, warp energy for jumps. Reads nothing
/// from the game and changes nothing. Every formula is the one the movement code runs;
/// see /workspace/pulsar4x-team/plans/move-order-cost.md and the comments below for where.
/// </summary>
public static class MoveCostCalculator
{
    static readonly IReadOnlyDictionary<string, double> NoFuel = new Dictionary<string, double>();

    /// <summary>
    /// A warp jump of <paramref name="distance_m"/>. Pass the star's pull
    /// <paramref name="gravity_ms2"/> at the start and the heading's cosine against the
    /// outward line, <paramref name="radial"/>, to price the well as
    /// <see cref="WarpMath.PriceCruise"/> does. Gravity 0 prices free space at drive speed.
    /// </summary>
    public static MoveCost Warp(MoveCostShip ship, double distance_m, double gravity_ms2 = 0, double radial = 0)
    {
        const MoveCostMode mode = MoveCostMode.Warp;
        if (!ship.HasWarp)
            return No(mode, "no warp drive");

        // WarpAbilityDB.MaxSpeed: MaxSpeedCalc(TotalWarpPower, dry mass). Open-space speed is the
        // empty ship's; cargo does not change it.
        double drive = ship.DryMass_kg > 0 ? WarpMath.MaxSpeedCalc(ship.WarpPower, ship.DryMass_kg) : 0;
        if (!(drive > 0))
            return No(mode, "warp drive produces no speed");

        // WarpMath.PriceHeading prices the well with MassVolumeDB.MassTotal, so climbing does
        // scale with cargo. No well flies at drive speed on the base sustain.
        double mass = ship.MassTotal_kg;
        var price = gravity_ms2 > 0 && double.IsFinite(gravity_ms2)
            ? WarpMath.PriceInWell(ship.Coupling, ship.BleedFraction, ship.EnergyOutputMax, mass,
                gravity_ms2, radial, drive, ship.BubbleSustainCost)
            : (speed: drive, demandKW: ship.BubbleSustainCost, limitLine: "");
        double speed = price.speed > 0 && double.IsFinite(price.speed) ? price.speed : drive;
        double duration = Math.Max(0, distance_m) / speed;
        // Same gate as the live warp (WarpMath.PriceCruise): the plant holds the demand and the
        // speed reaches WarpMath.MinWarpSpeed_ms.
        string refusal = WarpMath.WarpRefusal(speed, price.demandKW, ship.EnergyOutputMax);

        // WarpMoveProcessor.TryStartWarp and FinishArrival book creation and collapse as one
        // second of that many kW; the sustain demand runs from entry to exit.
        double energy = ship.BubbleCreationCost + price.demandKW * duration + ship.BubbleCollapseCost;

        // WarpMoveAction.Execute: short of the creation cost waits to charge, unless the store
        // can never hold it or nothing generates.
        double wait = 0;
        bool feasible = string.IsNullOrEmpty(refusal);
        string reason = refusal;
        if (feasible && ship.BubbleCreationCost > ship.EnergyStored)
        {
            bool storeCanHold = ship.EnergyStoreMax + 1e-6 >= ship.BubbleCreationCost;
            if (!storeCanHold || ship.EnergyOutputMax <= 0)
            {
                feasible = false;
                reason = "not enough power to open a warp";
            }
            else
            {
                wait = (ship.BubbleCreationCost - ship.EnergyStored) / ship.EnergyOutputMax;
            }
        }

        var fuel = WarpPlantFuel(ship, price.demandKW, duration, ship.BubbleCreationCost + ship.BubbleCollapseCost);
        return new MoveCost(mode, feasible, reason, price.limitLine ?? "", fuel, energy, 0, duration, 0, wait, speed);
    }

    /// <summary>
    /// Plant fuel a warp burns (energy phase 6). The sustain runs <paramref name="demandKW"/> for
    /// <paramref name="duration_s"/>, served free sources first, then the reactor, then the cells
    /// (EnergyGenProcessor.BurnRates). <paramref name="storeKJ"/> (bubble creation and collapse)
    /// comes out of the store and is made back at full output, each source in proportion to its
    /// output. Empty when nothing burns.
    /// </summary>
    static IReadOnlyDictionary<string, double> WarpPlantFuel(MoveCostShip ship, double demandKW, double duration_s, double storeKJ)
    {
        double free = Math.Max(0, ship.FreeOutputMax);
        double reactor = ship.ReactorFuelPerKJ > 0 ? Math.Max(0, ship.ReactorOutputMax) : 0;
        double cells = ship.CellFuelPerKJ > 0 ? Math.Max(0, ship.CellOutputMax) : 0;
        double reactorKJ = 0, cellKJ = 0;

        double rest = Math.Max(0, demandKW - free);
        double fromReactor = Math.Min(rest, reactor);
        double fromCells = Math.Min(rest - fromReactor, cells);
        if (duration_s > 0 && double.IsFinite(duration_s))
        {
            reactorKJ += fromReactor * duration_s;
            cellKJ += fromCells * duration_s;
        }
        double all = free + Math.Max(0, ship.ReactorOutputMax) + Math.Max(0, ship.CellOutputMax);
        if (storeKJ > 0 && all > 0)
        {
            reactorKJ += storeKJ * reactor / all;
            cellKJ += storeKJ * cells / all;
        }

        var fuel = new Dictionary<string, double>();
        void Add(string type, double kg)
        {
            if (string.IsNullOrEmpty(type) || !(kg > 0) || !double.IsFinite(kg))
                return;
            fuel[type] = fuel.TryGetValue(type, out double had) ? had + kg : kg;
        }
        Add(ship.ReactorFuelType, reactorKJ * ship.ReactorFuelPerKJ);
        Add(ship.CellFuelType, cellKJ * ship.CellFuelPerKJ);
        return fuel.Count == 0 ? NoFuel : fuel;
    }

    /// <summary>
    /// <see cref="Warp(MoveCostShip, double, double, double)"/> for a live ship as it is now.
    /// </summary>
    public static MoveCost Warp(Entity ship, double distance_m, double gravity_ms2 = 0, double radial = 0)
        => Warp(MoveCostShip.FromEntity(ship), distance_m, gravity_ms2, radial);

    /// <summary>One reaction burn of <paramref name="deltaV"/> m/s, as NewtonThrustAction plans it.</summary>
    public static MoveCost Burn(MoveCostShip ship, double deltaV)
    {
        const MoveCostMode mode = MoveCostMode.Burn;
        if (!HasReactionDrive(ship))
            return No(mode, "no reaction drive");

        double fuel = FuelFor(ship.MassTotal_kg, deltaV, ship.ExhaustVelocity);
        return Spend(mode, ship, deltaV, fuel, 0);
    }

    /// <summary>
    /// A two-burn Hohmann between circular radii around one body, as
    /// ChangeOrbitalAltitudeAction plans it with <see cref="OrbitalMath.Hohmann2"/>.
    /// Duration is the coast from the first burn to the second.
    /// </summary>
    public static MoveCost Hohmann(MoveCostShip ship, double parentMass_kg, double fromRadius_m, double toRadius_m)
    {
        const MoveCostMode mode = MoveCostMode.Hohmann;
        if (!HasReactionDrive(ship))
            return No(mode, "no reaction drive");
        if (!(fromRadius_m >= 1) || !(toRadius_m >= 1) || !double.IsFinite(fromRadius_m) || !double.IsFinite(toRadius_m))
            return No(mode, "no usable radius");

        double mass = ship.MassTotal_kg;
        // OrbitMath.SGP(parent, ship) adds the ship's mass to the parent's.
        double sgp = GeneralMath.StandardGravitationalParameter(parentMass_kg, mass);
        var manuvers = OrbitalMath.Hohmann2(sgp, fromRadius_m, toRadius_m);
        double dv1 = manuvers[0].deltaV.Length();
        double dv2 = manuvers[1].deltaV.Length();
        if (!double.IsFinite(dv1) || !double.IsFinite(dv2) || !double.IsFinite(manuvers[1].timeInSeconds))
            return No(mode, "Hohmann solution did not converge");

        // MovePlanner.PlanFromWindow: the second burn starts lighter by the first burn's fuel.
        double fuel1 = FuelFor(mass, dv1, ship.ExhaustVelocity);
        double fuel2 = FuelFor(mass - fuel1, dv2, ship.ExhaustVelocity);
        return Spend(mode, ship, dv1 + dv2, fuel1 + fuel2, manuvers[1].timeInSeconds);
    }

    /// <summary>
    /// <see cref="Hohmann(MoveCostShip, double, double, double)"/> for a live ship from its
    /// current orbit to a circular <paramref name="toRadius_m"/> around the same parent.
    /// Refuses what <see cref="ChangeOrbitalAltitudeAction"/> refuses (same check), including a
    /// ship already at that altitude.
    /// </summary>
    public static MoveCost Hohmann(Entity ship, double toRadius_m)
    {
        const MoveCostMode mode = MoveCostMode.Hohmann;
        if (!ChangeOrbitalAltitudeAction.TryCheckTransfer(ship, toRadius_m, out var orbit, out bool alreadyThere, out string reason))
            return No(mode, reason);
        if (alreadyThere)
            return No(mode, "already at altitude");
        if (!orbit.Parent.TryGetDataBlob<MassVolumeDB>(out var parentMass))
            return No(mode, "no SOI parent");
        return Hohmann(MoveCostShip.FromEntity(ship), parentMass.MassTotal, orbit.SemiMajorAxis, toRadius_m);
    }

    static bool HasReactionDrive(MoveCostShip ship)
        // MovePlanner.EvaluateNewtonian refuses ThrustInNewtons <= 0.
        => ship.ThrustInNewtons > 0 && ship.ExhaustVelocity > 0;

    static double FuelFor(double mass, double deltaV, double exhaustVelocity)
    {
        if (!(mass > 0) || !(deltaV > 0))
            return 0;
        // OrbitMath.TsiolkovskyFuelUse, as NewtonThrustAction.CreateCommand and MovePlanner.FuelKg use it.
        return OrbitMath.TsiolkovskyFuelUse(mass, exhaustVelocity, deltaV);
    }

    static MoveCost Spend(MoveCostMode mode, MoveCostShip ship, double deltaV, double fuel, double coast)
    {
        // MovePlanner.BurnSeconds: fuel over the burn rate.
        double burnSeconds = ship.FuelBurnRate > 0 ? fuel / ship.FuelBurnRate : 0;
        var used = new Dictionary<string, double> { [ship.FuelType ?? ""] = fuel };
        if (!double.IsFinite(fuel) || fuel < 0)
            return new MoveCost(mode, false, "burn solution did not converge", "", used, 0, deltaV, coast, burnSeconds, 0, 0);

        // NewtonThrustAbilityDB.SetFuel: the Δv the tank holds at this mass.
        double mass = ship.MassTotal_kg;
        double have = mass > 0
            ? OrbitMath.TsiolkovskyRocketEquation(mass, mass - ship.FuelMass_kg, ship.ExhaustVelocity)
            : 0;
        bool feasible = fuel <= ship.FuelMass_kg;
        // Same wording as MovePlanner.EvaluateNewtonian.
        string reason = feasible ? "" : $"needs {deltaV:N0} m/s Δv, have {have:N0} m/s";
        return new MoveCost(mode, feasible, reason, "", used, 0, deltaV, coast, burnSeconds, 0, 0);
    }

    static MoveCost No(MoveCostMode mode, string reason)
        => new MoveCost(mode, false, reason, "", NoFuel, 0, 0, 0, 0, 0, 0);
}
