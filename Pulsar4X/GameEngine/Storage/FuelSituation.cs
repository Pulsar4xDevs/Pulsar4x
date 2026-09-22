using System;
using System.Collections.Generic;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Fleets;
using Pulsar4X.Movement;
using Pulsar4X.Ships;

namespace Pulsar4X.Storage;

/// <summary>
/// Observe step for fuel: tank state + preferred fill source (fleet tanker or explicit target).
/// Shared by weighting (context scores) and <see cref="RefuelShipPlanner"/>.
/// </summary>
public readonly struct FuelSnapshot
{
    public readonly ICargoable? FuelType;
    public readonly double MassStored;
    public readonly double MassCapacity;
    public readonly float Fraction;       // 0..1, 1 if no engine/fuel type
    public readonly int? PreferredSourceId;
    public readonly string? SourceReason;

    public bool HasFuelSystem => FuelType != null;
    public bool IsCritical => HasFuelSystem && Fraction < 0.15f;
    public bool IsLow => HasFuelSystem && Fraction < 0.45f;

    public FuelSnapshot(ICargoable? fuelType, double stored, double capacity, int? sourceId, string? sourceReason)
    {
        FuelType = fuelType;
        MassStored = stored;
        MassCapacity = capacity;
        Fraction = capacity > 0 ? (float)Math.Clamp(stored / capacity, 0, 1) : 1f;
        PreferredSourceId = sourceId;
        SourceReason = sourceReason;
    }
}

public static class FuelSituation
{
    /// <summary>
    /// Fuel mass in tank vs capacity from the ship's newton fuel type and cargo store.
    /// </summary>
    public static bool TryGetFuelMass(Entity ship, out ICargoable fuel, out double stored, out double capacity)
    {
        fuel = null!;
        stored = 0;
        capacity = 0;

        if (!ship.TryGetDataBlob<NewtonThrustAbilityDB>(out var thrust))
            return false;
        if (!ship.TryGetDataBlob<CargoStorageDB>(out var store))
            return false;


        var cargoLib = ship.GetFactionCargoDefinitions();
        fuel = cargoLib.GetAny(thrust.FuelType);
        if (fuel == null)
            return false;

        stored = store.GetMassStored(fuel, includeEscro: false);
        // Capacity: free + stored in mass terms for this cargo type volume budget.
        capacity = stored + store.GetFreeMass(fuel);
        if (capacity <= 0)
            capacity = stored; // full or unknown free
        return true;
    }

    /// <summary>
    /// Prefer an explicit goal target, else a fleet-mate with spare fuel of our type, else none.
    /// </summary>
    public static FuelSnapshot Observe(Entity ship, int? explicitSourceId = null)
    {
        if (!TryGetFuelMass(ship, out var fuel, out var stored, out var capacity))
            return new FuelSnapshot(null, 0, 0, null, "no newton fuel system");

        if (explicitSourceId is int id && id > 0
            && ship.Manager.TryGetGlobalEntityById(id, out var explicitSource)
            && explicitSource.HasDataBlob<CargoStorageDB>())
        {
            return new FuelSnapshot(fuel, stored, capacity, id, "goal target");
        }

        if (TryFindFleetTanker(ship, fuel, out var tankerId, out var reason))
            return new FuelSnapshot(fuel, stored, capacity, tankerId, reason);

        return new FuelSnapshot(fuel, stored, capacity, null, "no fleet tanker with fuel");
    }

    /// <summary>
    /// Another ship in the same fleet that holds our fuel type and has more mass than a trickle.
    /// </summary>
    public static bool TryFindFleetTanker(Entity ship, ICargoable fuel, out int tankerId, out string reason)
    {
        tankerId = -1;
        reason = "not in a fleet";

        // Walk parents: ship may be a fleet child.
        if (!TryGetParentFleet(ship, out var fleet) || !fleet.TryGetDataBlob<FleetDB>(out var fleetDB))
            return false;

        Entity? best = null;
        double bestSpare = 0;

        foreach (var mate in fleetDB.Children)
        {
            if (mate.Id == ship.Id)
                continue;
            if (!mate.HasDataBlob<ShipInfoDB>())
                continue;
            if (!mate.TryGetDataBlob<CargoStorageDB>(out var mateStore))
                continue;

            double spare = mateStore.GetMassStored(fuel, includeEscro: false);
            if (spare <= bestSpare)
                continue;
            bestSpare = spare;
            best = mate;
        }

        if (best == null || bestSpare <= 0)
        {
            reason = "no fleet mate carrying our fuel";
            return false;
        }

        tankerId = best.Id;
        reason = "fleet tanker";
        return true;
    }

    static bool TryGetParentFleet(Entity ship, out Entity fleet)
    {
        fleet = null!;
        // FleetDB.Children holds ships; ships often know parent via PositionDB or formation.
        // Use FleetDB on parent if the manager indexes it; otherwise scan fleets of the same faction.
        foreach (var e in ship.Manager.GetAllEntitiesWithDataBlob<FleetDB>())
        {
            if (e.FactionOwnerID != ship.FactionOwnerID)
                continue;
            if (!e.TryGetDataBlob<FleetDB>(out var fdb))
                continue;
            foreach (var child in fdb.Children)
            {
                if (child.Id == ship.Id)
                {
                    fleet = e;
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Context multiplier for <see cref="GoalType.RefuelAt"/> from tank state (drive DontRunOutOfFuel).
    /// 1 = no urgency; rises sharply as fraction falls. Scaled by commander Caution if provided.
    /// </summary>
    public static float RefuelContextMultiplier(FuelSnapshot snap, float caution = 1f)
    {
        if (!snap.HasFuelSystem)
            return 0f; // cannot refuel what we don't burn

        // Comfortable above 50%: near-zero autonomous refuel pressure.
        if (snap.Fraction >= 0.5f)
            return 0.05f + (1f - snap.Fraction) * 0.2f;

        // Below half: climb toward high urgency; critical &lt;15% is maxed.
        float scarcity = (0.5f - snap.Fraction) / 0.5f; // 0 at 50%, 1 at empty
        float urgency = 0.5f + scarcity * 2.5f;          // ~0.5 .. 3.0
        if (snap.IsCritical)
            urgency = Math.Max(urgency, 3.5f);

        return urgency * Math.Max(0.25f, caution);
    }
}
