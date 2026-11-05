using System;
using System.Collections.Generic;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Events;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Names;
using Pulsar4X.Technology;

namespace Pulsar4X.People
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
            blobs.Add(new BonusesDB());
            var entity = Entity.Create();
            entity.FactionOwnerID = factionID;
            manager.AddEntity(entity, blobs);

            var faction = manager.Game.Factions[factionID];
            if(faction.TryGetDataBlob<FactionInfoDB>(out var factionInfoDB))
            {
                factionInfoDB.Commanders.Add(entity);
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

        public static CommanderDB CreateScientist(Game game)
        {
            var scientist = new CommanderDB()
            {
                Name = NameFactory.GetCommanderName(game),
                Rank = 1,
                Type = CommanderTypes.Scientist
            };

            return scientist;
        }

        public static CommanderDB CreateAdmin(Game game)
        {
            var db = new CommanderDB()
            {
                Name = NameFactory.GetCommanderName(game),
                Rank = 1,
                Type = CommanderTypes.Civilian
            };
            return db;
        }

        public const int ScientistMinAge = 25;
        public const int ScientistMaxAge = 60;
        public const int ScientistMinLabs = 15;
        public const int ScientistMaxLabs = 30;
        public const float ScientistCategoryBonus = 1.1f;

        /// <summary>
        /// A lab scientist with a random name, age, favoured category and lab limit, all from the game's seeded random.
        /// Added to the faction's AllScientists. The caller houses it on the colony.
        /// </summary>
        public static Scientist CreateScientist(Entity faction, Entity location)
        {
            var game = faction.Manager!.Game;
            string name = NameFactory.GetCommanderName(game);
            int age = game.RNG.Next(ScientistMinAge, ScientistMaxAge + 1);
            string category = RandomTechCategory(game);
            byte maxLabs = (byte)game.RNG.Next(ScientistMinLabs, ScientistMaxLabs + 1);
            return CreateScientist(faction, location, name, age, category, maxLabs);
        }

        public static Scientist CreateScientist(Entity faction, Entity location, string name, int age, string category, byte maxLabs)
        {
            var bonuses = new Dictionary<string, float>();
            if (!string.IsNullOrEmpty(category))
                bonuses.Add(category, ScientistCategoryBonus);

            Scientist sci = new Scientist();
            sci.Name = name;
            sci.Age = age;
            sci.MaxLabs = maxLabs;
            sci.Bonuses = bonuses;

            var factionTech = faction.GetDataBlob<FactionTechDB>();
            factionTech.AllScientists.Add((sci, location));

            return sci;
        }

        /// <summary>One tech category id, picked from the sorted ids so the same seed gives the same pick.</summary>
        public static string RandomTechCategory(Game game)
        {
            var ids = new List<string>(game.TechCategories.Keys);
            if (ids.Count == 0)
                return "";
            ids.Sort(StringComparer.Ordinal);
            return ids[game.RNG.Next(0, ids.Count)];
        }

        public static void DestroyCommander(Entity commanderToDestroy)
        {
            var game = commanderToDestroy.Manager.Game;
            var faction = game.Factions[commanderToDestroy.FactionOwnerID];

            if(faction.TryGetDataBlob<FactionInfoDB>(out var factionInfoDB))
            {
                factionInfoDB.Commanders.Remove(commanderToDestroy);
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
