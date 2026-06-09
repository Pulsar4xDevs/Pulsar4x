using Newtonsoft.Json;
using System;
using System.Diagnostics;
using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;
using Pulsar4X.Names;

namespace Pulsar4X.Engine
{
    public enum SystemActivityState
    {
        Stasis,      // No processing — system time falls behind
        Background,  // Throttled hotloop processors
        Foreground   // Normal processing (current behavior)
    }

    [DebuggerDisplay("{NameDB.DefaultName} - {ID.ToString()}")]
    [JsonObject(MemberSerialization.OptIn)]
    public class StarSystem : EntityManager
    {
        [PublicAPI]
        public string ID => ManagerID;

        [JsonIgnore]
        public SystemActivityState ActivityState { get; private set; } = SystemActivityState.Stasis;

        // TODO: Find better names for this.
        /// <summary>
        /// Number of external observers (e.g. clients) currently observing this system.
        /// 
        /// If this value is greater than 0, the system needs to be simulated in at least <see cref="SystemActivityState.Background"/> mode.
        /// </summary>
        [JsonIgnore]
        private int _externalObservers = 0;

        /// <summary>
        /// Number of external observers (e.g. clients) currently observing this system with priority.
        /// 
        /// If this value is greater than 0, the system needs to be simulated in <see cref="SystemActivityState.Foreground"/> mode.
        /// </summary>
        [JsonIgnore]
        private int _externalPriorityObservers = 0;

        // TODO: Possibly rework this to atomic if it causes perf issues.
        [JsonIgnore]
        private readonly object _observerLock = new();

        [JsonProperty]
        internal int SystemIndex { get; set; }

        [PublicAPI]
        [JsonProperty]
        public NameDB NameDB { get; set; }

        //[PublicAPI]
        //public EntityManager SystemManager { get { return this; } }

        [JsonConstructor]
        public StarSystem()
        {
        }

        public void Initialize(Game game, string name, int seed = -1, bool postLoad = false, string systemID = "")
        {
            base.Initialize(game, seed, postLoad);

            NameDB = new NameDB(name);

            if (systemID.IsNotNullOrEmpty())
                ManagerID = systemID;

            game.Systems.Add(this);
        }

        // public StarSystem(SerializationInfo info, StreamingContext context) : base(info, context)
        // {

        //     ManagerGuid = (string)info.GetValue("ID", typeof(string));
        //     Seed = (int)info.GetValue("Seed", typeof(int));
        //     NameDB = (NameDB)info.GetValue("Name", typeof(NameDB));
        // }

        // public void ExportBodies(SerializationInfo info)
        // {
        //     List<Entity> bodies = this.GetAllEntitiesWithDataBlob<StarInfoDB>();
        //     bodies.AddRange(this.GetAllEntitiesWithDataBlob<SystemBodyInfoDB>());

        //     info.AddValue("ID", Guid);
        //     info.AddValue("Seed", Seed);
        //     info.AddValue("Name", NameDB);
        //     info.AddValue("Bodies", bodies);
        // }

        // public override void GetObjectData(SerializationInfo info, StreamingContext context)
        // {

        //     info.AddValue("ID", Guid);
        //     info.AddValue("Seed", Seed);
        //     info.AddValue("Name", NameDB);
        //     base.GetObjectData(info, context);
        // }


        // [OnDeserialized]
        // public void OnDeserialized(StreamingContext context)
        // {
        //     Game = (Game)context.Context;
        //     Game.PostLoad += GameOnPostLoad;

        // }

        [Obsolete("Avoid setting the state directly. Use external observers.")]
        public void SetActivityState(SystemActivityState newState) => SetActivityStateInternal(newState);

        internal void SetActivityStateInternal(SystemActivityState newState)
        {
            var oldState = ActivityState;
            ActivityState = newState;

            if (oldState == SystemActivityState.Stasis && newState != SystemActivityState.Stasis)
            {
                CatchUpFromStasis(Game.TimePulse.GameGlobalDateTime);
            }

            switch (newState)
            {
                case SystemActivityState.Foreground:
                    ManagerSubpulses.FrequencyMultiplier = 1.0;
                    break;
                case SystemActivityState.Background:
                    ManagerSubpulses.FrequencyMultiplier = 10.0;
                    break;
                case SystemActivityState.Stasis:
                    ManagerSubpulses.FrequencyMultiplier = 1.0;
                    break;
            }
        }

        // TODO: Introduce wrappers for these methods.
        /// <summary>
        /// Increments the count of external observers monitoring this system.
        /// 
        /// If priority is true, this observer requires the system to be in Foreground mode; otherwise, Background mode is sufficient.
        /// </summary>
        /// <param name="priority"></param>
        public void IncrementExternalObserver(bool priority = false)
        {
            lock (_observerLock)
            {
                _externalObservers++;

                if (priority)
                    PromoteExternalObserverInternal(false);

                // TODO: Defer update to the game engine thread.
                UpdateActivityState();
            }
        }

        /// <summary>
        /// Decrements the count of external observers monitoring this system.
        /// 
        /// If priority is true, the observer is assumed to have required Foreground mode; otherwise, Background mode is assumed.
        /// </summary>
        /// <param name="priority"></param>
        public void DecrementExternalObserver(bool priority = false)
        {
            lock (_observerLock)
            {
                if (priority)
                    DemoteExternalObserverInternal(false);

                Debug.Assert(_externalObservers > 0, "External observers cannot be negative.");
                _externalObservers--;

                // TODO: Defer update to the game engine thread.
                UpdateActivityState();
            }
        }

        public void PromoteExternalObserver() => PromoteExternalObserverInternal(true);

        public void DemoteExternalObserver() => DemoteExternalObserverInternal(true);

        internal void PromoteExternalObserverInternal(bool doUpdate)
        {
            lock (_observerLock)
            {
                Debug.Assert(_externalPriorityObservers < _externalObservers, "Can not have more priority observers than total observers.");
                _externalPriorityObservers++;
                if (doUpdate)
                    UpdateActivityState();
            }
        }

        internal void DemoteExternalObserverInternal(bool doUpdate)
        {
            lock (_observerLock)
            {
                Debug.Assert(_externalPriorityObservers > 0, "External priority observers cannot be negative.");
                _externalPriorityObservers--;
                if (doUpdate)
                    UpdateActivityState();
            }
        }

        /// <summary>
        /// Updates the activity state of the system respecting observer rules.
        /// </summary>
        internal void UpdateActivityState()
        {
            var oldState = ActivityState;
            var newState = SystemActivityState.Stasis;

            // TODO: Optimize critical sections.
            lock (_observerLock)
            {
                if (_externalPriorityObservers > 0)
                {
                    newState = SystemActivityState.Foreground;
                }
                else if (_externalObservers > 0)
                {
                    newState = SystemActivityState.Background;
                }
                else if (HasFactionEntities())
                {
                    newState = SystemActivityState.Background;
                }

                if (oldState != newState)
                {
                    SetActivityStateInternal(newState);
                }
            }
        }

        internal void CatchUpFromStasis(DateTime targetDateTime)
        {
            if (ManagerSubpulses.StarSysDateTime >= targetDateTime)
                return;

            var orbitProcessor = Game.ProcessorManager.GetProcessor<Orbits.OrbitDB>();
            orbitProcessor.ProcessManager(this, (int)(targetDateTime - ManagerSubpulses.StarSysDateTime).TotalSeconds);

            ManagerSubpulses.FastForwardTo(targetDateTime);
        }

        private void GameOnPostLoad(object sender, EventArgs eventArgs)
        {
            // if (!Game.Systems.ContainsKey(Guid))
            // {
            //     Game.Systems.Add(Guid, this);
            //     if(Game.GameMasterFaction != null) //clients wont have a GameMaster
            //         Game.GameMasterFaction.GetDataBlob<FactionInfoDB>().KnownSystems.Add(Guid);
            // }
        }
    }
}
