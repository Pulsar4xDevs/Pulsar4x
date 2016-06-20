using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Pulsar4X.ECSLib
{
    public enum PulseActionEnum
    {
        JumpOutProcessor,
        JumpInProcessor,
        EconProcessor,
        OrbitProcessor,
        MoveOnlyProcessor, 
        SomeOtherProcessor
    }

    internal static class PulseActionDictionary
    {
        [ThreadStatic]
        private static Entity _currentEntity;
        [ThreadStatic]
        private static StarSystem _currentSystem;
        [ThreadStatic]
        private static Game _game;
        [ThreadStatic]
        private static SystemEntityJumpPair _jumpPair;

        internal static Dictionary<PulseActionEnum, Delegate> EnumProcessorMap = new Dictionary<PulseActionEnum, Delegate>
        {
            { PulseActionEnum.JumpOutProcessor, new Action<StarSystem>(processor => { InterSystemJumpProcessor.JumpOut(_game, _jumpPair) ;}) },
            { PulseActionEnum.JumpInProcessor, new Action<StarSystem>(processor => { InterSystemJumpProcessor.JumpIn(_game, _jumpPair) ;}) },
            { PulseActionEnum.EconProcessor, new Action<StarSystem>(processor => { EconProcessor.ProcessSystem(_currentSystem);}) },
            { PulseActionEnum.OrbitProcessor, new Action<StarSystem>(processor => { OrbitProcessor.UpdateSystemOrbits(_currentSystem);}) },
            { PulseActionEnum.MoveOnlyProcessor, new Action<StarSystem>(processor => { DoNothing();}) }, //movement always runs on a subpulse prior to this. 
            //{ SystemActionEnum.SomeOtherProcessor, new Action<StarSystem>(processor => { Something.SomeOtherProcess(_currentSystem, _currentEntity);}) },
        };
        internal static void DoAction(PulseActionEnum action, StarSystem starSystem, Entity entity)
        {
            _currentSystem = starSystem;
            _currentEntity = entity;
                
            EnumProcessorMap[action].DynamicInvoke(entity);

        }
        internal static void DoAction(PulseActionEnum action, StarSystem starSystem)
        {

            _currentSystem = starSystem;
            EnumProcessorMap[action].DynamicInvoke(starSystem);

        }

        internal static void DoAction(PulseActionEnum action, Game game, SystemEntityJumpPair jumpPair)
        {
            _jumpPair = jumpPair;
            _game = game;

            EnumProcessorMap[action].DynamicInvoke(game, jumpPair);            
        }

        private static void DoNothing()
        { }
    }

    
}
