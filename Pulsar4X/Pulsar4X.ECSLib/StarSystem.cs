using Newtonsoft.Json;
using System;

namespace Pulsar4X.ECSLib
{
    [JsonObject(MemberSerialization.OptIn)]
    public class StarSystem
    {
        [PublicAPI]
        [JsonProperty]
        public Guid Guid { get; private set; }

        [PublicAPI]
        [JsonProperty]
        public NameDB NameDB { get; private set; }

        [PublicAPI]
        [JsonProperty]
        public int EconLastTickRun { get; internal set; }

        [PublicAPI]
        [JsonProperty]
        public EntityManager SystemManager { get; private set; }

        [PublicAPI]
        [JsonProperty]
        public int Seed { get; private set; }
        internal Random RNG { get; private set; }

        [JsonConstructor]
        private StarSystem()
        {
        }

        public StarSystem(Game game, string name, int seed)
        {
            Guid = Guid.NewGuid();
            NameDB = new NameDB(name);
            EconLastTickRun = 0;
            SystemManager = new EntityManager(game);
            Seed = seed;
            RNG = new Random(seed);

            game.StarSystems.Add(Guid, this);
        }
    }
}
