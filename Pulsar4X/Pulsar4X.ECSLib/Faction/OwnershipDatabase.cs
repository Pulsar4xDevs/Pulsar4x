using System;
using System.Collections.Generic;
namespace Pulsar4X.ECSLib
{
    internal class OwnershipDatabase
    {
        internal Entity Faction;

        internal Dictionary<EntityManager, SystemOwnershipDatabase> KnownSystems { get; } = new Dictionary<EntityManager, SystemOwnershipDatabase>();

        internal OwnershipDatabase(){}

        internal void Initialize(Entity factionEntity)
        { 
        }


    }


    internal class SystemOwnershipDatabase
    {
        internal Entity Faction;
        internal StarSystem StarSystem;
        internal EntityManager Manager;
        internal Dictionary<Guid, Entity> OwnedEntities { get; } = new Dictionary<Guid, Entity>();

        internal void Initialize(OwnershipDatabase database, StarSystem starSystem)
        {
            Faction = database.Faction;
            StarSystem = starSystem;
            Manager = starSystem.SystemManager;


            foreach (var ownedDB in Manager.GetAllDataBlobsOfType<OwnedDB>())
            {
                if (ownedDB.OwnedByFaction == Faction)
                    OwnedEntities.Add(ownedDB.OwningEntity.Guid, ownedDB.OwningEntity);
            }
        }

    }
}
