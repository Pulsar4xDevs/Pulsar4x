using System;
using System.Collections.Generic;
using Pulsar4X.Colonies;
using Pulsar4X.Engine;
using Pulsar4X.Storage;

namespace Pulsar4X.Logistics;

/// <summary>
/// Multi-faction phase 2: two colonies on the same <see cref="ColonyInfoDB.PlanetEntity"/> trade and move goods
/// instantly and for free. Same body only, not the same well: Earth and Luna still haul.
/// </summary>
public static class SameBody
{
    /// <summary>True when both are distinct colonies on the same body.</summary>
    public static bool Is(Entity a, Entity b)
    {
        if (a == null || b == null || a.Id == b.Id)
            return false;
        if (!a.TryGetDataBlob<ColonyInfoDB>(out var ai) || !b.TryGetDataBlob<ColonyInfoDB>(out var bi))
            return false;
        return ai.PlanetEntity.IsValid && bi.PlanetEntity.IsValid && ai.PlanetEntity.Id == bi.PlanetEntity.Id;
    }

    /// <summary>
    /// Moves warehouse goods from <paramref name="source"/> to <paramref name="dest"/> now. Each line is clamped to
    /// the source's stock and the destination's free space. Returns the units moved per line (0 when skipped).
    /// No money changes hands here.
    /// </summary>
    internal static List<long> Move(Entity source, Entity dest, IReadOnlyList<(ICargoable item, long units)> lines)
    {
        var moved = new List<long>(lines.Count);
        if (!source.TryGetDataBlob<CargoStorageDB>(out var from) || !dest.TryGetDataBlob<CargoStorageDB>(out var to))
        {
            foreach (var _ in lines)
                moved.Add(0);
            return moved;
        }
        foreach (var (item, units) in lines)
        {
            long take = Math.Min(units, from.GetUnitsStored(item, false));
            if (take > 0 && !string.IsNullOrEmpty(item.CargoTypeID) && to.TypeStores.ContainsKey(item.CargoTypeID))
                take = Math.Min(take, to.GetFreeUnitSpace(item));
            else
                take = 0;
            if (take > 0)
            {
                take = from.RemoveCargoByUnit(item, take);
                long added = to.AddCargoByUnit(item, take);
                if (added < take)
                    from.AddCargoByUnit(item, take - added);   // put back what did not fit
                take = added;
            }
            moved.Add(Math.Max(0, take));
        }
        return moved;
    }
}
