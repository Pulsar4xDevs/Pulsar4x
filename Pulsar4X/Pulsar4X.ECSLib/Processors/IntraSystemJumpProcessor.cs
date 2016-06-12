using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    internal static class IntraSystemJumpProcessor
    {
        internal static void JumpOut(Game game, SystemEntityJumpPair jumpPair)
        {
            jumpPair.JumpSystem.SystemManager.RemoveEntity(jumpPair.JumpingEntity);
            game.GlobalManager.SetupEntity(jumpPair.JumpingEntity);

        }
        internal static void JumpIn(Game game, SystemEntityJumpPair jumpPair)
        {
            game.GlobalManager.RemoveEntity(jumpPair.JumpingEntity);
            jumpPair.JumpSystem.SystemManager.SetupEntity(jumpPair.JumpingEntity);            
        }

        internal static void SetJump(Game game, StarSystem exitSystem, DateTime exitTime, StarSystem entrySystem, DateTime entryTime, Entity jumpingEntity)
        {
            SystemEntityJumpPair exitPair = new SystemEntityJumpPair
            {
                JumpSystem = exitSystem,
                JumpingEntity = jumpingEntity
            };
            game.GameLoop.AddSystemInteractionInterupt(exitTime, JumpOut, exitPair);

            SystemEntityJumpPair entryPair = new SystemEntityJumpPair
            {
                JumpSystem = entrySystem,
                JumpingEntity = jumpingEntity
            };
            game.GameLoop.AddSystemInteractionInterupt(entryTime, JumpIn, entryPair);
        }

    }

    internal struct SystemEntityJumpPair
    {
        internal StarSystem JumpSystem;
        internal Entity JumpingEntity;
    } 
}
