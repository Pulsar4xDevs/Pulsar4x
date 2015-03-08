using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Pulsar4X.ECSLib.Processors
{
    public delegate void ProcessFunction(StarSystem system, int deltaSeconds);

    /// <summary>
    /// Class to contain all information necessary for StarSystems to execute a phase.
    /// </summary>
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

    /// <summary>
    /// Top-Level processor for time phase management.
    /// </summary>
    static class PhaseProcessor
    {
        private static List<List<ProcessFunction>> m_phases;

        /// <summary>
        /// Sets up the structure of the phases.
        /// 
        /// Each phase is a list of functions to be called.
        /// functions will be called in order in their phase, and the entire
        /// universe will sync after a phase is complete.
        /// </summary>
        internal static void Initialize()
        {
            m_phases = new List<List<ProcessFunction>>();

            List<ProcessFunction> movementPhase = new List<ProcessFunction>();
            movementPhase.Add(OrbitProcessor.Process);

            m_phases.Add(movementPhase);
        }

        /// <summary>
        /// Executes the Subpulse.
        /// 
        /// Subpulse is broken up into "Phases" each phase contains a list of Processor.Process
        /// functions to be called on that phase.
        /// 
        /// To execute a phase, we create a ThreadPool task for each StarSystem, passing the StarSystem
        /// the list of functions to execute on that phase. Once StarSystem completes calling each function,
        /// StarSystem sets the WaitHandle passed to it.
        /// 
        /// Once all StarSystems have set their WaitHandles the phase, the process is repeated for the next
        /// phase until no phases remain.
        /// 
        /// Once no phases remain, the subpulse is complete, and we return to the calling function.
        /// </summary>
        /// <param name="subpulseTime">Time that this pulse spans.</param>
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
