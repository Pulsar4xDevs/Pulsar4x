using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Game
    {
        #region Properties

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
        public ReadOnlyDictionary<Guid, StarSystem> Systems => new ReadOnlyDictionary<Guid, StarSystem>(StarSystems);

        /// <summary>
        /// List of StarSystems currently in the game.
        /// </summary>
        [JsonProperty]
        internal Dictionary<Guid, StarSystem> StarSystems { get; private set; }

        [PublicAPI] 
        [JsonProperty]
        public Guid GameMasterFaction;

        /// <summary>
        /// Global Entity Manager.
        /// </summary>
        [PublicAPI]
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

        [JsonProperty]
        internal List<LogEvent> _logEvents;

        internal readonly Dictionary<Guid, EntityManager> GlobalGuidDictionary = new Dictionary<Guid, EntityManager>();
        internal readonly ReaderWriterLockSlim GuidDictionaryLock = new ReaderWriterLockSlim();

        [JsonProperty] public bool EnableMultiThreading = true;

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
        /// <param name="progress"></param>
        /// <exception cref="ArgumentNullException"><paramref name="gameName"/> is <see langword="null" />.</exception>
        /// <exception cref="StaticDataLoadException">Thrown in a variety of situations when StaticData could not be loaded.</exception>
        [PublicAPI]
        public static Game NewGame([NotNull] string gameName, DateTime startDateTime, int numSystems, List<string> dataSets = null, IProgress<double> progress = null)
        {
            if (gameName == null)
            {
                throw new ArgumentNullException(nameof(gameName));
            }

            var newGame = new Game {GameName = gameName, CurrentDateTime = startDateTime};
            if (dataSets == null || dataSets.Count == 0)
            {
                StaticDataManager.LoadData("Pulsar4x", newGame);
            }
            else
            {
                foreach (string dataSet in dataSets)
                {
                    StaticDataManager.LoadData(dataSet, newGame);
                    StaticDataManager.LoadData(dataSet, newGame);
                }
            }
            FactionFactory.CreateGameMaster(newGame);

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
        #endregion


        #endregion
    }
}
