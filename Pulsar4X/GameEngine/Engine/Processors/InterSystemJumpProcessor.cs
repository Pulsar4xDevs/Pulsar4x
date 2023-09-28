using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Engine
{
    public static class InterSystemJumpProcessor
    {
        //TODO look at turning the entity into a ProtoEntity instead of shifting it to the GlobalManager
        internal static void JumpOut(Game game, SystemEntityJumpPair jumpPair)
        {
            jumpPair.JumpingEntity.Transfer(game.GlobalManager);


        }
        internal static void JumpIn(Game game, SystemEntityJumpPair jumpPair)
        {
            jumpPair.JumpingEntity.Transfer(jumpPair.JumpSystem);
        }

        public static void SetJump(Game game, DateTime exitTime, StarSystem entrySystem, DateTime entryTime, Entity jumpingEntity)
        {
            SystemEntityJumpPair jumpPair = new SystemEntityJumpPair
            {
                JumpSystem = entrySystem,
                JumpingEntity = jumpingEntity
            };
            game.TimePulse.AddSystemInteractionInterupt(exitTime, PulseActionEnum.JumpOutProcessor, jumpPair);

            game.TimePulse.AddSystemInteractionInterupt(entryTime, PulseActionEnum.JumpInProcessor, jumpPair);
        }

    }
}
