using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Auth;
using Pulsar4X.Datablobs;
using Pulsar4X.Modding;
using Pulsar4X.Orbital;
using Pulsar4X.Components;
using Pulsar4X.Engine.Designs;

namespace Pulsar4X.Tests
{
    internal static class TestingUtilities
    {

        
        public static Entity BasicSol(EntityManager mgr)
        {
            double parentMass = 1.989e30;
            BaseDataBlob[] parentblobs = new BaseDataBlob[4];
            parentblobs[0] = new PositionDB(mgr.ManagerGuid) { AbsolutePosition = Vector3.Zero };
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
            parentblobs[0] = new PositionDB(mgr.ManagerGuid) { AbsolutePosition = Vector3.Zero };
            parentblobs[1] = new MassVolumeDB() { MassDry = parentMass };
            parentblobs[2] = new OrbitDB();
            parentblobs[3] = new NameDB();
            var ent = Entity.Create();
            mgr.AddEntity(ent, parentblobs);
            return ent;
        }

        internal static Game CreateTestUniverse(int numSystems, DateTime testTime, bool generateDefaultHumans = false)
        {
            var gamesettings = new NewGameSettings { GameName = "Unit Test Game", StartDateTime = testTime, MaxSystems = numSystems, DefaultSolStart = generateDefaultHumans, CreatePlayerFaction = false };
            ModLoader modLoader = new ModLoader();
            ModDataStore modDataStore = new ModDataStore();
            modLoader.LoadModManifest("Data/basemod/modInfo.json", modDataStore);
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

            if (generateDefaultHumans)
            {
                DefaultStartFactory.DefaultHumans(game, "Humans");
            }

            return game;
        }

        internal static Game CreateTestUniverse(int numSystems, bool generateDefaultHumans = false)
        {
            return CreateTestUniverse(numSystems, DateTime.Now, generateDefaultHumans);
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

            GameSettings = new  NewGameSettings { GameName = "Unit Test Game", MaxSystems = numSystems, CreatePlayerFaction = false };
            ModLoader modLoader = new ModLoader();
            ModDataStore modDataStore = new ModDataStore();
            modLoader.LoadModManifest("Data/basemod/modInfo.json", modDataStore);
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
            Earth = NameLookup.GetFirstEntityWithName(Sol, "Earth"); //Sol.Entities[3]; //should be fourth entity created 
             EarthColony = ColonyFactory.CreateColony(HumanFaction, HumanSpecies, Earth);
             var humondatastore = HumanFaction.GetDataBlob<FactionInfoDB>().Data;
            DefaultEngineDesign = DefaultStartFactory.DefaultThrusterDesign(Game, HumanFaction, humondatastore);
            DefaultWeaponDesign = DefaultStartFactory.DefaultSimpleLaser(Game, HumanFaction, humondatastore);
            DefaultShipDesign = DefaultStartFactory.DefaultShipDesign(Game, HumanFaction, humondatastore);

            Vector3 position = Earth.GetDataBlob<PositionDB>().AbsolutePosition;
            DefaultShip = ShipFactory.CreateShip(DefaultShipDesign, HumanFaction, position, Earth,  "Serial Peacemaker");
            Sol.SetDataBlob(DefaultShip.Id, new TransitableDB());
        }


    }

    /// <summary>
    /// Provides methods to check the "equivalence" of data blobs.
    /// Note: This is not a true equality checker, but a utility to check
    /// if two data blobs are effectively equivalent for testing purposes.
    /// </summary>
    public static class DataBlobEqualityChecker
    {
        /// <summary>
        /// Determines if two data blobs are effectively equivalent.
        /// </summary>
        /// <typeparam name="T">The type of the data blobs.</typeparam>
        /// <param name="first">The first data blob.</param>
        /// <param name="second">The second data blob.</param>
        /// <returns>true if the two data blobs are effectively equivalent; otherwise, false.</returns>
        public static bool AreEqual<T>(T first, T second) where T : BaseDataBlob
        {
            // Use a HashSet to track compared dataBlobs.
            var visitedPairs = new HashSet<(object?, object?)>();
            return AreEqualInternal(first, second, visitedPairs);
        }
        private static bool AreEqualInternal<T>(T first, T second, HashSet<(object?, object?)> visitedPairs) where T : BaseDataBlob
        {
            // Avoid infinite recursion
            if (visitedPairs.Contains((first, second)) || visitedPairs.Contains((second, first)))
                return true;
            visitedPairs.Add((first, second));

            // Get the actual implementation type, not a base type.
            Type firstType = first.GetType();
            Type secondType = second.GetType();

            if (firstType != secondType) return false;

            // Reflect equality checks across the properties.
            foreach (PropertyInfo property in firstType.GetProperties())
            {
                object? firstValue = property.GetValue(first);
                object? secondValue = property.GetValue(second);
                if (!CompareProperty(visitedPairs, firstValue, secondValue))
                    return false;
            }

            return true;
        }

        private static bool CompareProperty(HashSet<(object?, object?)> visitedPairs, object? firstValue, object? secondValue)
        {
            if (visitedPairs.Contains((firstValue, secondValue)) || visitedPairs.Contains((secondValue, firstValue)))
                return true;

            visitedPairs.Add((firstValue, secondValue));
            
            if (firstValue is null && secondValue is null)
                return true;

            if (firstValue is null || secondValue is null)
                return false;

            Type propertyType = firstValue.GetType();
            if (propertyType != secondValue.GetType())
                return false;

            // Special handling for Entities so we don't check references
            if (propertyType == typeof(Entity))
            {
                if (!EntityPropertiesMatch((Entity)firstValue, (Entity)secondValue, visitedPairs))
                {
                    return false;
                }
            }
            else if (firstValue.GetType().IsValueType || firstValue is string)
            {
                if (!firstValue.Equals(secondValue)) // For value types and strings, a direct equality check suffices.
                    return false;
            }
            else if (propertyType.IsSubclassOf(typeof(BaseDataBlob)))
            {
                if (!AreEqualInternal((BaseDataBlob)firstValue, (BaseDataBlob)secondValue, visitedPairs))
                    return false;
            }
            else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var firstList = (IList)firstValue;
                var secondList = (IList)secondValue;

                if (firstList.Count != secondList.Count) return false;

                for (int i = 0; i < firstList.Count; i++)
                {
                    object? firstListItem = firstList[i];
                    object? secondListItem = secondList[i];
                    if (!CompareProperty(visitedPairs, firstListItem, secondListItem))
                        return false;

                }
            }
            // If the property type is a generic dictionary, we need to handle it specially.
            else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                var firstDict = (IDictionary)firstValue;
                var secondDict = (IDictionary)secondValue;

                // If the dictionaries have different counts, they're not equivalent.
                if (firstDict.Count != secondDict.Count) return false;

                // For each key in the first dictionary...
                foreach (object? firstKey in firstDict.Keys)
                {
                    bool keyMatchFound = false;

                    // ...we loop through all keys in the second dictionary to find an equivalent.
                    foreach (object? secondKey in secondDict.Keys)
                    {
                        // Use CompareProperty to check if keys from both dictionaries are effectively equivalent.
                        if (!CompareProperty(visitedPairs, firstKey, secondKey))
                            continue;

                        keyMatchFound = true;

                        // If keys are equivalent, retrieve the values for both keys.
                        object? firstDictValue = firstDict[firstKey];
                        object? secondDictValue = secondDict[secondKey];

                        // Now compare the values for the matched keys. If values aren't equivalent, entire dictionaries aren't equivalent.
                        if (!CompareProperty(visitedPairs, firstDictValue, secondDictValue))
                            return false;

                        break; // Break out of inner loop as a matching key was found.
                    }

                    // If no equivalent key was found in the second dictionary for a key in the first dictionary, they're not equivalent.
                    if (!keyMatchFound) return false;
                }
            }
            else if (propertyType.IsClass)
                return ReflectAndCompareClass(visitedPairs, firstValue, secondValue);
            else if (!firstValue.Equals(secondValue))
                return false;

            return true;
        }

        private static bool ReflectAndCompareClass(HashSet<(object?, object?)> visitedPairs, object firstObject, object secondObject)
        {
            Type objectType = firstObject.GetType();
            
            // Compare fields.
            foreach (FieldInfo field in objectType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                object? firstFieldValue = field.GetValue(firstObject);
                object? secondFieldValue = field.GetValue(secondObject);
                if (!CompareProperty(visitedPairs, firstFieldValue, secondFieldValue))
                    return false;
            }

            // Compare properties.
            foreach (PropertyInfo property in objectType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                object? firstPropertyValue = property.GetValue(firstObject);
                object? secondPropertyValue = property.GetValue(secondObject);
                if (!CompareProperty(visitedPairs, firstPropertyValue, secondPropertyValue))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if the properties of two entities make them effectively equivalent.
        /// </summary>
        /// <param name="entityA">The first entity.</param>
        /// <param name="entityB">The second entity.</param>
        /// /// <param name="visitedPairs">A collection used to track already compared pairs of data blobs, preventing infinite recursion when circular references are present.</param>
        /// <returns>true if the two entities are effectively equivalent; otherwise, false.</returns>
        private static bool EntityPropertiesMatch(Entity entityA, Entity entityB, HashSet<(object?, object?)> visitedPairs)
        {
            // Check the entity's DataBlobs for exact information.
            List<BaseDataBlob> entityADataBlobs = entityA.GetAllDataBlobs();
            List<BaseDataBlob> entityBDataBlobs = entityB.GetAllDataBlobs();

            if (entityADataBlobs.Count != entityBDataBlobs.Count) return false;

            for (int index = 0; index < entityADataBlobs.Count; index++)
            {
                // Compare each DataBlob to ensure is has exact data.
                BaseDataBlob entityADataBlob = entityADataBlobs[index];
                BaseDataBlob entityBDataBlob = entityBDataBlobs[index];
                if (AreEqualInternal(entityADataBlob, entityBDataBlob, visitedPairs))
                    continue;
                return false;
            }
            return true;
        }
    }


}
