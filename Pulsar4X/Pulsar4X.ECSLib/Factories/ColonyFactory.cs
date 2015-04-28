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
        public static Entity CreateColony(Entity factionEntity, Entity planetEntity)
        {
            List<BaseDataBlob> blobs = new List<BaseDataBlob>();

            NameDB name = new NameDB(factionEntity, "somestring");
            ColonyInfoDB colonyInfoDB = new ColonyInfoDB(Entity.GetInvalidEntity(), 0, planetEntity);
            blobs.Add(colonyInfoDB);
            InstalationsDB colonyInstalationsDB = new InstalationsDB();
            blobs.Add(colonyInstalationsDB);

            Entity colonyEntity = Entity.Create(planetEntity.Manager, blobs);
            factionEntity.GetDataBlob<FactionDB>().Colonies.Add(colonyEntity);
            return colonyEntity;
        }
    }
}