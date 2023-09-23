using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Engine
{
    [DebuggerDisplay("{NameDB.DefaultName} - {Guid.ToString()}")]
    [JsonObject(MemberSerialization.OptIn)]
    public class StarSystem : EntityManager, ISerializable
    {
        private readonly Random RNG;

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
        internal StarSystem()
        {
        }

        public StarSystem(Game game, string name) : base(game, false)
        {
            NameDB = new NameDB(name);

            var R = new Random();
            Seed = R.Next(int.MaxValue - 1);        // Find a random integer for the seed so can recreate if needed
            RNG = new Random(Seed);
            game.Systems.Add(Guid, this);
        }

        public StarSystem(Game game, string name, int seed) : base(game, false)
        {
            NameDB = new NameDB(name);

            Seed = seed;
            RNG = new Random(seed);
            game.Systems.Add(Guid, this);
        }

        internal StarSystem(Game game, string name, int seed, string systemID): base(game, false)
        {
            NameDB = new NameDB(name);

            Seed = seed;
            RNG = new Random(seed);
            ManagerGuid = systemID;
            game.Systems.Add(Guid, this);
        }

        public StarSystem(SerializationInfo info, StreamingContext context) : base(info, context)
        {

            ManagerGuid = (string)info.GetValue("ID", typeof(string));
            Seed = (int)info.GetValue("Seed", typeof(int));
            NameDB = (NameDB)info.GetValue("Name", typeof(NameDB));
        }

        public void ExportBodies(SerializationInfo info)
        {
            List<Entity> bodies = this.GetAllEntitiesWithDataBlob<StarInfoDB>();
            bodies.AddRange(this.GetAllEntitiesWithDataBlob<SystemBodyInfoDB>());

            info.AddValue("ID", Guid);
            info.AddValue("Seed", Seed);
            info.AddValue("Name", NameDB);
            info.AddValue("Bodies", bodies);
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {

            info.AddValue("ID", Guid);
            info.AddValue("Seed", Seed);
            info.AddValue("Name", NameDB);
            base.GetObjectData(info, context);
        }


        [OnDeserialized]
        public void OnDeserialized(StreamingContext context)
        {
            Game = (Game)context.Context;
            Game.PostLoad += GameOnPostLoad;

        }

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
