using Newtonsoft.Json;
using System;
using System.Diagnostics;
using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;

namespace Pulsar4X.Engine
{
    [DebuggerDisplay("{NameDB.DefaultName} - {Guid.ToString()}")]
    [JsonObject(MemberSerialization.OptIn)]
    public class StarSystem : EntityManager
    {
        private Random RNG;

        [PublicAPI]
        public string Guid
        {
            get
            {
                return ManagerGuid;
            }
        }

        [JsonProperty]
        internal int SystemIndex { get; set; }

        [PublicAPI]
        [JsonProperty]
        public NameDB NameDB { get;  set; }

        //[PublicAPI]
        //public EntityManager SystemManager { get { return this; } }

        [PublicAPI]
        [JsonProperty]
        public int Seed { get;  set; }

        [PublicAPI]
        public int RNGNext(int min, int max)
        {
            var next = RNG.Next(min, max);
            return next;
        }

        [PublicAPI]
        public double RNGNextDouble()
        {
            var next = RNG.NextDouble();
            return next;
        }

        public bool RNGNexBool(float chance)
        {
            return RNG.NextDouble() < chance;
        }
        public bool RNGNexBool(double chance)
        {
            return RNG.NextDouble() < chance;
        }

        [JsonConstructor]
        public StarSystem()
        {
        }

        public void Initialize(Game game, string name, int seed = -1, string systemID = "")
        {
            base.Initialize(game);

            NameDB = new NameDB(name);

            if(seed == -1)
            {
                var random = new Random();
                Seed = random.Next(int.MaxValue - 1);
            }
            else
            {
                Seed = seed;
            }

            RNG = new Random(seed);

            if(systemID.IsNotNullOrEmpty())
                ManagerGuid = systemID;

            game.Systems.Add(Guid, this);
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

        private void GameOnPostLoad(object sender, EventArgs eventArgs)
        {
            if (!Game.Systems.ContainsKey(Guid))
            {
                Game.Systems.Add(Guid, this);
                if(Game.GameMasterFaction != null) //clients wont have a GameMaster
                    Game.GameMasterFaction.GetDataBlob<FactionInfoDB>().KnownSystems.Add(Guid);
            }
        }
    }
}
