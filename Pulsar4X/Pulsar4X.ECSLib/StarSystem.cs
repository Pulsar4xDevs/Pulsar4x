using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class StarSystem
    {
        public EntityManager SystemManager { get; private set; }

        public List<StarSystem> Neighbors { get; private set; }

        public NameDB NameDB { get; private set; }

        public int Seed { get; private set; }

        public Random RNG { get; private set; }

        public int EconLastTickRun { get; set; }

        public StarSystem(Game game, string name, int seed)
        {
            SystemManager = new EntityManager(game);
            Neighbors = new List<StarSystem>();
            NameDB = new NameDB(Entity.InvalidEntity, name);
            Seed = seed;
            RNG = new Random(seed);
            EconLastTickRun = 0;
        }
    }
}
