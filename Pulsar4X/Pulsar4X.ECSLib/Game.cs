﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public List<Player> Players;
        
        [PublicAPI]
        [JsonProperty]
        public Player SpaceMaster;

        [PublicAPI]
        [JsonProperty]
        public Entity GameMasterFaction;

        [PublicAPI]
        public string GameName
        {
            get { return _gameName; }
            internal set { _gameName = value; }
        }
        [JsonProperty]
        private string _gameName;

        [PublicAPI]
        public VersionInfo Version => VersionInfo.PulsarVersionInfo;

        [PublicAPI]
        public bool IsLoaded { get; internal set; }

        [PublicAPI]
        public DateTime CurrentDateTime
        {
            get { return _currentDateTime; }
            internal set { _currentDateTime = value; }
        }
        [JsonProperty]
        private DateTime _currentDateTime;

        [JsonProperty]
        internal int NumSystems;

        [PublicAPI]
        [Obsolete("Use game.GetSystems(AuthenticationToken) or game.GetSystem(AuthenticationToken, Guid)")]
        public ReadOnlyDictionary<Guid, StarSystem> Systems => new ReadOnlyDictionary<Guid, StarSystem>(StarSystems);

        /// <summary>
        /// List of StarSystems currently in the game.
        /// </summary>
        [JsonProperty]
        internal Dictionary<Guid, StarSystem> StarSystems { get; private set; }

        /// <summary>
        /// Global Entity Manager.
        /// </summary>
        [PublicAPI]
        [Obsolete("Will be made internal to conform with data hiding.")]
        public EntityManager GlobalManager => _globalManager;

        [JsonProperty]
        private readonly EntityManager _globalManager;

        [PublicAPI]
        [JsonProperty]
        public StaticDataStore StaticData { get; internal set; }

        [CanBeNull]
        [PublicAPI]
        public PulseInterrupt CurrentInterrupt { get; private set; }

        [PublicAPI]
        public SubpulseLimit NextSubpulse { get; private set; }

        [JsonProperty]
        internal GalaxyFactory GalaxyGen { get; private set; }

        [PublicAPI]
        public ReadOnlyCollection<LogEvent> LogEvents => new ReadOnlyCollection<LogEvent>(_logEvents);

        public bool EnableMultiThreading
        {
            get
            {
                return _enableMultiThreading;
            }

            set
            {
                _enableMultiThreading = value;
            }
        }
        [JsonProperty]
        private bool _enableMultiThreading = true;

        [JsonProperty]
        internal List<LogEvent> _logEvents;

        internal readonly Dictionary<Guid, EntityManager> GlobalGuidDictionary = new Dictionary<Guid, EntityManager>();
        internal readonly ReaderWriterLockSlim GuidDictionaryLock = new ReaderWriterLockSlim();


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
            IsLoaded = false;
            _globalManager = new EntityManager(this);
            StarSystems = new Dictionary<Guid, StarSystem>();
            NextSubpulse = new SubpulseLimit();
            GalaxyGen = new GalaxyFactory(true);
            StaticData = new StaticDataStore();
            _logEvents = new List<LogEvent>();

            PostLoad += (sender, args) => { InitializeProcessors(); };
        }

        #endregion

        #region Functions

        #region Internal Functions

        internal void PostGameLoad()
        {
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

        /// <summary>
        /// </summary>
        /// <param name="gameName"></param>
        /// <param name="startDateTime"></param>
        /// <param name="numSystems"></param>
        /// <param name="smPassword">Password for the SpaceMaster</param>
        /// <param name="dataSets">IEnumerable containing filePaths to mod directories to load.</param>
        /// <param name="progress"></param>
        /// <exception cref="ArgumentNullException"><paramref name="gameName"/> is <see langword="null" />.</exception>
        /// <exception cref="StaticDataLoadException">Thrown in a variety of situations when StaticData could not be loaded.</exception>
        [PublicAPI]
        public static Game NewGame([NotNull] string gameName, DateTime startDateTime, int numSystems, string smPassword = "", IEnumerable<string> dataSets = null, IProgress<double> progress = null)
        {
            if (gameName == null)
            {
                throw new ArgumentNullException(nameof(gameName));
            }

            var newGame = new Game {GameName = gameName, CurrentDateTime = startDateTime};

            // Load static data
            if (dataSets != null)
            {
                foreach (string dataSet in dataSets)
                {
                    StaticDataManager.LoadData(dataSet, newGame);
                }
            }
            if (newGame.StaticData.LoadedDataSets.Count == 0)
            {
                StaticDataManager.LoadData("Pulsar4x", newGame);
            }

            // Create SM
            newGame.SpaceMaster = new Player("Space Master", smPassword);
            newGame.Players = new List<Player>();
            newGame.GameMasterFaction = FactionFactory.CreatePlayerFaction(newGame, newGame.SpaceMaster, "SpaceMaster Faction");

            // Generate systems
            for (int i = 0; i < numSystems; i++)
            {
                newGame.GalaxyGen.StarSystemFactory.CreateSystem(newGame, "System #" + i);
                progress?.Report((double)newGame.StarSystems.Count / numSystems);
            }

            newGame.PostGameLoad();

            return newGame;
        }

        /// <summary>
        /// Time advancement code. Attempts to advance time by the number of seconds
        /// passed to it.
        /// Interrupts may prevent the entire requested timeframe from being advanced.
        /// </summary>
        /// <param name="deltaSeconds">Time Advance Requested</param>
        /// <param name="progress">IProgress implementation to report progress.</param>
        /// <returns>Total Time Advanced</returns>
        /// <exception cref="OperationCanceledException">Thrown when a cancellation request is honored.</exception>
        [PublicAPI]
        public int AdvanceTime(int deltaSeconds, IProgress<double> progress = null)
        {
            return AdvanceTime(deltaSeconds, CancellationToken.None, progress);
        }

        /// <summary>
        /// Time advancement code. Attempts to advance time by the number of seconds
        /// passed to it.
        /// 
        /// Interrupts may prevent the entire requested timeframe from being advanced.
        /// </summary>
        /// <param name="deltaSeconds">Time Advance Requested</param>
        /// <param name="cancellationToken">Cancellation token for this request.</param>
        /// <param name="progress">IProgress implementation to report progress.</param>
        /// <exception cref="OperationCanceledException">Thrown when a cancellation request is honored.</exception>
        /// <returns>Total Time Advanced (in seconds)</returns>
        [PublicAPI]
        public int AdvanceTime(int deltaSeconds, CancellationToken cancellationToken, IProgress<double> progress = null)
        {
            int timeAdvanced = 0;

            // Clamp deltaSeconds to a multiple of our MinimumTimestep.
            deltaSeconds = deltaSeconds - deltaSeconds % GameConstants.MinimumTimestep;
            if (deltaSeconds == 0)
            {
                deltaSeconds = GameConstants.MinimumTimestep;
            }

            // Clear any interrupt flag before starting the pulse.
            CurrentInterrupt = null;
            while ((CurrentInterrupt == null) && (deltaSeconds > 0))
            {
                cancellationToken.ThrowIfCancellationRequested();
                int subpulseTime = Math.Min(NextSubpulse.MaxSeconds, deltaSeconds);
                // Set next subpulse to max value. If it needs to be shortened, it will
                // be shortened in the pulse execution.
                NextSubpulse.MaxSeconds = int.MaxValue;

                // Update our date.
                CurrentDateTime += TimeSpan.FromSeconds(subpulseTime);

                // Execute all processors. Magic happens here.
                RunProcessors(StarSystems.Values.ToList(), deltaSeconds);

                // Update our remaining values.
                deltaSeconds -= subpulseTime;
                timeAdvanced += subpulseTime;
                progress?.Report((double)timeAdvanced / deltaSeconds);
            }

            if (CurrentInterrupt != null)
            {
                // Gamelog?
            }
            return timeAdvanced;
        }

        /// <summary>
        /// Runs all processors on the list of systems provided.
        /// </summary>
        /// <param name="systems">Systems to have processors run on them.</param>
        /// <param name="deltaSeconds">Game-time to progress in the processors.</param>
        [PublicAPI]
        public void RunProcessors(List<StarSystem> systems, int deltaSeconds)
        {
            OrbitProcessor.Process(this, systems, deltaSeconds);
            ShipMovementProcessor.Process(this, systems, deltaSeconds);
            
            EconProcessor.Process(this, systems, deltaSeconds);
        }

        [PublicAPI]
        public void AddLogEvent(LogEvent logEvent)
        {
            if (_logEvents.Count > 0)
            {
                LogEvent lastEvent = _logEvents.Last();
                if (lastEvent.Time > logEvent.Time)
                {
                    throw new InvalidOperationException("Cannot add a LogEvent that occured before the last LogEvent");
                }
            }

            _logEvents.Add(logEvent);
        }

        [PublicAPI]
        public List<LogEvent> GetEventsForFaction(Entity faction, bool factionIsSM = false)
        {
            return new List<LogEvent>(_logEvents.Where(@event => @event.Faction == faction || (factionIsSM && @event.IsSMOnly)));
        }

        [PublicAPI]
        public Player AddPlayer(string playerName, string playerPassword)
        {
            var player = new Player(playerName, playerPassword);
            Players.Add(player);
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
                    systems.AddRange(accessRole.Key.GetDataBlob<FactionInfoDB>().KnownSystems.Select(systemGuid => StarSystems[systemGuid]));
                }
            }
            return systems;
        }

        [PublicAPI]
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
                        return StarSystems[system];
                    }
                }
            }

            return null;
        }
        
        #endregion

        public Player GetPlayerForToken(AuthenticationToken authToken)
        {
            if (SpaceMaster.IsTokenValid(authToken))
            {
                return SpaceMaster;
            }

            return Players.FirstOrDefault(player => player.IsTokenValid(authToken));
        }

        #endregion
    }
}
