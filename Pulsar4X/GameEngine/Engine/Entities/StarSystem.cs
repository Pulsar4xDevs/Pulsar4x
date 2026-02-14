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
        public string ID
        {
            get
            {
                return ManagerID;
            }
        }

        [JsonProperty]
        public SystemActivityState ActivityState { get; set; } = SystemActivityState.Stasis;

        [JsonProperty]
        internal int SystemIndex { get; set; }

        [PublicAPI]
        [JsonProperty]
        public NameDB NameDB { get;  set; }

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
            
            if(systemID.IsNotNullOrEmpty())
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

        public void SetActivityState(SystemActivityState newState)
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
