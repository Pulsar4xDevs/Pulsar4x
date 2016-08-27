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
        /// <param name="starSystem">the system to process</param>
        internal static void Process(StarSystem starSystem)
        {
            EntityManager currentManager = starSystem.SystemManager;
            TimeSpan orbitCycle = starSystem.Game.Settings.OrbitCycleTime;
            DateTime toDate = starSystem.SystemSubpulses.SystemLocalDateTime + orbitCycle;

            starSystem.SystemSubpulses.AddSystemInterupt(toDate + orbitCycle, PulseActionEnum.BalisticMoveProcessor);

            foreach (Entity objectEntity in starSystem.SystemManager.GetAllEntitiesWithDataBlob<NewtonBalisticDB>())
            {
                NewtonBalisticDB balisticDB = objectEntity.GetDataBlob<NewtonBalisticDB>();
                PositionDB position = objectEntity.GetDataBlob<PositionDB>();
                position.RelativePosition += Distance.KmToAU(balisticDB.CurrentSpeed * orbitCycle.TotalSeconds);
            }
        }
    }
}
