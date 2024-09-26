using System;
using System.Collections.Generic;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Events;
using Pulsar4X.Extensions;

namespace Pulsar4X.Engine
{
    public static class CommanderFactory
    {
        public static Entity Create(EntityManager? manager, int factionID, CommanderDB commanderDB)
        {
            if(manager == null) throw new ArgumentNullException("manager cannot be null");

            var blobs = new List<BaseDataBlob>();
            var nameDB = new NameDB(commanderDB.ToString(), factionID, commanderDB.ToString());
            blobs.Add(nameDB);
            blobs.Add(commanderDB);
            var entity = Entity.Create();
            entity.FactionOwnerID = factionID;
            manager.AddEntity(entity, blobs);

            var faction = manager.Game.Factions[factionID];
            if(faction.TryGetDatablob<FactionInfoDB>(out var factionInfoDB))
            {
                factionInfoDB.Commanders.Add(entity.Id);
            }

            return entity;
        }

        public static CommanderDB CreateAcademyGraduate(Game game)
        {
            var commander = new CommanderDB()
            {
                Name = NameFactory.GetCommanderName(game),
                Rank = 1,
                Type = CommanderTypes.Navy
            };

            return commander;
        }

        public static CommanderDB CreateShipCaptain(Game game)
        {
            var commander = new CommanderDB()
            {
                Name = NameFactory.GetCommanderName(game),
                Rank = 6,
                Type = CommanderTypes.Navy
            };

            return commander;
        }

        public static Scientist CreateScientist(Entity faction, Entity location)
        {
            //all this stuff needs a proper bit of code to get names from a file or something

            //this is going to have to be thought out properly.
            Dictionary<string, float> bonuses = new Dictionary<string, float>();
            bonuses.Add("tech-category-power-propulsion", 1.1f);

            Scientist sci = new Scientist();
            sci.Name = "Augusta King";
            sci.Age = 30;
            sci.MaxLabs = 25;
            sci.Bonuses = bonuses;

            var factionTech = faction.GetDataBlob<FactionTechDB>();
            factionTech.AllScientists.Add((sci, location));

            return sci;
        }

        public static void DestroyCommander(Entity commanderToDestroy)
        {
            var game = commanderToDestroy.Manager.Game;
            var faction = game.Factions[commanderToDestroy.FactionOwnerID];

            if(faction.TryGetDatablob<FactionInfoDB>(out var factionInfoDB))
            {
                factionInfoDB.Commanders.Remove(commanderToDestroy.Id);
            }

            EventManager.Instance.Publish(
                Event.Create(
                    EventType.CrewLosses,
                    commanderToDestroy.Manager.StarSysDateTime,
                    $"{commanderToDestroy.GetOwnersName()} has been killed",
                    commanderToDestroy.FactionOwnerID,
                    commanderToDestroy.Manager.ManagerID,
                    commanderToDestroy.Id
                ));

            commanderToDestroy.Destroy();
        }
    }
}
