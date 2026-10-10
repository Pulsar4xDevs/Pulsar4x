using Pulsar4X.Colonies;
using Pulsar4X.Engine;
using Pulsar4X.Logistics;
using Pulsar4X.Names;

namespace Pulsar4X.Factions;

/// <summary>Ceres Depot, the office that lists Strata's charts.</summary>
static class CeresOffice
{
    public static Entity? Find(EntityManager? manager)
    {
        if (manager == null)
            return null;
        foreach (var colony in manager.GetAllEntitiesWithDataBlob<ColonyInfoDB>())
        {
            if (!colony.TryGetDataBlob<NameDB>(out var name))
                continue;
            if (name.DefaultName != CeresStart.DepotFactionName)
                continue;
            if (!colony.HasDataBlob<LogiBaseDB>())
                continue;
            return colony;
        }
        return null;
    }
}
