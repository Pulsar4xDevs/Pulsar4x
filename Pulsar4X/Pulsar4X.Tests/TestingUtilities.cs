using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Tests
{
    internal static class TestingUtilities
    {
        internal static Game CreateTestUniverse(int numSystems, DateTime testTime, bool generateDefaultHumans = false)
        {
            var game = new Game(new NewGameSettings { GameName = "Unit Test Game", StartDateTime = testTime, MaxSystems = numSystems, DefaultSolStart = generateDefaultHumans, CreatePlayerFaction = false });

            var smAuthToken = new AuthenticationToken(game.SpaceMaster);
            
            // Systems are currently generated in the Game Constructor.
            // Later, Systems will be initialized in the game constructor, but not actually generated until player discovery.
            //game.GenerateSystems(smAuthToken, numSystems);

            // add a faction:
            Entity humanFaction = FactionFactory.CreateFaction(game, "New Terran Utopian Empire");

            // add a species:
            Entity humanSpecies = SpeciesFactory.CreateSpeciesHuman(humanFaction, game.GlobalManager);

            // add another faction:
            Entity greyAlienFaction = FactionFactory.CreateFaction(game, "The Grey Empire");
            // Add another species:
            Entity greyAlienSpecies = SpeciesFactory.CreateSpeciesHuman(greyAlienFaction, game.GlobalManager);

            // Greys Name the Humans.
            humanSpecies.GetDataBlob<NameDB>().SetName(greyAlienFaction, "Stupid Terrans");
            // Humans name the Greys.
            greyAlienSpecies.GetDataBlob<NameDB>().SetName(humanFaction, "Space bugs");

            //TODO Expand the "Test Universe" to cover more datablobs and entities. And ships. Etc.

            if (generateDefaultHumans)
            {
                DefaultStartFactory.DefaultHumans(game, game.SpaceMaster, "Humans");
            }

            return game;
        }

        internal static Game CreateTestUniverse(int numSystems, bool generateDefaultHumans = false)
        {
            return CreateTestUniverse(numSystems, DateTime.Now, generateDefaultHumans);
        }
    }
}
