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
        EntityManager SystemManager { get { return m_systemManager; } }
        private EntityManager m_systemManager;

        List<StarSystem> Neighbors { get { return m_neighbors; } }
        private List<StarSystem> m_neighbors;

        public AutoResetEvent updateComplete;

        public StarSystem()
        {
            m_systemManager = new EntityManager();
            m_neighbors = new List<StarSystem>();

            updateComplete = new AutoResetEvent(false);
            Game.Instance.SystemWaitHandles.Add(updateComplete);
        }

        public void Update(object objDeltaSeconds)
        {
            int deltaSeconds = (int)objDeltaSeconds;

            OrbitProcessor.Process(m_systemManager);

            updateComplete.Set();
        }
    }
}
