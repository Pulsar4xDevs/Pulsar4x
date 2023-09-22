using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
//this allows the test project to see internal functions of this project.
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("Pulsar4X.Tests")]

namespace Pulsar4X.ECSLib
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Game
    {
        #region Properties

        [PublicAPI]
        [JsonProperty(Order = 3)]
        public StaticDataStore StaticData { get; } = new StaticDataStore();

        [PublicAPI]
        [JsonProperty(Order = 7)]
        public List<Player> Players = new List<Player>();
        public List<Entity> Factions = new List<Entity>();
        [PublicAPI]
        [JsonProperty(Order = 8)]
        public Player SpaceMaster = new Player("Space Master", "");

        [PublicAPI]
        [JsonProperty(Order = 9)]
        public Entity GameMasterFaction;

        [PublicAPI]
        public bool IsLoaded { get; internal set; } = false;

        //internal ProcessorManager ProcessorManager;

        /// <summary>
        /// List of StarSystems currently in the game.
        /// </summary>
        [JsonProperty(Order = 5)]
        public Dictionary<Guid, StarSystem> Systems { get; private set; } = new Dictionary<Guid, StarSystem>();

        [JsonProperty(Order = 4)]
        public readonly EntityManager GlobalManager;
        internal readonly Dictionary<Guid, EntityManager> GlobalManagerDictionary = new Dictionary<Guid, EntityManager>();

        internal IOrderHandler OrderHandler { get; set; }




        [PublicAPI]
        [JsonProperty(Order = 2)]
        public MasterTimePulse GamePulse { get { return StaticRefLib.GamePulse; } }

        [JsonProperty(Order = 6)]
        internal GalaxyFactory GalaxyGen { get; private set; }



        private PathfindingManager _pathfindingManager;

        [PublicAPI]
        [JsonProperty(Order = 1)]
        public GameSettings Settings { get { return StaticRefLib.GameSettings; } }//TODO remove from here and get from RefLib

        public Random RNG { get; } = new Random(12345689);

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

            OrderHandler = new StandAloneOrderHandler(this);


            StaticRefLib.Setup(this);

            GlobalManager = new EntityManager(this, true);
            StaticRefLib.SetEventlog(new EventLog(this));
            GameMasterFaction = FactionFactory.CreatePlayerFaction(this, SpaceMaster, "SpaceMaster Faction");

        }

        public Game([NotNull] NewGameSettings newGameSettings) : this()
        {
            if (newGameSettings == null)
            {
                throw new ArgumentNullException(nameof(newGameSettings));
            }

            GalaxyGen = new GalaxyFactory(true, newGameSettings.MasterSeed);

            StaticRefLib.GameSettings = newGameSettings;
            GamePulse.GameGlobalDateTime = newGameSettings.StartDateTime;

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

                foreach (var kvp in newGameSettings.DefaultHaltOnEvents)
                {
                    //defaultPlayer.HaltsOnEvent.Add(kvp.Key, kvp.Value);
                }

                if (newGameSettings.DefaultSolStart ?? false)
                {
                    //DefaultStartFactory.DefaultHumans(this, newGameSettings.DefaultFactionName);
                }
                else
                {
                    //FactionFactory.CreatePlayerFaction(this, defaultPlayer, newGameSettings.DefaultFactionName);
                }
            }

            // Temp: This will be reworked later.
            GenerateSystems(new AuthenticationToken(SpaceMaster, newGameSettings.SMPassword), newGameSettings.MaxSystems);

            /*
            GlobalManager.ManagerSubpulses.Init(GlobalManager);
            foreach (StarSystem starSys in this.Systems.Values)
            {
                starSys.ManagerSubpulses.Init(starSys);
            }*/

            // Fire PostLoad event
            PostLoad += (sender, args) => { InitializeProcessors(); };



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
                    foreach (Guid systemGuid in accessRole.Key.GetDataBlob<FactionInfoDB>().KnownSystems)
                    {
                        StarSystem system = Systems[systemGuid];
                        if (!systems.Contains(system))
                        {
                            systems.Add(system);
                        }
                    }
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
                    if (accessRole.Key.GetDataBlob<FactionInfoDB>().KnownSystems.Contains(systemGuid))
                    {
                        return Systems[systemGuid];
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

            Player foundPlayer = Players.Find(player => player.ID == authToken?.PlayerID);
            return foundPlayer?.IsTokenValid(authToken) != null ? foundPlayer : null;
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
