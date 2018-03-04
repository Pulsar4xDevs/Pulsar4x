using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    [DebuggerDisplay("{NameDB.DefaultName} - {Guid.ToString()}")]
    [JsonObject(MemberSerialization.OptIn)]
    public class StarSystem : EntityManager
    {


        [PublicAPI]
        [JsonProperty]
        public Guid Guid { get;  set; }

        [JsonProperty]
        internal int SystemIndex { get; set; }

        [PublicAPI]
        [JsonProperty]
        public NameDB NameDB { get;  set; }

        [PublicAPI]
        public EntityManager SystemManager { get { return this; } }

        [PublicAPI]
        [JsonProperty]
        public int Seed { get;  set; }

        internal Random RNG { get; private set; }

        [JsonConstructor]
        internal StarSystem()
        {
        }

        public StarSystem(Game game, string name, int seed) : base(game, false)
        {
            Guid = Guid.NewGuid();
            NameDB = new NameDB(name);

            Seed = seed;
            RNG = new Random(seed);
            game.Systems.Add(Guid, this);
        }


        public StarSystem(SerializationInfo info, StreamingContext context) : base(info, context)
        {

            Guid = (Guid)info.GetValue("Guid", typeof(Guid));
            Seed = (int)info.GetValue("Seed", typeof(int));
        }


        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Guid", Guid);
            info.AddValue("Seed", Seed);
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
