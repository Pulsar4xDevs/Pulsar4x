using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Pulsar4X.ECSLib.Processors
{
    public delegate void ProcessFunction(StarSystem system, int deltaSeconds);

    public class PhaseState
    {
        public int DeltaSeconds;
        public AutoResetEvent WaitHandle;
        public List<ProcessFunction> ProcessFunctions;

        public PhaseState(int deltaSeconds, AutoResetEvent waitHandle, List<ProcessFunction> processFunctions)
        {
            DeltaSeconds = deltaSeconds;
            WaitHandle = waitHandle;
            ProcessFunctions = processFunctions;
        }
    }

    static class PhaseProcessor
    {
        private static List<List<ProcessFunction>> m_phases;

        internal static void Initialize()
        {
            m_phases = new List<List<ProcessFunction>>();

            List<ProcessFunction> movementPhase = new List<ProcessFunction>();
            movementPhase.Add(OrbitProcessor.Process);

            m_phases.Add(movementPhase);
        }

        internal static void Process(int subpulseTime)
        {
            foreach (List<ProcessFunction> phaseProcessors in m_phases)
            {
                List<AutoResetEvent> waitHandles = new List<AutoResetEvent>();
                foreach (StarSystem system in Game.Instance.StarSystems)
                {
                    AutoResetEvent systemWaitHandle = new AutoResetEvent(false);
                    waitHandles.Add(systemWaitHandle);
                    PhaseState stateInfo = new PhaseState(subpulseTime, systemWaitHandle, phaseProcessors);

                    ThreadPool.QueueUserWorkItem(system.ProcessPhase, stateInfo);
                }

                AutoResetEvent.WaitAll(waitHandles.ToArray());
            }
        }
    }
}
