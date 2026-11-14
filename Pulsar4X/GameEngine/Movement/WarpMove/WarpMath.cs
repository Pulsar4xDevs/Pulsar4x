using System;
using System.Collections.Generic;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Energy;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Galaxy;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;
using Stringify = Pulsar4X.Api.Stringify;

namespace Pulsar4X.Movement;

public static class WarpMath
{
    /// <summary>
    /// recalculates a shipsMaxSpeed.
    /// </summary>
    /// <param name="ship"></param>
    public static void CalcMaxWarpAndEnergyUsage(Entity ship)
    {
        Dictionary<string, double> totalFuelUsage = new Dictionary<string, double>();
        var instancesDB = ship.GetDataBlob<ComponentInstancesDB>();
        int totalEnginePower = instancesDB.GetTotalEnginePower(out totalFuelUsage);

        //Note: TN aurora uses the TCS for max speed calcs.
        WarpAbilityDB warpDB = ship.GetDataBlob<WarpAbilityDB>();
        warpDB.TotalWarpPower = totalEnginePower;
        //propulsionDB.FuelUsePerKM = totalFuelUsage;
        // MaxSpeed is derived from TotalWarpPower and the dry mass; nothing to store.
    }

    /// <summary>
    /// Calculates max ship speed based on engine power and ship mass
    /// </summary>
    /// <param name="power">TotalEnginePower</param>
    /// <param name="tonage">HullSize</param>
    /// <returns>Max speed in km/s</returns>
    public static int MaxSpeedCalc(double power, double tonage)
    {
        // From Aurora4x wiki:  Speed = (Total Engine Power / Total Class Size in HS) * 1000 km/s
        return (int)((power / tonage) * 1000);
    }

    struct Orbit
    {
        public Vector3 position;
        public double T;
    }

    public static (Vector3 position, DateTime etiDateTime) GetInterceptPosition(Entity mover, Entity target, DateTime atDateTime, Vector3 offsetPosition = new Vector3())
    {
        var moverPos = (Vector3)MoveMath.GetAbsoluteFuturePosition(mover, atDateTime);
        double spd_m = mover.GetDataBlob<WarpAbilityDB>().MaxSpeed;
        return GetInterceptPosition(moverPos, spd_m, target, atDateTime, offsetPosition);
    }

    /// <summary>
    /// Intercept from an explicit inertial start. Use this when the mover's orbit
    /// blob is already gone (WarpMovingDB.OnSetToEntity) so the search is not
    /// computed as if the ship sat at the system origin.
    /// </summary>
    public static (Vector3 position, DateTime etiDateTime) GetInterceptPosition(Vector3 moverAbsolutePos, double speed, Entity target, DateTime atDateTime, Vector3 offsetPosition = new Vector3())
    {
        var tgtPos = (Vector3)MoveMath.GetAbsoluteFuturePosition(target, atDateTime);
        var exitPos = tgtPos + offsetPosition;

        var tgtMoveType = target.GetDataBlob<PositionDB>().MoveType;
        switch (tgtMoveType)
        {
            case PositionDB.MoveTypes.None:
            {
                var distance = (exitPos - moverAbsolutePos).Length();
                return (exitPos, atDateTime + TimeSpan.FromSeconds(distance / speed));
            }
            case PositionDB.MoveTypes.Orbit:
            {
                return GetInterceptPosition_m(moverAbsolutePos, speed, target.GetDataBlob<OrbitDB>(), atDateTime, offsetPosition);
            }
            //For the following cases, we need to know if the target is an object which is owned by the same empire and we know what it's doing,
            //or if that info is unknown and how do we try predict?
            case PositionDB.MoveTypes.NewtonSimple:
                throw new NotImplementedException("not implemented");
            case PositionDB.MoveTypes.NewtonComplex:
                throw new NotImplementedException("not implemented");
            case PositionDB.MoveTypes.Warp:
                throw new NotImplementedException("not implemented");
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <summary>
    /// Calculates a cartisian position for an intercept for a ship and an target's orbit using warp.
    /// </summary>
    /// <returns>The intercept position and DateTime</returns>
    /// <param name="mover">The entity that is trying to intercept a target.</param>
    /// <param name="targetOrbit">Target orbit.</param>
    /// <param name="atDateTime">Datetime of transit start</param>
    public static (Vector3 position, DateTime etiDateTime) GetInterceptPosition(Entity mover, OrbitDB targetOrbit, DateTime atDateTime, Vector3 offsetPosition = new Vector3())
    {
        var moverPos = (Vector3)MoveMath.GetAbsoluteFuturePosition(mover, atDateTime);
        double spd_m = mover.GetDataBlob<WarpAbilityDB>().MaxSpeed;
        return WarpMath.GetInterceptPosition_m(moverPos, spd_m, targetOrbit, atDateTime, offsetPosition);
    }
    /// <summary>
    /// Calculates a cartisian position for an intercept for a ship and an target's orbit using warp.
    /// </summary>
    /// <param name="moverAbsolutePos"></param>
    /// <param name="speed"></param>
    /// <param name="targetOrbit"></param>
    /// <param name="atDateTime"></param>
    /// <param name="offsetPosition">position relative to the target object we wish to stop warp.</param>
    /// <returns></returns>
    public static (Vector3 position, DateTime etiDateTime) GetInterceptPosition_m(Vector3 moverAbsolutePos, double speed, OrbitDB targetOrbit, DateTime atDateTime, Vector3 offsetPosition = new Vector3())
    {

        var pos = moverAbsolutePos;
        double tim = 0;

        var pl = new Orbit()
        {
            position = moverAbsolutePos,
            T = GetSearchPeriod(targetOrbit)
        };

        double a = targetOrbit.SemiMajorAxis * 2;

        Vector3 p;
        int i;
        double tt, t, dt, a0, a1, T;
        // find orbital position with min error (coarse)
        a1 = -1.0;
        dt = 0.01 * pl.T;


        for (t=0; t< pl.T; t+=dt)
        {
            p = OrbitMath.GetAbsolutePosition(targetOrbit, atDateTime + TimeSpan.FromSeconds(t));  //pl.position(sim_t + t);                     // try time t
            p += offsetPosition;
            tt = (p - pos).Length() / speed;  //length(p - pos) / speed;
            a0 = tt - t; if (a0 < 0.0) continue;              // ignore overshoots
            a0 /= pl.T;                                   // remove full periods from the difference
            a0 -= Math.Floor(a0);
            a0 *= pl.T;
            if ((a0 < a1) || (a1 < 0.0))
            {
                a1 = a0;
                tim = t;
            }   // remember best option
        }
        // find orbital position with min error (fine)
        for (i = 0; i < 10; i++)                               // recursive increase of accuracy
            for (a1 = -1.0, t = tim - dt, T = tim + dt, dt *= 0.1; t < T; t += dt)
            {
                p = OrbitMath.GetAbsolutePosition(targetOrbit, atDateTime + TimeSpan.FromSeconds(t));  //p = pl.position(sim_t + t);                     // try time t
                p += offsetPosition;
                tt = (p - pos).Length() / speed;  //tt = length(p - pos) / speed;
                a0 = tt - t; if (a0 < 0.0) continue;              // ignore overshoots
                a0 /= pl.T;                                   // remove full periods from the difference
                a0 -= Math.Floor(a0);
                a0 *= pl.T;
                if ((a0 < a1) || (a1 < 0.0))
                {
                    a1 = a0;
                tim = tt;
                }   // remember best option
            }
        // direction
        p = OrbitMath.GetAbsolutePosition(targetOrbit, atDateTime + TimeSpan.FromSeconds(tim));//pl.position(sim_t + tim);
        p += offsetPosition;
        //dir = normalize(p - pos);
        return (p, atDateTime + TimeSpan.FromSeconds(tim));
    }
    static double GetSearchPeriod(OrbitDB orbit)
    {
        double period = orbit.OrbitalPeriod.TotalSeconds;
        var current = orbit;
        while (current.ParentDB is OrbitDB parent)
        {
            period = Math.Max(period, parent.OrbitalPeriod.TotalSeconds);
            current = parent;
        }
        // Optional safety floor so we never search a ridiculously small window
        return Math.Max(period, 3600.0);
    }

    /// <summary>
    /// Slowest warp worth opening a bubble for, m/s. A heading the plant can only hold below this
    /// does not start. Tunable.
    /// </summary>
    public const double MinWarpSpeed_ms = 1000;

    /// <summary>
    /// Why a priced bubble must not open, or "" when it may. The plant has to hold the bubble's
    /// demand (sustain, or the uphill climb at the slowed speed), and the speed it holds has to
    /// reach <see cref="MinWarpSpeed_ms"/>. One gate for WarpMoveAction, WarpMoveProcessor.TryStartWarp,
    /// MovePlanner.EvaluateWarp (all through <see cref="PriceCruise"/>) and MoveCostCalculator.Warp.
    /// </summary>
    /// <param name="outputKW">Plant output left for the warp: total output less life support.</param>
    /// <param name="lifeSupportKW">Life support already taken off <paramref name="outputKW"/> (crew plan phase 4), for the text.</param>
    public static string WarpRefusal(double speed, double demandKW, double outputKW, double lifeSupportKW = 0)
    {
        if (demandKW > outputKW)
        {
            if (lifeSupportKW > 0)
                return "Plant output " + Stringify.Power(outputKW + lifeSupportKW) + " less " + Stringify.Power(lifeSupportKW)
                    + " of life support leaves " + Stringify.Power(outputKW) + ", which cannot hold the warp bubble's " + Stringify.Power(demandKW) + ".";
            return "Plant output " + Stringify.Power(outputKW) + " cannot hold the warp bubble's " + Stringify.Power(demandKW) + ".";
        }
        if (!(speed >= MinWarpSpeed_ms))
            return "Warp speed " + Stringify.Velocity(speed) + " is below the " + Stringify.Velocity(MinWarpSpeed_ms) + " minimum.";
        return "";
    }

    /// <summary>
    /// Design-time warp checks for the ship designer. Totals come from the components the same
    /// way installation builds the live ship:
    /// sustain and creation add up over every drive (WarpDriveAtb.OnComponentInstallation);
    /// plant output is generator output only (EnergyGenAbilityDB.MaxOutputFromReactor; solar is
    /// left out because it drops away from a star);
    /// the store is EnergyStoreAtb.MaxStore for the drive's energy type plus one second of each
    /// generator's output (EnergyGenerationAtb.OnComponentInstallation adds that), i.e. the live
    /// EnergyStoreMax[WarpAbilityDB.EnergyType].
    /// The sustain line is the <see cref="WarpRefusal"/> demand test; the creation line is
    /// WarpMoveAction's "store can never hold it" test.
    /// Life support is crew capacity × <paramref name="kWPerCrew"/> × <paramref name="lifeSupportFactor"/>
    /// (CrewTech.LifeSupportFactor: closed-loop life-support research lowers it; 1 = no research).
    /// The designer's fuel check (<see cref="DesignFuelWarnings"/>) comes first, so a Newtonian engine with
    /// no tank for its fuel warns even on a ship with no warp drive. Empty when nothing is wrong.
    /// </summary>
    /// <param name="cargoGoods">The faction's cargo goods, to tell which holds can store an engine's fuel. Null: any cargo hold counts.</param>
    public static List<string> DesignWarpWarnings(IEnumerable<(ComponentDesign design, int count)> components,
        double lifeSupportFactor = 1, double kWPerCrew = GameSettings.DefaultShipLifeSupport_kWPerCrew,
        Pulsar4X.Storage.CargoDefinitionsLibrary? cargoGoods = null)
    {
        var warnings = DesignFuelWarnings(components, cargoGoods);
        double sustain = 0;
        double creation = 0;
        double output = 0;
        string energyType = null;
        bool hasDrive = false;
        foreach (var (design, count) in components)
        {
            if (design.TryGetAttribute<WarpDriveAtb>(out var drive))
            {
                hasDrive = true;
                energyType ??= drive.EnergyType;
                sustain += drive.BubbleSustainCost * count;
                creation += drive.BubbleCreationCost * count;
            }
            if (design.TryGetAttribute<EnergyGenerationAtb>(out var plant))
                output += plant.PowerOutputMax * count;
        }

        if (!hasDrive)
            return warnings;

        double store = 0;
        foreach (var (design, count) in components)
        {
            if (design.TryGetAttribute<EnergyStoreAtb>(out var battery) && battery.EnergyTypeID == energyType)
                store += battery.MaxStore * count;
            if (design.TryGetAttribute<EnergyGenerationAtb>(out var plant) && plant.EnergyTypeID == energyType)
                store += plant.PowerOutputMax * count;
        }

        // Crew plan phase 4: life support is must-run, 1 kW per crew at tech 0 (min(required, berths) aboard),
        // lowered by the closed-loop life-support tech factor, as the live ship gets it (ShipCrew.KWPerCrew).
        if (!(lifeSupportFactor >= 0) || !double.IsFinite(lifeSupportFactor))
            lifeSupportFactor = 1;
        if (!(kWPerCrew >= 0) || !double.IsFinite(kWPerCrew))
            kWPerCrew = GameSettings.DefaultShipLifeSupport_kWPerCrew;
        double lifeSupport = Pulsar4X.Ships.ShipCrew.Split(components).Capacity * kWPerCrew * lifeSupportFactor;
        if (sustain + lifeSupport > output)
            warnings.Add(lifeSupport > 0
                ? "Can't warp: bubble sustain " + Stringify.Power(sustain) + " plus life support " + Stringify.Power(lifeSupport)
                    + " needs " + Stringify.Power(sustain + lifeSupport) + ", plant makes " + Stringify.Power(output) + "."
                : "Can't warp: bubble sustain needs " + Stringify.Power(sustain) + ", plant makes " + Stringify.Power(output) + ".");
        // WarpMoveAction.Execute: storeMax + 1e-6 >= creationCost or the warp fails.
        if (store + 1e-6 < creation)
            warnings.Add("Can't warp: bubble creation needs " + Stringify.Energy(creation) + ", batteries hold " + Stringify.Energy(store) + ".");
        return warnings;
    }

    /// <summary>
    /// Designer check: every fuel a fitted Newtonian engine (NewtonionThrustAtb) burns needs somewhere to go.
    /// With <paramref name="cargoGoods"/>, a hold counts only when its store type (CargoStorageAtb.StoreTypeID)
    /// is the fuel's cargo type, as the designer's fuel capacity and the live CargoStorageDB see it, so a general
    /// cargo hold does not count for a liquid fuel. Without it, any cargo hold with volume counts.
    /// One warning per fuel with nowhere to go; empty when there is no Newtonian engine or every fuel has a tank.
    /// </summary>
    public static List<string> DesignFuelWarnings(IEnumerable<(ComponentDesign design, int count)> components,
        Pulsar4X.Storage.CargoDefinitionsLibrary? cargoGoods = null)
    {
        var warnings = new List<string>();
        var engines = new Dictionary<string, List<string>>(); // fuel id -> engine names
        var stores = new Dictionary<string, double>(); // store type id -> volume
        double anyStore = 0;
        foreach (var (design, count) in components)
        {
            if (design == null || count <= 0)
                continue;
            if (design.TryGetAttribute<NewtonionThrustAtb>(out var engine) && !string.IsNullOrEmpty(engine.FuelType))
            {
                if (!engines.TryGetValue(engine.FuelType, out var names))
                    engines[engine.FuelType] = names = new List<string>();
                if (!names.Contains(design.Name))
                    names.Add(design.Name);
            }
            if (design.TryGetAttribute<Pulsar4X.Storage.CargoStorageAtb>(out var hold) && hold.MaxVolume > 0)
            {
                stores[hold.StoreTypeID ?? ""] = (stores.TryGetValue(hold.StoreTypeID ?? "", out var v) ? v : 0) + hold.MaxVolume * count;
                anyStore += hold.MaxVolume * count;
            }
        }
        foreach (var (fuelId, names) in engines)
        {
            var fuel = cargoGoods?.GetAny(fuelId);
            bool stored = cargoGoods == null
                ? anyStore > 0
                : fuel != null && stores.TryGetValue(fuel.CargoTypeID, out var volume) && volume > 0;
            if (stored)
                continue;
            string fuelName = fuel?.Name ?? fuelId;
            warnings.Add("No fuel storage: " + string.Join(", ", names) + " burns " + fuelName
                + ", but no fitted tank or hold can store it. Add a fuel tank.");
        }
        return warnings;
    }

    /// <summary>
    /// One speed for the bubble, priced from the heading at opening.
    /// Two heading checks, then stop. The speed that flies is the second price.
    /// <c>refusal</c> is <see cref="WarpRefusal"/> for that price; "" means the bubble may open.
    /// </summary>
    public static (double speed, double demandKW, string limitLine, Vector3 exit, DateTime exitTime, string refusal) PriceCruise(
        Entity ship, Entity target, DateTime at, Vector3 offset)
    {
        var warp = ship.GetDataBlob<WarpAbilityDB>();
        double drive = warp.MaxSpeed;
        double sustain = warp.BubbleSustainCost;
        // Crew plan phase 4: life support comes off the top; the bubble gets what is left.
        double lifeSupportKW = Pulsar4X.Ships.ShipCrew.LifeSupport(ship);
        double outputKW = ship.TryGetDataBlob<EnergyGenAbilityDB>(out var power) ? Math.Max(0, power.TotalOutputMax - lifeSupportKW) : 0;
        var shipPos = ship.GetDataBlob<PositionDB>().AbsolutePosition;

        if (!(drive > 0) || !double.IsFinite(drive))
            return (drive, sustain, "", shipPos, at, WarpRefusal(drive, sustain, outputKW, lifeSupportKW));

        var first = GetInterceptPosition(shipPos, drive, target, at, offset);
        var firstPrice = PriceHeading(ship, shipPos, first.position, drive, sustain, outputKW);
        double priced = firstPrice.speed > 0 && double.IsFinite(firstPrice.speed) ? firstPrice.speed : drive;

        var second = GetInterceptPosition(shipPos, priced, target, at, offset);
        var secondPrice = PriceHeading(ship, shipPos, second.position, drive, sustain, outputKW);
        double cruise = secondPrice.speed > 0 && double.IsFinite(secondPrice.speed) ? secondPrice.speed : drive;

        var exit = second;
        if (Math.Abs(cruise - priced) > 1e-3)
            exit = GetInterceptPosition(shipPos, cruise, target, at, offset);

        return (cruise, secondPrice.demandKW, secondPrice.limitLine ?? "", exit.position, exit.etiDateTime,
            WarpRefusal(cruise, secondPrice.demandKW, outputKW, lifeSupportKW));
    }

    static (double speed, double demandKW, string limitLine) PriceHeading(
        Entity ship, Vector3 shipPos, Vector3 exitPos, double driveSpeed, double sustain, double outputKW)
    {
        var warp = ship.GetDataBlob<WarpAbilityDB>();
        double mass = ship.TryGetDataBlob<MassVolumeDB>(out var massDb) ? massDb.MassTotal : 0;
        var root = ship.GetDataBlob<PositionDB>().Root;
        if (root == null
            || root == Entity.InvalidEntity
            || !root.IsValid
            || root == ship
            || !root.TryGetDataBlob<MassVolumeDB>(out var starMass)
            || !root.TryGetDataBlob<PositionDB>(out var starPos)
            || !(mass > 0)
            || !(driveSpeed > 0))
            return (driveSpeed, sustain, "");

        Vector3 fromStar = shipPos - starPos.AbsolutePosition;
        double distance = fromStar.Length();
        double r = Math.Max(distance, Math.Max(starMass.RadiusInM, 1));
        double g = GeneralMath.StandardGravitationalParameter(starMass.MassDry) / (r * r);
        if (!double.IsFinite(g) || !(g > 0) || !(distance > 0))
            return (driveSpeed, sustain, "");

        Vector3 heading = exitPos - shipPos;
        double headingLen = heading.Length();
        if (!(headingLen > 0))
            return (driveSpeed, sustain, "");

        double radial = Vector3.Dot(heading / headingLen, fromStar / distance);
        return PriceInWell(warp.Coupling, warp.BleedFraction, outputKW, mass, g, radial, driveSpeed, sustain);
    }

    /// <summary>
    /// The speed and sustain demand for one heading in a star's well, from numbers only.
    /// <paramref name="radial"/> is the heading's cosine against the outward line from the star.
    /// <see cref="PriceCruise"/> calls this for a live ship; <see cref="MoveCostCalculator"/> calls it
    /// for a hypothetical one.
    /// </summary>
    public static (double speed, double demandKW, string limitLine) PriceInWell(
        double coupling, double bleed, double outputKW, double mass, double g, double radial,
        double driveSpeed, double sustain)
    {
        if (radial > 0 && coupling > 0)
            return PriceClimb(outputKW, coupling, mass, g, radial, driveSpeed, sustain);

        if (radial <= 0)
            return PriceFall(bleed, driveSpeed, sustain);

        return (driveSpeed, sustain, "");
    }

    static (double speed, double demandKW, string limitLine) PriceClimb(
        double outputKW, double coupling, double mass, double g, double radial, double driveSpeed, double sustain)
    {
        double plantSpeed = outputKW * 1000.0 / (coupling * mass * g * radial);
        if (!(plantSpeed > 0) || !double.IsFinite(plantSpeed))
            return (driveSpeed, sustain, "");

        double cruise = Math.Min(driveSpeed, plantSpeed);
        // Plant-limited outward speed is defined as the plant output. The product
        // form is a hair over that output, and EnergyGen then schedules an empty
        // time past TimeSpan.Max.
        double climbKW = plantSpeed <= driveSpeed
            ? outputKW
            : coupling * mass * g * (cruise * radial) / 1000.0;
        double demand = Math.Max(sustain, climbKW);
        string line = plantSpeed < driveSpeed
            ? "Drive " + Stringify.Velocity(driveSpeed) + ". Plant holds " + Stringify.Velocity(cruise) + " uphill. Warp at " + Stringify.Velocity(cruise) + "."
            : "Drive " + Stringify.Velocity(driveSpeed) + ". Uphill, plant can hold it. Warp at " + Stringify.Velocity(cruise) + ".";
        return (cruise, demand, line);
    }

    static (double speed, double demandKW, string limitLine) PriceFall(double bleed, double driveSpeed, double sustain)
    {
        if (!(bleed > 0))
            return (driveSpeed, sustain, "");

        double cap = driveSpeed * bleed;
        if (cap < driveSpeed)
            return (cap, sustain, "Downhill, bleed limited to " + Stringify.Velocity(cap) + ".");

        return (driveSpeed, sustain, "Downhill, at drive speed.");
    }

}