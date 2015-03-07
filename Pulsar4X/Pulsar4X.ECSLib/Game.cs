using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib.DataBlobs;

namespace Pulsar4X.ECSLib
{
    public class Game
    {
        public EntityManager GlobalManager { get { return m_globalManager; } }
        private EntityManager m_globalManager;

        public static Game Instance { get { return m_instance; } }
        private static Game m_instance;

        public DateTime CurrentDateTime { get; set; }

        public Game()
        {
            m_globalManager = new EntityManager();

            m_instance = this;
        }
    }
}
