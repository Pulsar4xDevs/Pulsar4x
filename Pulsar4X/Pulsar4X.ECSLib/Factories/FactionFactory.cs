using System.Collections.Generic;
using Pulsar4X.ECSLib.DataBlobs;


namespace Pulsar4X.ECSLib.Factories
{
    public static class FactionFactory
    {
        public static Entity CreateFaction(EntityManager globalManager, string factionName)
        {

            List<BaseDataBlob> blobs = new List<BaseDataBlob>();
            NameDB name = new NameDB(Entity.GetInvalidEntity(), factionName);
            FactionDB factionDB = new FactionDB(new List<Entity>(), new List<StarSystem>(), new List<Entity>());
            blobs.Add(name);
            blobs.Add(factionDB);
            Entity factionEntity = Entity.Create(globalManager, blobs);

            //factionEntity didn't exsist when we created the NameDB, so we have to recreate the name dictionary here.
            name.Name = new JDictionary<Entity, string>() { { factionEntity, factionName } };
            
            return factionEntity;
        }
    }
}