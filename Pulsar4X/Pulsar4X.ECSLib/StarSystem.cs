using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    [JsonObject(MemberSerialization.OptIn)]
    public class StarSystem
    {
        [PublicAPI]
        public int Seed
        {
            get { return _seed; }
        }
        [JsonProperty("Seed")]
        private readonly int _seed;

        public NameDB NameDB
        {
            get { return _nameDB; }
        }
        [JsonProperty("NameDB")]
        private readonly NameDB _nameDB;

        public Random RNG { get; private set; }

        public int EconLastTickRun { get; set; }

        public EntityManager SystemManager
        {
            get { return _systemManager; }
        }
        [JsonProperty("SystemManager")]
        private readonly EntityManager _systemManager;


        public List<StarSystem> Neighbors
        {
            get { return _neighbors; }
        }
        [JsonProperty("Neighbors")]
        private readonly List<StarSystem> _neighbors;

        public StarSystem(Game game, string name, int seed)
        {
            _systemManager = new EntityManager(game);
            _neighbors = new List<StarSystem>();
            _nameDB = new NameDB(name);
            _seed = seed;
            RNG = new Random(seed);
            EconLastTickRun = 0;
        }
    }
}
