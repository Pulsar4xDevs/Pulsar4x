using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    internal static class NewtonBalisticProcessor
    {

        /// <summary>
        /// process balistic movement for a single system
        /// currently is not affected by gravity. 
        /// </summary>
        /// <param name="manager">the system to process</param>
        internal static void Process(EntityManager manager)
        {
            TimeSpan orbitCycle = manager.Game.Settings.OrbitCycleTime;
            DateTime toDate = manager.ManagerSubpulses.SystemLocalDateTime + orbitCycle;

            manager.ManagerSubpulses.AddSystemInterupt(toDate + orbitCycle, PulseActionEnum.BalisticMoveProcessor);

            foreach (Entity objectEntity in manager.GetAllEntitiesWithDataBlob<NewtonBalisticDB>())
            {
                NewtonBalisticDB balisticDB = objectEntity.GetDataBlob<NewtonBalisticDB>();
                PositionDB position = objectEntity.GetDataBlob<PositionDB>();
                position.RelativePosition += Distance.KmToAU(balisticDB.CurrentSpeed * orbitCycle.TotalSeconds);
            }
        }
    }
}
