using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Pulsar4X.ECSLib
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Game
    {
        #region Properties

        [PublicAPI]
        [JsonProperty]
        public List<Player> Players = new List<Player>();
        
        [PublicAPI]
        [JsonProperty]
        public Player SpaceMaster = new Player("Space Master", "");

        [PublicAPI]
        [JsonProperty]
        public Entity GameMasterFaction;

        [PublicAPI]
        public bool IsLoaded { get; internal set; } = false;

        [PublicAPI]
        public DateTime CurrentDateTime
        {
            get { return GameLoop.GameGlobalDateTime; }
        }


        /// <summary>
        /// List of StarSystems currently in the game.
        /// </summary>
        [JsonProperty]
        internal Dictionary<Guid, StarSystem> Systems { get; private set; } = new Dictionary<Guid, StarSystem>();

        [JsonProperty]
        public readonly EntityManager GlobalManager;

        [PublicAPI]
        [JsonProperty]
        public StaticDataStore StaticData { get; internal set; } = new StaticDataStore();


        /// <summary>
        /// this is used to marshal events to the UI thread. 
        /// </summary>
        internal SynchronizationContext SyncContext { get; private set; }

        [PublicAPI]
        [JsonProperty]
        public TimeLoop GameLoop { get; set; }

        [JsonProperty]
        internal GalaxyFactory GalaxyGen { get; private set; }

        [PublicAPI]
        public EventLog EventLog { get; internal set; }

        internal readonly Dictionary<Guid, EntityManager> GlobalGuidDictionary = new Dictionary<Guid, EntityManager>();
        internal readonly ReaderWriterLockSlim GuidDictionaryLock = new ReaderWriterLockSlim();
        private PathfindingManager _pathfindingManager;

        [PublicAPI]
        [JsonProperty]
        public GameSettings Settings { get; set; }

        [JsonProperty]
        private readonly OrbitProcessor _orbitProcessor = new OrbitProcessor();

        [JsonProperty]
        private readonly EconProcessor _econProcessor = new EconProcessor();

        #endregion

        #region Events
        /// <summary>
        /// PostLoad event fired when the game is loaded.
        /// Event is cleared each load.
        /// </summary>
        internal event EventHandler PostLoad;
        #endregion

        #region Constructors

        internal Game()
        {
            SyncContext = SynchronizationContext.Current;        
            GameLoop = new TimeLoop(this);            
            EventLog = new EventLog(this);
            GlobalManager = new EntityManager(this);
        }

        public Game([NotNull] NewGameSettings newGameSettings) : this()
        {
            if (newGameSettings == null)
            {
                throw new ArgumentNullException(nameof(newGameSettings));
            }

            GalaxyGen = new GalaxyFactory(true, newGameSettings.MasterSeed);

            Settings = newGameSettings;
            GameLoop.GameGlobalDateTime = newGameSettings.StartDateTime;

            // Load Static Data
            if (newGameSettings.DataSets != null)
            {
                foreach (string dataSet in newGameSettings.DataSets)
                {
                    StaticDataManager.LoadData(dataSet, this);
                }
            }
            if (StaticData.LoadedDataSets.Count == 0)
            {
                StaticDataManager.LoadData("Pulsar4x", this);
            }
            // Create SM


            SpaceMaster.ChangePassword(new AuthenticationToken(SpaceMaster, ""), newGameSettings.SMPassword);
            GameMasterFaction = FactionFactory.CreatePlayerFaction(this, SpaceMaster, "SpaceMaster Faction");

            if (newGameSettings.CreatePlayerFaction ?? false)
            {
                Player defaultPlayer = AddPlayer(newGameSettings.DefaultPlayerName, newGameSettings.DefaultPlayerPassword);

                foreach (var kvp in newGameSettings.DefaultHaltOnEvents)
                {
                    defaultPlayer.HaltsOnEvent.Add(kvp.Key, kvp.Value);
                }

                if (newGameSettings.DefaultSolStart ?? false)
                {
                    DefaultStartFactory.DefaultHumans(this, defaultPlayer, newGameSettings.DefaultFactionName);
                }
                else
                {
                    FactionFactory.CreatePlayerFaction(this, defaultPlayer, newGameSettings.DefaultFactionName);
                }
            }

            // Temp: This will be reworked later.
            GenerateSystems(new AuthenticationToken(SpaceMaster, newGameSettings.SMPassword), newGameSettings.MaxSystems);

            

            // Fire PostLoad event
            PostLoad += (sender, args) => { InitializeProcessors(); };
            foreach(StarSystem starSys in this.Systems.Values)
            {
                starSys.SystemManager.ManagerSubpulses.Initalise();
            }
        }

        #endregion

        #region Functions

        #region Internal Functions

        internal void PostGameLoad()
        {
            _pathfindingManager = new PathfindingManager(this);

            // Invoke the Post Load event down the chain.
            PostLoad?.Invoke(this, EventArgs.Empty);

            // set isLoaded to true:
            IsLoaded = true;

            // Post load event completed. Drop all handlers.
            PostLoad = null;
        }

        /// <summary>
        /// Prepares, and defines the order that processors are run in.
        /// </summary>
        private static void InitializeProcessors()
        {
            ShipMovementProcessor.Initialize();
            //InstallationProcessor.Initialize();
        }

        #endregion

        #region Public API

        [PublicAPI]
        public Player AddPlayer(string playerName, string playerPassword = "")
        {
            var player = new Player(playerName, playerPassword);
            Players.Add(player);
            EventLog.AddPlayer(player);
            return player;
        }

        [PublicAPI]
        public List<StarSystem> GetSystems(AuthenticationToken authToken)
        {
            Player player = GetPlayerForToken(authToken);
            var systems = new List<StarSystem>();

            if (player?.AccessRoles == null)
            {
                return systems;
            }

            foreach (KeyValuePair<Entity, AccessRole> accessRole in player.AccessRoles)
            {
                // TODO: Implement vision access roles.
                if ((accessRole.Value & AccessRole.FullAccess) == AccessRole.FullAccess)
                {
                    systems.AddRange(accessRole.Key.GetDataBlob<FactionInfoDB>().KnownSystems.Select(systemGuid => Systems[systemGuid]));
                }
            }
            return systems;
        }

        [PublicAPI]
        [CanBeNull]
        public StarSystem GetSystem(AuthenticationToken authToken, Guid systemGuid)
        {
            Player player = GetPlayerForToken(authToken);

            if (player?.AccessRoles == null)
            {
                return null;
            }

            foreach (KeyValuePair<Entity, AccessRole> accessRole in player.AccessRoles)
            {
                // TODO: Implement vision access roles.
                if ((accessRole.Value & AccessRole.FullAccess) == AccessRole.FullAccess)
                {
                    foreach (Guid system in accessRole.Key.GetDataBlob<FactionInfoDB>().KnownSystems.Where(system => system == systemGuid))
                    {
                        return Systems[system];
                    }
                }
            }

            return null;
        }
        
        #endregion

        [CanBeNull]
        public Player GetPlayerForToken(AuthenticationToken authToken)
        {
            if (SpaceMaster.IsTokenValid(authToken))
            {
                return SpaceMaster;
            }

            foreach (Player player in Players.Where(player => player.ID == authToken?.PlayerID))
            {
                return player.IsTokenValid(authToken) ? player : null;
            }

            return null;
        }

        [PublicAPI]
        public void GenerateSystems(AuthenticationToken authToken, int numSystems)
        {
            var systemSeeds = new List<int>(numSystems);

            for (int i = 0; i < numSystems; i++)
            {
                systemSeeds.Add(GalaxyGen.SeedRNG.Next());
            }

            GenerateSystems(authToken, systemSeeds);
        }

        [PublicAPI]
        public void GenerateSystems(AuthenticationToken authToken, List<int> systemSeeds)
        {
            if (SpaceMaster.IsTokenValid(authToken))
            {
                foreach (int systemSeed in systemSeeds)
                {
                    GalaxyGen.StarSystemFactory.CreateSystem(this, $"Star System #{Systems.Count + 1}", systemSeed);
                }
            }
        }

        #endregion
    }
}
