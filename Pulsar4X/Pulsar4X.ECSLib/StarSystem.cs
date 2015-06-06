using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

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

        public StarSystem(string name, int seed)
        {
            SystemManager = new EntityManager();
            Neighbors = new List<StarSystem>();
            NameDB = new NameDB(Entity.InvalidEntity, name);
            Seed = seed;
            RNG = new Random(seed);
            EconLastTickRun = 0;
        }
    }
}
