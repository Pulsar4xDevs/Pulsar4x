using System;
using Newtonsoft.Json;
using Pulsar4X.Modding;
using Pulsar4X.DataStructures;
using Pulsar4X.Blueprints;

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

        internal event EventHandler PostLoad;

        public Game()
        {
            GlobalManager = new EntityManager(this, true);
            Themes = new();
            AtmosphericGases = new();
            SystemGenSettings = new();
            TimePulse = new ();
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