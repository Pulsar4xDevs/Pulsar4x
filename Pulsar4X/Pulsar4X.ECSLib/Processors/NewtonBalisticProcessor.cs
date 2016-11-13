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

            List<Entity> RemoveList = new List<Entity>();
            List<StarSystem> RemoveSystem = new List<StarSystem>();

            foreach (Entity objectEntity in manager.GetAllEntitiesWithDataBlob<NewtonBalisticDB>())
            {
                NewtonBalisticDB balisticDB = objectEntity.GetDataBlob<NewtonBalisticDB>();
                PositionDB position = objectEntity.GetDataBlob<PositionDB>();
                position.RelativePosition += Distance.KmToAU(balisticDB.CurrentSpeed * orbitCycle.TotalSeconds);

                Entity myTarget = manager.GetLocalEntityByGuid(balisticDB.TargetGuid);
                PositionDB targetPos = myTarget.GetDataBlob<PositionDB>();

                if(targetPos.AbsolutePosition == position.AbsolutePosition)
                {
                    //do something in damage processor for asteroid hitting a planet?
                    DamageProcessor.OnTakingDamage(myTarget, 1000000); ///one. million. damage points.

                    StarSystem mySystem;
                    if (!manager.Game.Systems.TryGetValue(position.SystemGuid, out mySystem))
                        throw new GuidNotFoundException(position.SystemGuid);

                    RemoveList.Add(objectEntity);
                    RemoveSystem.Add(mySystem);

                    mySystem.SystemManager.RemoveEntity(objectEntity); //get rid of the asteroid
                }
            }

            /// <summary>
            /// Clean up the asteroids that have hit something and been put in the remove list.
            /// </summary>
            for(int removeIterator = 0; removeIterator < RemoveList.Count; removeIterator++)
            {
                RemoveSystem[removeIterator].SystemManager.RemoveEntity(RemoveList[removeIterator]);
            }

            /// <summary>
            /// This may not be necessary but clear these two lists.
            /// </summary>
            RemoveList.Clear();
            RemoveSystem.Clear();
        }
    }
}
