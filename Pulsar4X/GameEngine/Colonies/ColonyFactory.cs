using System.Collections.Generic;
using Pulsar4X.Orbital;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Engine
{
    public static class ColonyFactory
    {
        /// <summary>
        /// Creates a new colony with zero population unless specified.
        /// </summary>
        public static Entity CreateColony(Entity factionEntity, Entity speciesEntity, Entity planetEntity, long initialPopulation = 0)
        {
            var blobs = new List<BaseDataBlob>();

            string planetName = planetEntity.GetDataBlob<NameDB>().GetName(factionEntity.Id);
            NameDB name = new NameDB(planetName + " Colony"); // TODO: Review default name.
            name.SetName(factionEntity.Id, name.DefaultName);

            var pos = new Vector3(planetEntity.GetDataBlob<MassVolumeDB>().RadiusInM, 0, 0);

            blobs.Add(name);
            blobs.Add(new ColonyInfoDB(speciesEntity, initialPopulation, planetEntity));
            blobs.Add(new ColonyBonusesDB());
            blobs.Add(new MiningDB());
            blobs.Add(new OrderableDB());
            blobs.Add(new MassVolumeDB());
            blobs.Add(new VolumeStorageDB());
            blobs.Add(new PositionDB(pos));
            blobs.Add(new TeamsHousedDB());
            blobs.Add(new ComponentInstancesDB()); //installations get added to the componentInstancesDB

            Entity colonyEntity = Entity.Create();
            colonyEntity.FactionOwnerID = factionEntity.Id;
            planetEntity.Manager.AddEntity(colonyEntity, blobs);
            var factionInfo = factionEntity.GetDataBlob<FactionInfoDB>();
            factionInfo.Colonies.Add(colonyEntity);
            factionEntity.GetDataBlob<FactionOwnerDB>().SetOwned(colonyEntity);
            return colonyEntity;
        }
    }
}