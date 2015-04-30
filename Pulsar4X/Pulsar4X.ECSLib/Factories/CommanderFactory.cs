using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public static class CommanderFactory
    {
        public static Entity CreateScientist(EntityManager entityManager, Entity faction)
        {
            //all this stuff needs a proper bit of code to get names from a file or something.
            CommanderNameSD name;
            name.First = "firstname";
            name.Last = "lastname";
            name.IsFemale = true;

            CommanderTypes type = CommanderTypes.Civilian;

            //this is going to have to be thought out properly.
            Dictionary<ResearchCategories, float> bonuses = new Dictionary<ResearchCategories, float>();
            bonuses.Add(ResearchCategories.PowerAndPropulsion, 1.1f);
            int maxTeamSize = 25;

            //create the blob.
            CommanderDB scientist = new CommanderDB(name, 1, type);
            ScientistBonusDB bonus = new ScientistBonusDB(bonuses, maxTeamSize);
   
            List<BaseDataBlob> blobs = new List<BaseDataBlob>();
            blobs.Add(scientist);
            blobs.Add(bonus);

            Entity officer = Entity.Create(entityManager, blobs);

            return officer;    
        }
    }
}
