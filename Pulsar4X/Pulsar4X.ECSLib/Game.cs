using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public VersionInfo Version { get { return VersionInfo.PulsarVersionInfo;} }

        [PublicAPI]
        [JsonIgnore]
        public bool IsLoaded { get; internal set; }

        [PublicAPI]
        public DateTime CurrentDateTime
        {
            get { return _currentDateTime; }
            internal set { _currentDateTime = value; }
        }
        [JsonProperty]
        private DateTime _currentDateTime;

        [PublicAPI]
        [JsonIgnore]
        public ReadOnlyCollection<StarSystem> Systems { get { return new ReadOnlyCollection<StarSystem>(StarSystems); } }

        /// <summary>
        /// Global Entity Manager.
        /// </summary>
        [PublicAPI]
        public EntityManager GlobalManager { get { return _globalManager; } }
        [JsonProperty]
        private EntityManager _globalManager;

        [PublicAPI]
        public StaticDataStore StaticData { get; private set; }

        [CanBeNull]
        [PublicAPI]
        public PulseInterrupt CurrentInterrupt { get; private set; }

        [PublicAPI]
        public SubpulseLimit NextSubpulse { get; private set; }

        [JsonProperty]
        internal GalaxyFactory GalaxyGen { get; private set; }

        /// <summary>
        /// List of StarSystems currently in the game.
        /// </summary>
        [JsonProperty]
        internal List<StarSystem> StarSystems { get; private set; }

        internal readonly Dictionary<Guid, EntityManager> GlobalGuidDictionary = new JDictionary<Guid, EntityManager>();
        internal readonly ReaderWriterLockSlim GuidDictionaryLock = new ReaderWriterLockSlim();

        #endregion

        #region Events
        /// <summary>
        /// PostLoad event fired when the game is loaded.
        /// Event is cleared each load.
        /// </summary>
        internal event EventHandler PostLoad;
        #endregion

        internal Game()
        {

            IsLoaded = false;
            _globalManager = new EntityManager(this);
            StarSystems = new List<StarSystem>();
            NextSubpulse = new SubpulseLimit();
            GalaxyGen = new GalaxyFactory(true);

            PostLoad += (sender, args) => { InitializeProcessors(); };
        }

        [PublicAPI]
        public Game(string gameName, int numSystems)
            : this()
        {
            GameName = gameName;
            for (int i = 0; i < numSystems; i++)
            {
                StarSystem newSystem = GalaxyGen.StarSystemFactory.CreateSystem(this, "System #" + i);
                StarSystems.Add(newSystem);
            }

            PostGameLoad();
        }

        #region Functions
        internal void RunProcessors(List<StarSystem> systems, int deltaSeconds)
        {
            OrbitProcessor.Process(this, systems, deltaSeconds);
            InstallationProcessor.Process(systems, deltaSeconds);
        }

        /// <summary>
        /// Prepares, and defines the order that processors are run in.
        /// </summary>
        private static void InitializeProcessors()
        {
            OrbitProcessor.Initialize();
            InstallationProcessor.Initialize();
        }

        /// <summary>
        /// Time advancement code. Attempts to advance time by the number of seconds
        /// passed to it.
        /// 
        /// Interrupts may prevent the entire requested timeframe from being advanced.
        /// </summary>
        /// <param name="deltaSeconds">Time Advance Requested</param>
        /// <param name="progress">IProgress implementation to report progress.</param>
        /// <returns>Total Time Advanced</returns>
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
            deltaSeconds = deltaSeconds - (deltaSeconds % GameSettings.GameConstants.MinimumTimestep);
            if (deltaSeconds == 0)
            {
                deltaSeconds = GameSettings.GameConstants.MinimumTimestep;
            }

            // Clear any interrupt flag before starting the pulse.
            CurrentInterrupt = null;

            while (CurrentInterrupt == null && deltaSeconds > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int subpulseTime = Math.Min(NextSubpulse.MaxSeconds, deltaSeconds);
                // Set next subpulse to max value. If it needs to be shortened, it will
                // be shortened in the pulse execution.
                NextSubpulse.MaxSeconds = int.MaxValue;

                // Update our date.
                CurrentDateTime += TimeSpan.FromSeconds(subpulseTime);

                // Execute all processors. Magic happens here.
                RunProcessors(StarSystems, deltaSeconds);

                // Update our remaining values.
                deltaSeconds -= subpulseTime;
                timeAdvanced += subpulseTime;
                if (progress != null)
                {
                    progress.Report((double)timeAdvanced / deltaSeconds);
                }
            }

            if (CurrentInterrupt != null)
            {
                // Gamelog?
            }
            return timeAdvanced;
        }

        internal void PostGameLoad()
        {
            // Invoke the Post Load event down the chain.
            if (PostLoad != null)
                PostLoad(this, EventArgs.Empty);

            // set isLoaded to true:
            IsLoaded = true;

            // Post load event completed. Drop all handlers.
            PostLoad = null;

        }
        #endregion
    }
}
