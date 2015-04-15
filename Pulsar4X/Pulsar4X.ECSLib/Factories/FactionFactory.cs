using System;
using System.Collections.Generic;
using Pulsar4X.ECSLib.DataBlobs;


namespace Pulsar4X.ECSLib.Factories
{
    public static class FactionFactory
    {
        public static Entity CreateFaction(EntityManager systemEntityManager)
        {

            List<BaseDataBlob> blobs = new List<BaseDataBlob>();       
            Entity factionEntity = systemEntityManager.CreateEntity(blobs);

            NameDB name = new NameDB(factionEntity, "somestring");
            blobs.Add(name);

            return factionEntity;
        }
    }
}