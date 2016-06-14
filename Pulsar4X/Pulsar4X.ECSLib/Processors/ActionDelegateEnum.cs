using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Pulsar4X.ECSLib
{
    public enum SystemActionEnum
    {
        JumpOutProcessor,
        JumpInProcessor,
        EconProcessor,
        OrbitProcessor, 
        SomeOtherProcessor
    }

    internal static class ActionDelegateDictionary
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

        internal static Dictionary<SystemActionEnum, Delegate> EnumProcessorMap = new Dictionary<SystemActionEnum, Delegate>
        {
            { SystemActionEnum.JumpOutProcessor, new Action<StarSystem>(processor => { IntraSystemJumpProcessor.JumpOut(_game, _jumpPair) ;}) },
            { SystemActionEnum.JumpInProcessor, new Action<StarSystem>(processor => { IntraSystemJumpProcessor.JumpIn(_game, _jumpPair) ;}) },
            { SystemActionEnum.EconProcessor, new Action<StarSystem>(processor => { EconProcessor.ProcessSystem(_currentSystem);}) },
            { SystemActionEnum.OrbitProcessor, new Action<StarSystem>(processor => { OrbitProcessor.UpdateSystemOrbits(_currentSystem);}) },
            //{ SystemActionEnum.SomeOtherProcessor, new Action<StarSystem>(processor => { Something.SomeOtherProcess(CurrentEntity);}) },
        };
        internal static void DoAction(SystemActionEnum action, StarSystem starSystem, Entity entity)
        {
            //lock (_currentSystem)
            //{
                _currentSystem = starSystem;
                _currentEntity = entity;
                
                EnumProcessorMap[action].DynamicInvoke(entity);
            //}
        }
        internal static void DoAction(SystemActionEnum action, StarSystem starSystem)
        {
            //lock (_currentSystem)
            //{
                _currentSystem = starSystem;

                EnumProcessorMap[action].DynamicInvoke(starSystem);
            //}
        }

        internal static void DoAction(SystemActionEnum action, Game game, SystemEntityJumpPair jumpPair)
        {
            _jumpPair = jumpPair;
            _game = game;

            EnumProcessorMap[action].DynamicInvoke(game, jumpPair);            
        }
    }

    
}
