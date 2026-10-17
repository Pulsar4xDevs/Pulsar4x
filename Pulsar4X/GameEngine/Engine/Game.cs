using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Pulsar4X.Engine.Auth;
using Pulsar4X.Modding;
using Pulsar4X.DataStructures;
using Pulsar4X.Blueprints;
using Pulsar4X.Interfaces;
using Pulsar4X.Engine.Orders;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using GameEngine.Engine.Orders;
using Pulsar4X.Events;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Energy;
using Pulsar4X.JumpPoints;
using Pulsar4X.Sensors;
using Pulsar4X.Messaging;
[assembly: InternalsVisibleTo("Pulsar4X.Tests")]

namespace Pulsar4X.Engine
{
    public class Game
    {
        /// <summary>
        /// Entities like Suns, Planets, Asteroids etc are considered Neutral
        /// and should have their FactionOwnerID set equal to NeutralFactionID
        /// </summary>
        public static readonly int NeutralFactionId = -99;

        [JsonProperty]
        public string Name { get; set; }

        [JsonProperty]
        public string CreatedOnGitHash { get; set; }

        [JsonProperty]
        public string LastSaveGitHash { get; set; }

        [JsonProperty]
        public MasterTimePulse TimePulse { get; internal set; }

        [JsonProperty]
        public SafeDictionary<string, ThemeBlueprint> Themes { get; internal set; }

        [JsonProperty]
        public SafeDictionary<string, GasBlueprint> AtmosphericGases { get; internal set; }

        [JsonProperty]
        public SafeDictionary<string, TechCategoryBlueprint> TechCategories { get; internal set; }

        [JsonProperty]
        public SystemGenSettingsBlueprint SystemGenSettings { get; internal set; }
        /// <summary>
        /// List of StarSystems currently in the game.
        /// </summary>
        [JsonProperty]
        public List<StarSystem> Systems { get; internal set; } = new ();

        [JsonProperty]
        public EntityManager GlobalManager { get; internal set; }

        [JsonProperty]
        internal readonly SafeDictionary<string, EntityManager> GlobalManagerDictionary = new ();

        [JsonIgnore]
        internal ProcessorManager ProcessorManager { get; private set; }

        [JsonProperty]
        public Player SpaceMaster { get; internal set; } = new Player("Space Master", "");

        [JsonProperty]
        public List<Player> Players { get; internal set; }= new List<Player>();

        [JsonIgnore]
        public IOrderHandler OrderHandler { get; internal set; }

        [JsonProperty]
        public Entity GameMasterFaction { get; internal set; }

        [JsonProperty]
        public GameSettings Settings { get; internal set; }

        [JsonProperty]
        public ModDataStore StartingGameData { get; private set; }

        [JsonProperty]
        public GalaxyFactory GalaxyGen { get; private set; }

        [JsonProperty]
        public Dictionary<int, Entity> Factions { get; } = new ();

        /// <summary>
        /// Tracks the next available faction mask index (0-31).
        /// Used by FactionFactory when creating new factions.
        /// </summary>
        [JsonProperty]
        internal int NextFactionMaskIndex { get; set; } = 0;

        /// <summary>
        /// Maximum number of factions supported by the mask system (32 bits in an int).
        /// </summary>
        public const int MaxFactions = 32;

        /// <summary>
        /// Faction the local host binds on load. A future server ignores this when a connection names its own faction.
        /// -1 means this save has no human faction.
        /// </summary>
        [JsonProperty]
        public int LocalHumanFactionId { get; set; } = -1;

        // This is horribly named, it generates the ID's for the ICargoables NOT Entities.
        // Setters restore the statics. Field initializers call GetEntityID while the graph is built,
        // so OnDeserialized writes the saved values again after that walk.
        [JsonProperty]
        private int EntityIDCounterValue
        {
            get => EntityIDCounter;
            set
            {
                _restoredEntityIdCounter = value;
                EntityIDCounter = value;
            }
        }

        [JsonProperty]
        internal int NextEntityID
        {
            get => EntityIDGenerator.NextId;
            set
            {
                _restoredNextEntityId = value;
                EntityIDGenerator.NextId = value;
            }
        }

        private int _restoredEntityIdCounter;
        private int _restoredNextEntityId;

        internal static int EntityIDCounter = 0;

        [OnDeserialized]
        private void RestoreIdCounters(StreamingContext context)
        {
            EntityIDCounter = _restoredEntityIdCounter;
            EntityIDGenerator.NextId = _restoredNextEntityId;
        }

        internal event EventHandler PostLoad;

        internal Random RNG => GlobalManager.RNG;

        public Game() { }

        /// <summary>
        /// Clears all static/singleton state to prepare for a new game session.
        /// This must be called before creating a new game to prevent state leakage
        /// from a previous game session.
        /// </summary>
        internal static void ClearGlobalState()
        {
            EventManager.Instance.Clear();
            MessagePublisher.Instance.Clear();
        }

        public Game(NewGameSettings settings, ModDataStore modDataStore)
        {
            ClearGlobalState();
            ApplyModData(modDataStore);
            ApplySettings(settings);

            if(SystemGenSettings == null) throw new ArgumentNullException("SystemGenSettings cannot be null");
            if(Settings == null) throw new ArgumentNullException("Settings cannot be null");

            TimePulse = new (this);
            TimePulse.Initialize(this);
            ProcessorManager = new ProcessorManager(this);
            OrderHandler = new StandAloneOrderHandler(this);
            GlobalManager = new EntityManager();
            GlobalManager.Initialize(this, settings.MasterSeed);
            GameMasterFaction = FactionFactory.CreateSpaceMasterFaction(this, SpaceMaster, "SpaceMaster Faction");
            GalaxyGen = new GalaxyFactory(SystemGenSettings);
        }

        public void ApplySettings(NewGameSettings settings)
        {
            Settings = settings;
        }

        public void ApplyModData(ModDataStore modDataStore)
        {
            StartingGameData = modDataStore;
            Themes = new SafeDictionary<string, ThemeBlueprint>(modDataStore.Themes);
            AtmosphericGases = new SafeDictionary<string, GasBlueprint>(modDataStore.AtmosphericGas);
            TechCategories = new SafeDictionary<string, TechCategoryBlueprint>(modDataStore.TechCategories);
            SystemGenSettings = modDataStore.SystemGenSettings["default-system-gen-settings"];
        }

        [CanBeNull]
        public Player? GetPlayerForToken(AuthenticationToken authToken)
        {
            if (SpaceMaster.IsTokenValid(authToken))
            {
                return SpaceMaster;
            }

            Player? foundPlayer = Players.Find(player => player.ID == authToken?.PlayerID);
            return foundPlayer?.IsTokenValid(authToken) != null ? foundPlayer : null;
        }

        public GasBlueprint GetGasBySymbol(string symbol)
        {
            return AtmosphericGases.Values.Where(g => g.ChemicalSymbol.Equals(symbol)).First();
        }

        public static int GetEntityID() => EntityIDCounter++;

        /// <summary>
        /// Allocates and returns the next available faction mask index.
        /// </summary>
        /// <returns>A unique index (0-31) for use in faction bit masks.</returns>
        /// <exception cref="InvalidOperationException">Thrown if all 32 faction slots are used.</exception>
        internal int AllocateFactionMaskIndex()
        {
            if (NextFactionMaskIndex >= MaxFactions)
                throw new InvalidOperationException($"Cannot create more than {MaxFactions} factions.");
            return NextFactionMaskIndex++;
        }

        public static string Save(Game game)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings() {
                Formatting = Formatting.Indented,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                TypeNameHandling = TypeNameHandling.Objects,
                ContractResolver = new NonPublicResolver()
            };

            return JsonConvert.SerializeObject(game, settings);
        }

        public static Game Load(string json, Action<Game>? accept = null)
        {
            int previousCargoId = EntityIDCounter;
            int previousEntityId = EntityIDGenerator.NextId;

            // Restores the id counters from before this load. Returns false when a
            // debugger is attached so the catch below does not run, and the debugger
            // stops on the line that threw.
            bool RestoreIds(int cargoId, int entityId)
            {
                EntityIDCounter = cargoId;
                EntityIDGenerator.NextId = entityId;
                return !Debugger.IsAttached;
            }

            Game loadedGame;
            try
            {
                loadedGame = ReadGame(json);
                // Manager is null until SetEntities. The host reads FactionInfoDB
                // through Entity.TryGetDataBlob, so entities are attached before
                // accept. Processor setup stays in Rehydrate: those processors
                // subscribe on the event bus, and that bus is cleared only after
                // accept succeeds.
                foreach (var mgr in loadedGame.GlobalManagerDictionary)
                    mgr.Value.SetEntities();

                accept?.Invoke(loadedGame);
            }
            catch (Exception) when (RestoreIds(previousCargoId, previousEntityId))
            {
                throw;
            }

            Rehydrate(loadedGame);
            return loadedGame;
        }

        static Game ReadGame(string json)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings() {
                Formatting = Formatting.Indented,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                TypeNameHandling = TypeNameHandling.Objects,
                ContractResolver = new NonPublicResolver(),
            };
            var loadedGame = JsonConvert.DeserializeObject<Game>(json, settings);
            if (loadedGame == null)
                throw new InvalidOperationException("Could not read that save.");
            return loadedGame;
        }

        static void Rehydrate(Game loadedGame)
        {
            ClearGlobalState();
            loadedGame.TimePulse.Initialize(loadedGame);
            loadedGame.ProcessorManager = new ProcessorManager(loadedGame);
            loadedGame.OrderHandler = new StandAloneOrderHandler(loadedGame);

            foreach (var mgr in loadedGame.GlobalManagerDictionary)
                mgr.Value.Initialize(loadedGame, postLoad: true);

            // Hook up the event logs
            foreach(var (id, faction) in loadedGame.Factions)
            {
                var info = faction.GetDataBlob<FactionInfoDB>();
                FactionStanceRules.MigrateGrants(info);
                info.EventLog.Subscribe();
            }
            loadedGame.GameMasterFaction.GetDataBlob<FactionInfoDB>().EventLog.Subscribe();

            foreach (var manager in loadedGame.GlobalManagerDictionary.Values)
                RehydrateActions(loadedGame, manager);

            foreach (var system in loadedGame.Systems)
                system.UpdateActivityState();

            // settings.Context = new StreamingContext(StreamingContextStates.All, loadedGame);
            // loadedGame.TimePulse = JsonConvert.DeserializeObject<MasterTimePulse>(JObject.Parse(json)["GameInfo"]["TimePulse"].ToString(), settings);
            // loadedGame.ProcessorManager = new ProcessorManager(loadedGame);
            // //loadedGame.ProcessorManager = JsonConvert.DeserializeObject<ProcessorManager>(JObject.Parse(json)["GameInfo"]["ProcessManager"].ToString(), settings);
            // loadedGame.GlobalManager = JsonConvert.DeserializeObject<EntityManager>(JObject.Parse(json)["GameInfo"]["GlobalManager"].ToString(), settings);
            // loadedGame.GlobalManager.Initialize(loadedGame);
            // loadedGame.GameMasterFaction = JsonConvert.DeserializeObject<Entity>(JObject.Parse(json)["GameInfo"]["GameMasterFaction"].ToString(), settings);
            // StandAloneOrderHandler currently doesn't need to be serialized
            // loadedGame.OrderHandler = JsonConvert.DeserializeObject<StandAloneOrderHandler>(JObject.Parse(json)["OrderHandler"].ToString(), settings);
        }

        static void RehydrateActions(Game game, EntityManager manager)
        {
            foreach (var entity in manager.GetAllEntitiesWithDataBlob<ActionQueueDB>())
            {
                var queue = entity.GetDataBlob<ActionQueueDB>();
                foreach (var action in queue.ActionList)
                    action.Rehydrate(game);
            }
        }

        public void PostNewGameInitialization()
        {
            // Link all JumpPoints between systems
            JPFactory.LinkAllJumpPoints(this);

            // There are few DB's that need to run the processor when the game begins
            foreach(var system in Systems)
            {
                var entitiesWithEnergyGen = system.GetAllEntitiesWithDataBlob<EnergyGenAbilityDB>();
                foreach (var entity in entitiesWithEnergyGen)
                {
                    ProcessorManager.GetInstanceProcessor(nameof(EnergyGenProcessor)).ProcessEntity(entity, TimePulse.GameGlobalDateTime);
                }

                var entitiesWithSensors = system.GetAllEntitiesWithDataBlob<SensorAbilityDB>();
                foreach (var entity in entitiesWithSensors)
                {
                    ProcessorManager.GetInstanceProcessor(nameof(SensorScan)).ProcessEntity(entity, TimePulse.GameGlobalDateTime);
                }

                // Systems with faction entities start as Background, others stay Stasis (default)
                system.UpdateActivityState();
            }
        }
    }

    // public class GameConverter : JsonConverter
    // {
    //     public override bool CanConvert(Type objectType)
    //     {
    //         return objectType == typeof(Game);
    //     }

    //     public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    //     {
    //         var jsonObject = JToken.Load(reader);

    //         List<ModManifest> modManifests = jsonObject["ModInfo"].ToObject<List<ModManifest>>(serializer);

    //         ModLoader modLoader = new ModLoader();
    //         ModDataStore modDataStore = new ModDataStore();

    //         foreach(var manifest in modManifests)
    //         {
    //             var modInfoFilePath = Path.Combine(manifest.ModDirectory, "modInfo.json");

    //             modLoader.LoadModManifest(modInfoFilePath, modDataStore);
    //         }

    //         var settings = jsonObject["Settings"].ToObject<NewGameSettings>(serializer);
    //         //var settings = new NewGameSettings();

    //         var game = jsonObject["GameInfo"].ToObject<Game>(serializer);
    //         game.ApplySettings(settings);
    //         game.ApplyModData(modDataStore);
    //         game.Initialize();

    //         return game;
    //     }

    //     public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    //     {
    //         var game = (Game)value;
    //         var jsonObject = new JObject();

    //         var modInfo = JToken.FromObject(game.StartingGameData.ModManifests, serializer);
    //         var settings = JToken.FromObject(game.Settings, serializer);
    //         var gameInfo = new JObject();

    //         // For each property in the Game class, serialize it using the serializer.
    //         gameInfo["TimePulse"] = JToken.FromObject(game.TimePulse, serializer);

    //         // ProcessManager currently doesn't need to be serialized
    //         //gameInfo["ProcessManager"] = JToken.FromObject(game.ProcessorManager, serializer);

    //         // StandAloneOrderHandler currently doesn't need to be serialized
    //         // gameInfo["OrderHandler"] = JToken.FromObject(game.OrderHandler, serializer);

    //         gameInfo["GlobalManager"] = JToken.FromObject(game.GlobalManager, serializer);
    //         gameInfo["GameMasterFaction"] = JToken.FromObject(game.GameMasterFaction, serializer);
    //         gameInfo["Systems"] = JToken.FromObject(game.Systems, serializer);
    //         // TODO: Serialize the other properties.

    //         jsonObject["ModInfo"] = modInfo;
    //         jsonObject["Settings"] = settings;
    //         jsonObject["GameInfo"] = gameInfo;

    //         jsonObject.WriteTo(writer);
    //     }
    // }
}