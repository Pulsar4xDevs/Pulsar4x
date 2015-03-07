using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.ECSLib
{
    public class StarSystem
    {
        EntityManager SystemManager { get { return m_systemManager; } }
        private EntityManager m_systemManager;

        List<StarSystem> Neighbors { get { return m_neighbors; } }
        private List<StarSystem> m_neighbors;

        public StarSystem()
        {
            m_systemManager = new EntityManager();
            m_neighbors = new List<StarSystem>();
        }


    }
}
