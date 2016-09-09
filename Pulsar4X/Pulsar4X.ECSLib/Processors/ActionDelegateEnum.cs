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
        OrderProcessor,
        BalisticMoveProcessor,
        MoveOnlyProcessor, 
        SomeOtherProcessor
    }

    internal static class PulseActionDictionary
    {
        [ThreadStatic]
        private static Entity _currentEntity;
        [ThreadStatic]
        private static EntityManager _currentManager;
        [ThreadStatic]
        private static Game _game;
        [ThreadStatic]
        private static SystemEntityJumpPair _jumpPair;

        internal static Dictionary<PulseActionEnum, Delegate> EnumProcessorMap = new Dictionary<PulseActionEnum, Delegate>
        {
            { PulseActionEnum.JumpOutProcessor, new Action<EntityManager>(processor => { InterSystemJumpProcessor.JumpOut(_game, _jumpPair) ;}) },
            { PulseActionEnum.JumpInProcessor, new Action<EntityManager>(processor => { InterSystemJumpProcessor.JumpIn(_game, _jumpPair) ;}) },
            { PulseActionEnum.EconProcessor, new Action<EntityManager>(processor => { EconProcessor.ProcessSystem(_currentManager);}) },
            { PulseActionEnum.OrbitProcessor, new Action<EntityManager>(processor => { OrbitProcessor.UpdateSystemOrbits(_currentManager);}) },
            { PulseActionEnum.OrderProcessor, new Action<EntityManager>(processor => { OrderProcessor.ProcessSystem(_currentManager);}) },
            { PulseActionEnum.BalisticMoveProcessor, new Action<EntityManager>(processor => { NewtonBalisticProcessor.Process(_currentManager);}) },
            { PulseActionEnum.MoveOnlyProcessor, new Action<EntityManager>(processor => { DoNothing();}) }, //movement always runs on a subpulse prior to this. 
            //{ SystemActionEnum.SomeOtherProcessor, new Action<StarSystem>(processor => { Something.SomeOtherProcess(_currentSystem, _currentEntity);}) },
        };
        internal static void DoAction(PulseActionEnum action, EntityManager manager, Entity entity)
        {
            _currentManager = manager;
            _currentEntity = entity;
                
            EnumProcessorMap[action].DynamicInvoke(entity);

        }
        internal static void DoAction(PulseActionEnum action, EntityManager manager)
        {

            _currentManager = manager;
            EnumProcessorMap[action].DynamicInvoke(manager);

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
