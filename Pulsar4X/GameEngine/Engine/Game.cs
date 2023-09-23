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

namespace Pulsar4X.Engine
{
    public class Game
    {
        public MasterTimePulse TimePulse { get; internal set; }

        public SafeDictionary<string, ThemeBlueprint> Themes { get; internal set; }
        public SafeDictionary<string, GasBlueprint> AtmosphericGases { get; internal set; }
        public SystemGenSettingsBlueprint SystemGenSettings { get; internal set; }
        /// <summary>
        /// List of StarSystems currently in the game.
        /// </summary>
        [JsonProperty(Order = 5)]
        public SafeDictionary<string, StarSystem> Systems { get; private set; } = new ();
        public EntityManager GlobalManager { get; }
        internal readonly SafeDictionary<string, EntityManager> GlobalManagerDictionary = new ();
        internal ProcessorManager ProcessorManager { get; private set; }
        public Player SpaceMaster = new Player("Space Master", "");
        public List<Player> Players = new List<Player>();
        public IOrderHandler OrderHandler { get; private set; }
        public Entity GameMasterFaction { get; private set; }
        public GameSettings Settings { get; private set; }
        public Random RNG { get; } = new Random(12345689);

        internal ModDataStore StartingGameData { get; private set; }
        internal GalaxyFactory GalaxyGen { get; private set; }
        public List<Entity> Factions { get; } = new List<Entity>();

        internal event EventHandler PostLoad;

        public Game(NewGameSettings settings, ModDataStore modDataStore)
        {
            Settings = settings;
            TimePulse = new (this);
            ProcessorManager = new ProcessorManager(this);
            OrderHandler = new StandAloneOrderHandler(this);
            GlobalManager = new EntityManager(this, true);
            Themes = new SafeDictionary<string, ThemeBlueprint>(modDataStore.Themes);
            AtmosphericGases = new SafeDictionary<string, GasBlueprint>(modDataStore.AtmosphericGas);
            SystemGenSettings = modDataStore.SystemGenSettings["default-system-gen-settings"];
            StartingGameData = modDataStore;
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
    }
}