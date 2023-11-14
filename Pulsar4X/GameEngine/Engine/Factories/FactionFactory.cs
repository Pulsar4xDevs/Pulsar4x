using System.Collections.Generic;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine.Auth;
using Pulsar4X.Events;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Engine
{

    public static class FactionFactory
    {
        /*
         *Stuff a faction needs to know:
         *name (nameDB)
         *password (AuthDB)
         *researched tech. (techDB)
         *
         *Owned Entites
         *
         *Sensor Contacts - these will be owned entites anyway.
         *  -System Bodies
         *  -Non Owned Entites
         *      -Colones
         *      -Ships
         *
         *Sensor Types
         *  - Grav, ie detecting anomalies in paths of known objects. - slow, but will find large dark planets.
         *  - Passive EM Spectrum:
         *      - Emited visable light (suns)
         *      - Reflected visable light (planets, moons)
         *      - Emitted IR (colonies, ship drives)
         *      - Reflected IR
         *      - Comms emmisions (colonies, ships)
         *  - Active EM:
         *      - Emmitting EM and looking for an echo. (radar)
         *
         * Owned Enties and Sensor Contacts need to be broken down by system.
         *
         *
         */


        public static Entity CreateFaction(Game game, string factionName)
        {
            var name = new NameDB(factionName);

            //var facinfo = new FactionInfoDB(new List<Entity>(), new List<Guid>(), );
            var factionInfo = new FactionInfoDB();
            factionInfo.Data = new FactionDataStore(game.StartingGameData);

            var factionTechDB = new FactionTechDB();

            var blobs = new List<BaseDataBlob> {
                name,
                factionInfo,
                new FactionAbilitiesDB(),
                factionTechDB,
                new FactionOwnerDB(),
                new FleetDB(),
                new OrderableDB(),
            };
            var factionEntity = Entity.Create();
            game.GlobalManager.AddEntity(factionEntity, blobs);

            factionInfo.EventLog = FactionEventLog.Create(factionEntity.Id);
            factionInfo.EventLog.Subscribe();

            // Need to unlock the starting data in the game
            foreach(var id in game.StartingGameData.DefaultItems["player-starting-items"].Items)
            {
                factionInfo.Data.Unlock(id);

                // Research any tech that is listed
                if(factionInfo.Data.Techs.ContainsKey(id))
                {
                    factionInfo.Data.IncrementTechLevel(id);
                }

                if(factionInfo.Data.CargoGoods.IsMaterial(id))
                {
                    factionInfo.IndustryDesigns[id] = (IConstructableDesign)factionInfo.Data.CargoGoods[id];
                }
            }

            // Add this faction to the SM's access list.
            game.SpaceMaster.SetAccess(factionEntity.Id, AccessRole.SM);
            name.SetName(factionEntity.Id, factionName);
            game.Factions.Add(factionEntity.Id, factionEntity);
            return factionEntity;
        }


        public static Entity CreatePlayerFaction(Game game, Player owningPlayer, string factionName)
        {
            Entity faction = CreateFaction(game, factionName);


            if (!Equals(owningPlayer, game.SpaceMaster))
            {
                owningPlayer.SetAccess(faction.Id, AccessRole.Owner);
            }

            return faction;
        }

        public static Entity CreateSpaceMasterFaction(Game game, Player owningPlayer, string factionName)
        {
            Entity faction = CreatePlayerFaction(game, owningPlayer, factionName);

            var factionInfo = faction.GetDataBlob<FactionInfoDB>();
            factionInfo.EventLog.Unsubscribe();
            factionInfo.EventLog = SpaceMasterEventLog.Create();
            factionInfo.EventLog.Subscribe();

            return faction;
        }


    }
}