using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class FactionSystemInfoProcessor
    {
        public FactionSystemInfoProcessor()
        {
        }

        void CreateFactionInfo(StarSystem starSys, Entity faction)
        {
            CreateFactionInfo(starSys, faction);



        }
        void CreateFactionInfo(EntityManager manager, Entity faction) 
        { 
            List<Entity> entitesWithOrbits = new List<Entity>(manager.GetAllEntitiesWithDataBlob<OrbitDB>());
            List<Entity> entitiesWithOwners = new List<Entity>(manager.GetEntitiesByFaction(faction.Guid));

            FactionSystemKnowledge factionKen = new FactionSystemKnowledge();
            factionKen.OwnedEntites.AddRange(entitiesWithOwners);

        }

        //sensors
        void UpdateFactionInfo(EntityManager manager, Entity faction)
        {
            
        }
    }

    public class FactionSystemKnowledge
    {
        internal Dictionary<Entity, DateTime> KnownEntites = new Dictionary<Entity, DateTime>();
        internal List<Entity> OwnedEntites = new List<Entity>();
    }

}
