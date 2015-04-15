using System;
using System.Collections.Generic;
using Pulsar4X.ECSLib.DataBlobs;

namespace Pulsar4X.ECSLib.Factories
{
    public static class ColonyFactory
    {
        /// <summary>
        /// Creates a new colony with zero population.
        /// </summary>
        /// <param name="systemEntityManager"></param>
        /// <param name="factionEntity"></param>
        /// <returns></returns>
        public static Entity CreateColony(EntityManager systemEntityManager, Entity factionEntity, Entity planetEntity)
        {
            List<BaseDataBlob> blobs = new List<BaseDataBlob>();

            Entity colonyEntity = systemEntityManager.CreateEntity(blobs);

            NameDB name = new NameDB(factionEntity, "somestring");
            blobs.Add(name);
            ColonyInfoDB colonyInfoDB = new ColonyInfoDB(Entity.GetInvalidEntity(), 0, planetEntity);
            blobs.Add(colonyInfoDB);

            return colonyEntity;
        }
    }
}