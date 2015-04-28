using System.Collections.Generic;
using Pulsar4X.ECSLib.DataBlobs;


namespace Pulsar4X.ECSLib.Factories
{
    public static class FactionFactory
    {
        public static Entity CreateFaction(EntityManager globalManager, string factionName)
        {

            List<BaseDataBlob> blobs = new List<BaseDataBlob>();
            NameDB name = new NameDB();
            FactionDB factionDB = new FactionDB();
            FactionAbilitiesDB factionAbilitiesDB = new FactionAbilitiesDB();
            TechDB techDB = new TechDB();
            blobs.Add(name);
            blobs.Add(factionDB);
            blobs.Add(factionAbilitiesDB);
            blobs.Add(techDB);
            Entity factionEntity = Entity.Create(globalManager, blobs);

            //factionEntity didn't exsist when we created the NameDB, so we have to recreate the name dictionary here.
            name.Name = new JDictionary<Entity, string>() { { factionEntity, factionName } };
            
            return factionEntity;
        }
    }
}