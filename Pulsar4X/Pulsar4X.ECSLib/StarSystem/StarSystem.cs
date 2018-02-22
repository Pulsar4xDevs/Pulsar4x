using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Pulsar4X.ECSLib
{
    [DebuggerDisplay("{NameDB.DefaultName} - {Guid.ToString()}")]
    [JsonObject(MemberSerialization.OptIn)]
    public class StarSystem : EntityManager
    {


        [PublicAPI]
        [JsonProperty]
        public Guid Guid { get; private set; }

        [JsonProperty]
        internal int SystemIndex { get; set; }

        [PublicAPI]
        [JsonProperty]
        public NameDB NameDB { get; private set; }

        [PublicAPI]
        [JsonProperty]
        public EntityManager SystemManager { get { return this; } }

        [PublicAPI]
        [JsonProperty]
        public int Seed { get; private set; }
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
