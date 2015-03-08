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
        public EntityManager SystemManager { get { return m_systemManager; } }
        private EntityManager m_systemManager;

        public List<StarSystem> Neighbors { get { return m_neighbors; } }
        private List<StarSystem> m_neighbors;

        public StarSystem()
        {
            m_systemManager = new EntityManager();
            m_neighbors = new List<StarSystem>();
        }

        internal void ProcessPhase(object state)
        {
            PhaseState phaseState = state as PhaseState;

            if (phaseState == null)
            {
                // This should never happen.
                // Possible causes: Bad Programming. Check your edits.
                throw new ArgumentNullException("state");
            }

            // Run each process function for this phase.
            foreach (ProcessFunction function in phaseState.ProcessFunctions)
            {
                function(this, phaseState.DeltaSeconds);
            }

            // Announce this system's completion of this phase.
            phaseState.WaitHandle.Set();
        }
    }
}
