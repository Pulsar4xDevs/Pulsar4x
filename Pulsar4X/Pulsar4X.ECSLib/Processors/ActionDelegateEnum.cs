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
        SomeOtherProcessor
    }

    internal static class PulseActionDictionary
    {
        #warning this possibly could be a crossplatform compatibility problem. 
        [ThreadStatic]//if this does what I think it does... mind = blown. need to write tests 
        private static Entity _currentEntity;
        [ThreadStatic]
        private static StarSystem _currentSystem;
        [ThreadStatic]
        private static Game _game;
        [ThreadStatic]
        private static SystemEntityJumpPair _jumpPair;

        internal static Dictionary<PulseActionEnum, Delegate> EnumProcessorMap = new Dictionary<PulseActionEnum, Delegate>
        {
            { PulseActionEnum.JumpOutProcessor, new Action<StarSystem>(processor => { IntraSystemJumpProcessor.JumpOut(_game, _jumpPair) ;}) },
            { PulseActionEnum.JumpInProcessor, new Action<StarSystem>(processor => { IntraSystemJumpProcessor.JumpIn(_game, _jumpPair) ;}) },
            { PulseActionEnum.EconProcessor, new Action<StarSystem>(processor => { EconProcessor.ProcessSystem(_currentSystem);}) },
            { PulseActionEnum.OrbitProcessor, new Action<StarSystem>(processor => { OrbitProcessor.UpdateSystemOrbits(_currentSystem);}) },
            //{ SystemActionEnum.SomeOtherProcessor, new Action<StarSystem>(processor => { Something.SomeOtherProcess(CurrentEntity);}) },
        };
        internal static void DoAction(PulseActionEnum action, StarSystem starSystem, Entity entity)
        {
            //lock (_currentSystem)
            //{
                _currentSystem = starSystem;
                _currentEntity = entity;
                
                EnumProcessorMap[action].DynamicInvoke(entity);
            //}
        }
        internal static void DoAction(PulseActionEnum action, StarSystem starSystem)
        {
            //lock (_currentSystem)
            //{
                _currentSystem = starSystem;

                EnumProcessorMap[action].DynamicInvoke(starSystem);
            //}
        }

        internal static void DoAction(PulseActionEnum action, Game game, SystemEntityJumpPair jumpPair)
        {
            _jumpPair = jumpPair;
            _game = game;

            EnumProcessorMap[action].DynamicInvoke(game, jumpPair);            
        }
    }

    
}
