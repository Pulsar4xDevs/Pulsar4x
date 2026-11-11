using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Auth;
using Pulsar4X.Engine.Factories;
using Pulsar4X.Datablobs;
using Pulsar4X.Modding;
using Pulsar4X.Orbital;
using Pulsar4X.Components;
using Pulsar4X.Colonies;
using Pulsar4X.Factions;
using Pulsar4X.JumpPoints;
using Pulsar4X.Names;
using Pulsar4X.Orbits;
using Pulsar4X.People;
using Pulsar4X.Ships;
using Pulsar4X.Galaxy;
using Pulsar4X.Movement;

namespace Pulsar4X.Tests
{
    internal static class TestingUtilities
    {


        public static Entity BasicSol(EntityManager mgr)
        {
            double parentMass = 1.989e30;
            BaseDataBlob[] parentblobs = new BaseDataBlob[4];
            parentblobs[0] = new PositionDB() { AbsolutePosition = Vector3.Zero };
            parentblobs[1] = MassVolumeDB.NewFromMassAndRadius_m(parentMass, 696342000.0 );
            parentblobs[2] = new OrbitDB();
            parentblobs[3] = new NameDB();
            var ent = Entity.Create();
            mgr.AddEntity(ent, parentblobs);
            return ent;
        }

        public static Entity BasicEarth(EntityManager mgr)
        {
            double parentMass = 5.97237e24;
            BaseDataBlob[] parentblobs = new BaseDataBlob[4];
            parentblobs[0] = new PositionDB() { AbsolutePosition = Vector3.Zero };
            parentblobs[1] = new MassVolumeDB() { MassDry = parentMass };
            parentblobs[2] = new OrbitDB();
            parentblobs[3] = new NameDB();
            var ent = Entity.Create();
            mgr.AddEntity(ent, parentblobs);
            return ent;
        }

        internal static Game CreateTestUniverse(int numSystems, DateTime testTime)
        {
            var gamesettings = new NewGameSettings { StartDateTime = testTime, MaxSystems = numSystems, DefaultSolStart = false, CreatePlayerFaction = false };
            ModLoader modLoader = new ModLoader();
            ModDataStore modDataStore = new ModDataStore();
            modLoader.LoadModManifest("Data/basemod/modInfo.json", modDataStore);
            modLoader.LoadScenario("Data/scenarios/warp-tech-start/scenarioInfo.json", modDataStore);
            var game = new Game(gamesettings, modDataStore );

            var smAuthToken = new AuthenticationToken(game.SpaceMaster);

            // Systems are currently generated in the Game Constructor.
            // Later, Systems will be initialized in the game constructor, but not actually generated until player discovery.
            var starSystemFactory = new StarSystemFactory(game);
            for (int i = 0; i < numSystems; i++)
            {
                game.Systems.Add(starSystemFactory.CreateSystem(game, $"System {i + 1}", 0));
            }

            // add a faction:
            Entity humanFaction = FactionFactory.CreateFaction(game, "New Terran Utopian Empire");

            // add a species:
            Entity humanSpecies = SpeciesFactory.CreateSpeciesHuman(humanFaction, game.GlobalManager);

            // add another faction:
            Entity greyAlienFaction = FactionFactory.CreateFaction(game, "The Grey Empire");
            // Add another species:
            Entity greyAlienSpecies = SpeciesFactory.CreateSpeciesHuman(greyAlienFaction, game.GlobalManager);

            // Greys Name the Humans.
            humanSpecies.GetDataBlob<NameDB>().SetName(greyAlienFaction.Id, "Stupid Terrans");
            // Humans name the Greys.
            greyAlienSpecies.GetDataBlob<NameDB>().SetName(humanFaction.Id, "Space bugs");
            //TODO Expand the "Test Universe" to cover more datablobs and entities. And ships. Etc.

            // Set all test systems to Foreground so existing tests work without modification
            foreach (var system in game.Systems)
            {
                system.SetActivityState(SystemActivityState.Foreground);
            }

            return game;
        }

        internal static Game CreateTestUniverse(int numSystems)
        {
            return CreateTestUniverse(numSystems, DateTime.Now);
        }
    }

    public class TestGame
    {
        public NewGameSettings GameSettings { get; set; }

        public Game Game { get; set; }

        public Entity HumanFaction { get; set; }

        public Entity HumanSpecies { get; set; }

        public Entity GreyAlienFaction { get; set; }

        public Entity GreyAlienSpecies { get; set; }

        public ComponentDesign DefaultEngineDesign { get; set; }

        public ComponentDesign DefaultWeaponDesign { get; set; }

        public ShipDesign DefaultShipDesign { get; set; }

        public Entity EarthColony { get; set; }

        public Entity DefaultShip { get; set; }
        public StarSystem Sol { get; set; }
        public Entity Earth { get; set; }

        internal TestGame(int numSystems = 10)
        {

            GameSettings = new  NewGameSettings { MaxSystems = numSystems, CreatePlayerFaction = false };
            ModLoader modLoader = new ModLoader();
            ModDataStore modDataStore = new ModDataStore();
            modLoader.LoadModManifest("Data/basemod/modInfo.json", modDataStore);
            modLoader.LoadScenario("Data/scenarios/warp-tech-start/scenarioInfo.json", modDataStore);
            Game = new Game(GameSettings, modDataStore);

            // add a faction:
            HumanFaction = FactionFactory.CreateFaction(Game, "New Terran Utopian Empire");

            // add a species:
            HumanSpecies = SpeciesFactory.CreateSpeciesHuman(HumanFaction, Game.GlobalManager);

            // add another faction:
            GreyAlienFaction = FactionFactory.CreateFaction(Game, "The Grey Empire");
            // Add another species:
            GreyAlienSpecies = SpeciesFactory.CreateSpeciesHuman(GreyAlienFaction, Game.GlobalManager);

            // Greys Name the Humans.
            HumanSpecies.GetDataBlob<NameDB>().SetName(GreyAlienFaction.Id, "Stupid Terrans");
            // Humans name the Greys.
            GreyAlienSpecies.GetDataBlob<NameDB>().SetName(HumanFaction.Id, "Space bugs");


            StarSystemFactory starfac = new StarSystemFactory(Game);
            Sol = starfac.CreateSol(Game);
            Sol.SetActivityState(SystemActivityState.Foreground);
            Earth = NameLookup.GetFirstEntityWithName(Sol, "Earth"); //Sol.Entities[3]; //should be fourth entity created
             EarthColony = ColonyFactory.CreateColony(HumanFaction, HumanSpecies, Earth);
            var humondatastore = HumanFaction.GetDataBlob<FactionInfoDB>().Data;
            foreach (var id in humondatastore.LockedComponentTemplates.Keys.ToList())
                humondatastore.Unlock(id);
            foreach (var uniqueId in humondatastore.LockedCargoGoods.GetAll().Values.Select(c => c.UniqueID).ToList())
                humondatastore.Unlock(uniqueId);
            foreach (var id in humondatastore.LockedTechs.Keys.ToList())
                humondatastore.Unlock(id);
            foreach (var id in humondatastore.LockedArmor.Keys.ToList())
                humondatastore.Unlock(id);

            var dropship = Game.StartingGameData.ShipDesigns["default-ship-design-dropship"];
            var componentIds = new List<string>();
            foreach (var component in dropship.Components)
            {
                if (!componentIds.Contains(component.Id))
                    componentIds.Add(component.Id);
            }
            if (!componentIds.Contains("default-design-merlin"))
                componentIds.Add("default-design-merlin");

            ComponentDesigner.StartResearched = true;
            try
            {
                foreach (var id in componentIds)
                {
                    var component = Game.StartingGameData.ComponentDesigns[id];
                    ComponentDesignFromJson.Create(HumanFaction, humondatastore, component);
                }

                var designs = HumanFaction.GetDataBlob<FactionInfoDB>().ComponentDesigns;
                DefaultEngineDesign = designs["default-design-merlin"];
                DefaultWeaponDesign = designs["default-design-laser-weapon"];
                DefaultShipDesign = ShipDesignFromJson.Create(HumanFaction, humondatastore, dropship);
            }
            finally
            {
                ComponentDesigner.StartResearched = false;
            }

            Vector3 position = Earth.GetDataBlob<PositionDB>().AbsolutePosition;
            DefaultShip = ShipFactory.CreateShip(DefaultShipDesign, HumanFaction, position, Earth,  "Serial Peacemaker");
            Sol.SetDataBlob(DefaultShip.Id, new JumpPointDB());
        }


    }
}
