using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    internal class ProcessorManager
    {
     
        private readonly Dictionary<Type, IHotloopProcessor> _hotloopProcessors = new Dictionary<Type, IHotloopProcessor>();
        private readonly List<IRecalcProcessor> _recalcProcessors = new List<IRecalcProcessor>();
        private readonly Dictionary<PulseActionEnum, IHotloopProcessor> _hotloopProcessorsByEnum = new Dictionary<PulseActionEnum, IHotloopProcessor>();
        private StaticDataStore _staticData;
        internal ProcessorManager(Game game)
        {
            _staticData = game.StaticData;
            CreateProcessors(game);
        }

        internal void AddHotloopProcessor<T>(IHotloopProcessor processor) where T: BaseDataBlob
        {
            _hotloopProcessors.Add(typeof(T), processor);
        }

        internal void AddRecalcProcessor(IRecalcProcessor processor)
        {
            if (processor.ProcessPriority <= _recalcProcessors.Count)
                _recalcProcessors.Insert(processor.ProcessPriority, processor);
            else
                _recalcProcessors.Add(processor);
        }

        internal void Hotloop<T>(EntityManager manager, int deltaSeconds) where T: BaseDataBlob
        {
            var type = typeof(T);
            
            _hotloopProcessors[type].ProcessManager(manager, deltaSeconds);
        }

        internal IHotloopProcessor GetProcessor<T>() where T : BaseDataBlob
        {
            return _hotloopProcessors[typeof(T)];
        }

        internal void RunProcessOnEntity<T>(Entity entity, int deltaSeconds)
            where T : BaseDataBlob
        {
            var type = typeof(T);
            _hotloopProcessors[type].ProcessEntity(entity, deltaSeconds);
        }

        internal void RecalcEntity(Entity entity)
        {
            foreach (var processor in _recalcProcessors)
            {
                processor.RecalcEntity(entity);
            }
        }



        private void CreateProcessors(Game game)
        {
            AddHotloopProcessor<EntityResearchDB>(new ResearchProcessor(game.StaticData));
            AddHotloopProcessor<MiningDB>(new MineResourcesProcessor(_staticData));            
            AddHotloopProcessor<RefiningDB>(new RefineResourcesProcessor(_staticData.ProcessedMaterials));
            AddHotloopProcessor<ConstructionDB>(new ConstructEntitiesProcessor());
            AddHotloopProcessor<PropulsionDB>(new ShipMovement());
            AddHotloopProcessor<OrbitDB>(new OrbitProcessor());
            AddHotloopProcessor<NewtonBalisticDB>(new NewtonBalisticProcessor());
            AddHotloopProcessor<OrderableDB>(new OrderableProcessor(game));
            AddHotloopProcessor<TranslateMoveDB>(new TranslateMoveProcessor());
            //AddHotloopProcessor<SensorProfileDB>(new SetReflectedEMProfile());



            ///AddRecalcProcessor
        }
    }

    /// <summary>
    /// Hotloop processor - this proccessor is fired at a specific regular time interval
    /// </summary>
    internal interface IHotloopProcessor
    {   
        void ProcessEntity(Entity entity, int deltaSeconds);
        void ProcessManager(EntityManager manager, int deltaSeconds);
        TimeSpan RunFrequency { get; }
    }


    /// <summary>
    /// Instance processor. - This processor is fired at a specific timedate or on command. 
    /// </summary>
    internal interface IInstanceProcessor
    {
        void ProcessEntity(Entity entity, int deltaSeconds);
    }


    /// <summary>
    /// Recalc processor. - this processor is called when something on the entity changes. 
    /// </summary>
    internal interface IRecalcProcessor
    {
        byte ProcessPriority { get; set; }
        void RecalcEntity(Entity entity);
    }
}