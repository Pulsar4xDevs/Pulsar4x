using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.Engine.Auth;
using Pulsar4X.Modding;
using Pulsar4X.DataStructures;
using Pulsar4X.Blueprints;
using Pulsar4X.Interfaces;
using Pulsar4X.Engine.Orders;
using Newtonsoft.Json.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
[assembly: InternalsVisibleTo("Pulsar4X.Tests")]

namespace Pulsar4X.Engine
{
    [JsonConverter(typeof(GameConverter))]
    public class Game
    {
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
        [JsonProperty(Order = 5)]
        public SafeDictionary<string, StarSystem> Systems { get; private set; } = new ();

        [JsonProperty]
        public EntityManager GlobalManager { get; internal set; }

        [JsonProperty]
        internal readonly SafeDictionary<string, EntityManager> GlobalManagerDictionary = new ();

        [JsonProperty]
        internal ProcessorManager ProcessorManager { get; private set; }

        [JsonProperty]
        public Player SpaceMaster = new Player("Space Master", "");

        [JsonProperty]
        public List<Player> Players = new List<Player>();

        [JsonProperty]
        public IOrderHandler OrderHandler { get; private set; }

        [JsonProperty]
        public Entity GameMasterFaction { get; private set; }

        [JsonProperty]
        public GameSettings Settings { get; private set; }

        [JsonProperty]
        public Random RNG { get; } = new Random(12345689);

        [JsonProperty]
        internal ModDataStore StartingGameData { get; private set; }

        [JsonProperty]
        internal GalaxyFactory GalaxyGen { get; private set; }

        [JsonProperty]
        public List<Entity> Factions { get; } = new List<Entity>();

        [JsonProperty]
        private int EntityIDCounterValue => EntityIDCounter;

        private static int EntityIDCounter = 0;

        internal event EventHandler PostLoad;

        public Game(NewGameSettings settings, ModDataStore modDataStore)
        {
            StartingGameData = modDataStore;
            Themes = new SafeDictionary<string, ThemeBlueprint>(modDataStore.Themes);
            AtmosphericGases = new SafeDictionary<string, GasBlueprint>(modDataStore.AtmosphericGas);
            TechCategories = new SafeDictionary<string, TechCategoryBlueprint>(modDataStore.TechCategories);
            SystemGenSettings = modDataStore.SystemGenSettings["default-system-gen-settings"];

            Settings = settings;
            TimePulse = new (this);
            ProcessorManager = new ProcessorManager(this);
            OrderHandler = new StandAloneOrderHandler(this);
            GlobalManager = new EntityManager(this, true);
            GameMasterFaction = FactionFactory.CreatePlayerFaction(this, SpaceMaster, "SpaceMaster Faction");
            GalaxyGen = new GalaxyFactory(SystemGenSettings, settings.MasterSeed);
        }

        [CanBeNull]
        public Player GetPlayerForToken(AuthenticationToken authToken)
        {
            if (SpaceMaster.IsTokenValid(authToken))
            {
                return SpaceMaster;
            }

            Player foundPlayer = Players.Find(player => player.ID == authToken?.PlayerID);
            return foundPlayer?.IsTokenValid(authToken) != null ? foundPlayer : null;
        }

        public GasBlueprint GetGasBySymbol(string symbol)
        {
            return AtmosphericGases.Values.Where(g => g.ChemicalSymbol.Equals(symbol)).First();
        }

        public static int GetEntityID() => EntityIDCounter++;

        public static string Save(Game game)
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.All,
                Formatting = Formatting.Indented
            };

            JsonSerializerSettings settings = new JsonSerializerSettings() {
                Formatting = Formatting.Indented,
                PreserveReferencesHandling = PreserveReferencesHandling.All
            };

            return JsonConvert.SerializeObject(game, settings);
        }

        public static Game Load(string json)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings() {
                Formatting = Formatting.Indented,
                PreserveReferencesHandling = PreserveReferencesHandling.All
            };
            var loadedGame = JsonConvert.DeserializeObject<Game>(json, settings);

            settings.Context = new StreamingContext(StreamingContextStates.All, loadedGame);
            loadedGame.TimePulse = JsonConvert.DeserializeObject<MasterTimePulse>(JObject.Parse(json)["TimePulse"].ToString(), settings);
            loadedGame.ProcessorManager = JsonConvert.DeserializeObject<ProcessorManager>(JObject.Parse(json)["ProcessManager"].ToString(), settings);
            loadedGame.GlobalManager = JsonConvert.DeserializeObject<EntityManager>(JObject.Parse(json)["GlobalManager"].ToString(), settings);
            loadedGame.GameMasterFaction = JsonConvert.DeserializeObject<Entity>(JObject.Parse(json)["GameMasterFaction"].ToString(), settings);
            // StandAloneOrderHandler currently doesn't need to be serialized
            // loadedGame.OrderHandler = JsonConvert.DeserializeObject<StandAloneOrderHandler>(JObject.Parse(json)["OrderHandler"].ToString(), settings);

            return loadedGame;
        }
    }

    public class GameConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Game);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);

            ModDataStore startingGameData = jsonObject["StartingGameData"].ToObject<ModDataStore>(serializer);
            NewGameSettings settings = jsonObject["Settings"].ToObject<NewGameSettings>(serializer);

            var game = new Game(settings, startingGameData);

            // For each property in the Game class, deserialize it using the serializer.
            // game.TimePulse = jsonObject["TimePulse"].ToObject<MasterTimePulse>(serializer);

            // TODO: Deserialize the other properties.

            return game;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var game = (Game)value;
            var jsonObject = new JObject();

            // For each property in the Game class, serialize it using the serializer.
            jsonObject["Settings"] = JToken.FromObject(game.Settings, serializer);
            jsonObject["StartingGameData"] = JToken.FromObject(game.StartingGameData, serializer);
            jsonObject["TimePulse"] = JToken.FromObject(game.TimePulse, serializer);
            jsonObject["ProcessManager"] = JToken.FromObject(game.ProcessorManager, serializer);

            // StandAloneOrderHandler currently doesn't need to be serialized
            // jsonObject["OrderHandler"] = JToken.FromObject(game.OrderHandler, serializer);

            jsonObject["GlobalManager"] = JToken.FromObject(game.GlobalManager, serializer);
            jsonObject["GameMasterFaction"] = JToken.FromObject(game.GameMasterFaction, serializer);

            jsonObject["Themes"] = JToken.FromObject(game.Themes, serializer);
            jsonObject["AtmosphericGases"] = JToken.FromObject(game.AtmosphericGases, serializer);
            jsonObject["TechCategories"] = JToken.FromObject(game.TechCategories, serializer);

            // TODO: Serialize the other properties.

            jsonObject.WriteTo(writer);
        }
    }
}