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
            CreateFactionInfo(starSys.SystemManager, faction);



        }
        void CreateFactionInfo(EntityManager manager, Entity faction) 
        { 
            List<Entity> entitesWithOrbits = new List<Entity>(manager.GetAllEntitiesWithDataBlob<OrbitDB>()); 
            List<Entity> entitiesWithOwners = new List<Entity>(manager.GetAllEntitiesWithDataBlob<OwnedDB>());

            FactionSystemKnowledge factionKen = new FactionSystemKnowledge();

            foreach (var item in entitiesWithOwners)
            {
                if (item.GetDataBlob<OwnedDB>().OwnedByFaction == faction)
                    factionKen.OwnedEntites.Add(item);
                
            }


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
