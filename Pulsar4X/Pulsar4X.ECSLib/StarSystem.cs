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

        public NameDB Names { get; private set; }

        public int Seed { get; private set; }

        public Random RNG { get; private set; }

        public StarSystem(string name, int seed)
        {
            SystemManager = new EntityManager();
            Neighbors = new List<StarSystem>();
            Names = new NameDB(Entity.GetInvalidEntity(), name);
            Seed = seed;
            RNG = new Random(seed);
        }
    }
}
