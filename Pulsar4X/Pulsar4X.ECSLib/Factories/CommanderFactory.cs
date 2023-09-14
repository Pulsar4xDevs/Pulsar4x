using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public static class CommanderFactory
    {
        public static Entity Create(EntityManager manager, Guid factionID, CommanderDB commanderDB)
        {
            var blobs = new List<BaseDataBlob>();
            var nameDB = new NameDB(commanderDB.ToString(), factionID, commanderDB.ToString());
            blobs.Add(commanderDB);
            blobs.Add(nameDB);
            var entity = Entity.Create(manager, factionID, blobs);

            var faction = manager.GetGlobalEntityByGuid(factionID);
            if(faction.TryGetDatablob<FactionInfoDB>(out var factionInfoDB))
            {
                factionInfoDB.Commanders.Add(entity.Guid);
            }

            return entity;
        }

        public static CommanderDB CreateAcademyGraduate()
        {
            var commander = new CommanderDB()
            {
                Name = NameFactory.GetCommanderName(),
                Rank = 1,
                Type = CommanderTypes.Navy
            };

            return commander;
        }

        public static CommanderDB CreateShipCaptain()
        {
            var commander = new CommanderDB()
            {
                Name = NameFactory.GetCommanderName(),
                Rank = 6,
                Type = CommanderTypes.Navy
            };

            return commander;
        }

        public static Scientist CreateScientist(Entity faction, Entity location)
        {
            //all this stuff needs a proper bit of code to get names from a file or something

            //this is going to have to be thought out properly.
            Dictionary<ResearchCategories, float> bonuses = new Dictionary<ResearchCategories, float>();
            bonuses.Add(ResearchCategories.PowerAndPropulsion, 1.1f);

            Scientist sci = new Scientist();
            sci.Name = "Augusta King";
            sci.Age = 30;
            sci.MaxLabs = 25;
            sci.Bonuses = bonuses;

            var factionTech = faction.GetDataBlob<FactionTechDB>();
            factionTech.AllScientists.Add((sci, location));

            return sci;
        }
    }
}
