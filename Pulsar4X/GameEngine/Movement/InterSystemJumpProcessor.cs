using System;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;

namespace Pulsar4X.Movement
{
    public static class InterSystemJumpProcessor
    {
        //TODO look at turning the entity into a ProtoEntity instead of shifting it to the GlobalManager
        internal static void JumpOut(Game game, SystemEntityJumpPair jumpPair)
        {
            game.GlobalManager.Transfer(jumpPair.JumpingEntity);
        }
        internal static void JumpIn(Game game, SystemEntityJumpPair jumpPair)
        {
            if (jumpPair.JumpSystem.ActivityState == SystemActivityState.Stasis)
                jumpPair.JumpSystem.SetActivityState(SystemActivityState.Background);

            jumpPair.JumpSystem.Transfer(jumpPair.JumpingEntity);
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
