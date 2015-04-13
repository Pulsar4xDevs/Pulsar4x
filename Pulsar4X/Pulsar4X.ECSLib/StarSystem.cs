using Pulsar4X.ECSLib.Processors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Pulsar4X.ECSLib
{
    public class StarSystem
    {
        public EntityManager SystemManager { get; private set; }

        public List<StarSystem> Neighbors { get; private set; }

        public StarSystem()
        {
            SystemManager = new EntityManager();
            Neighbors = new List<StarSystem>();
        }
    }
}
