using System;
using Newtonsoft.Json;
using Pulsar4X.Engine.Auth;
using Pulsar4X.Modding;
using Pulsar4X.DataStructures;
using Pulsar4X.Blueprints;
using Pulsar4X.Interfaces;

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
        public IOrderHandler OrderHandler { get; private set; }

        internal event EventHandler PostLoad;

        public Game()
        {
            GlobalManager = new EntityManager(this, true);
            ProcessorManager = new ProcessorManager(this);
            Themes = new();
            AtmosphericGases = new();
            SystemGenSettings = new();
            TimePulse = new (this);
        }

        public Game(ModDataStore modDataStore)
        {
            GlobalManager = new EntityManager(this, true);
            Themes = new SafeDictionary<string, ThemeBlueprint>(modDataStore.Themes);
            AtmosphericGases = new SafeDictionary<string, GasBlueprint>(modDataStore.AtmosphericGas);
            SystemGenSettings = modDataStore.SystemGenSettings["default-system-gen-settings"];
            TimePulse = new (this);
        }
    }
}