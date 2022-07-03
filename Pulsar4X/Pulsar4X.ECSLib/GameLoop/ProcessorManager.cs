using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Pulsar4X.ECSLib
{
    //consdisder making this a singleton
    internal class ProcessorManager
    {

        internal readonly Dictionary<Type, IHotloopProcessor> HotloopProcessors = new Dictionary<Type, IHotloopProcessor>();
        private readonly List<IRecalcProcessor> _recalcProcessors = new List<IRecalcProcessor>();
        //private readonly Dictionary<PulseActionEnum, IHotloopProcessor> _hotloopProcessorsByEnum = new Dictionary<PulseActionEnum, IHotloopProcessor>();
        private readonly Dictionary<string, IInstanceProcessor> _instanceProcessors = new Dictionary<string, IInstanceProcessor>();
        private StaticDataStore _staticData;
        internal ProcessorManager(Game game)
        {
            _staticData = game.StaticData;
            CreateProcessors(game);
        }

        internal void AddHotloopProcessor<T>(IHotloopProcessor processor) where T : BaseDataBlob
        {
            HotloopProcessors.Add(typeof(T), processor);
        }

        internal void AddRecalcProcessor(IRecalcProcessor processor)
        {
            if (processor.ProcessPriority <= _recalcProcessors.Count)
                _recalcProcessors.Insert(processor.ProcessPriority, processor);
            else
                _recalcProcessors.Add(processor);
        }

        internal void Hotloop<T>(EntityManager manager, int deltaSeconds) where T : BaseDataBlob
        {
            var type = typeof(T);

            HotloopProcessors[type].ProcessManager(manager, deltaSeconds);
        }

        internal IHotloopProcessor GetProcessor<T>() where T : BaseDataBlob
        {
            return HotloopProcessors[typeof(T)];
        }

        /// <summary>
        /// Runs a hotloop process on a single entity.
        /// </summary>
        /// <param name="entity">Entity.</param>
        /// <param name="deltaSeconds">Delta seconds.</param>
        /// <typeparam name="T">The datablob for the given hotloop process</typeparam>
        internal void RunProcessOnEntity<T>(Entity entity, int deltaSeconds)
            where T : BaseDataBlob
        {
            var type = typeof(T);
            HotloopProcessors[type].ProcessEntity(entity, deltaSeconds);
        }

        /// <summary>
        /// Runs a given instance process on an entity.
        /// </summary>
        /// <param name="typeName">the typename of the instance process</param>
        /// <param name="entity">Entity.</param>
        /// <param name="dateTime">Date time.</param>
        internal void RunInstanceProcessOnEntity(string typeName, Entity entity, DateTime dateTime)
        {
            _instanceProcessors[typeName].ProcessEntity(entity, dateTime);
        }

        internal void RecalcEntity(Entity entity)
        {
            foreach (var processor in _recalcProcessors)
            {
                processor.RecalcEntity(entity);
            }
        }

        internal IInstanceProcessor GetInstanceProcessor(string typeName)
        {
            return _instanceProcessors[typeName];
        }


        private void CreateProcessors(Game game)
        {
            /*
            AddHotloopProcessor<EntityResearchDB>(new ResearchProcessor(game.StaticData));
            AddHotloopProcessor<MiningDB>(new MineResourcesProcessor(_staticData));
            AddHotloopProcessor<RefineAbilityDB>(new RefineResourcesProcessor(_staticData.ProcessedMaterials));
            AddHotloopProcessor<ConstructAbilityDB>(new ConstructEntitiesProcessor());
            AddHotloopProcessor<PropulsionDB>(new ShipMovement());
            AddHotloopProcessor<OrbitDB>(new OrbitProcessor());
            AddHotloopProcessor<NewtonBalisticDB>(new NewtonBalisticProcessor());
            AddHotloopProcessor<OrderableDB>(new OrderableProcessor(game));
            AddHotloopProcessor<TranslateMoveDB>(new TranslateMoveProcessor());
            //AddHotloopProcessor<SensorProfileDB>(new SetReflectedEMProfile());
            */

            var hotloopTypes = GetDerivedTypesFor(typeof(IHotloopProcessor));
            foreach (var hotloopType in hotloopTypes)
            {  
                IHotloopProcessor processor = (IHotloopProcessor)Activator.CreateInstance(hotloopType);
                processor.Init(game);
                Type type = processor.GetParameterType;
                HotloopProcessors.Add(type, processor);
            }

            var instanceTypes = GetDerivedTypesFor(typeof(IInstanceProcessor));
            foreach (var itemType in instanceTypes)
            {
                IInstanceProcessor processor = (IInstanceProcessor)Activator.CreateInstance(itemType);
                _instanceProcessors.Add(processor.TypeName, processor);
            }

            ///AddRecalcProcessor
        }

        private static IEnumerable<Type> GetDerivedTypesFor(Type baseType)
        {
            var assembly = Assembly.GetExecutingAssembly();

            return assembly.GetTypes()
                .Where(baseType.IsAssignableFrom)
                .Where(t => baseType != t);
        }
    }

    /// <summary>
    /// Hotloop processor - this proccessor is fired at a specific regular time interval
    /// </summary>
    internal interface IHotloopProcessor
    {
        /// <summary>
        /// used to initialize processors that need access to static data etc, used to construct a derived class's concrete object. 
        /// </summary>
        /// <param name="game"></param>
        void Init(Game game);

        /// <summary>
        /// used when a specific entity should be processed. 
        /// </summary>
        /// <param name="entity">Entity.</param>
        /// <param name="deltaSeconds">Delta seconds.</param>
        void ProcessEntity(Entity entity, int deltaSeconds);

        /// <summary>
        /// used to process all entities that have the GetParameterType type of datablob in a specific manager. 
        /// </summary>
        /// <param name="manager">Manager.</param>
        /// <param name="deltaSeconds">Delta seconds.</param>
        int ProcessManager(EntityManager manager, int deltaSeconds);

        /// <summary>
        /// How Often this processor should run. 
        /// </summary>
        /// <value>The run frequency.</value>
        TimeSpan RunFrequency { get; }
        /// <summary>
        /// This is so that each processor can be offset a bit, so they are spaced apart a bit. 
        /// In the case of short quick turns this should help prevent a lag spike as the game tries to process all economy etc on a single tick. 
        /// </summary>
        /// <value>The first run offset.</value>
        TimeSpan FirstRunOffset { get; } 
        /// <summary>
        /// this should return the specific Datablob that a derived class is accociated with. 
        /// </summary>
        /// <value>The type of the get parameter.</value>
        Type GetParameterType { get; }
    }


    /// <summary>
    /// Instance processor. - This processor is fired at a specific timedate or on command. 
    /// </summary>
    public abstract class IInstanceProcessor
    {
        internal string TypeName { get { return GetType().Name; } }
        internal abstract void ProcessEntity(Entity entity, DateTime atDateTime);
    }


    /// <summary>
    /// Recalc processor. - this processor is called when something on the entity changes.
    /// ie if a ship gets damaged, or modified, etc. the max speed and other stuff may need to be recalculated.   
    /// </summary>
    internal interface IRecalcProcessor
    {
        /// <summary>
        /// This is used so that some recalc processors can be run before others
        /// </summary>
        /// <value>The process priority.</value>
        byte ProcessPriority { get; set; }
        /// <summary>
        /// function to recalculate an entity. 
        /// </summary>
        /// <param name="entity">Entity.</param>
        void RecalcEntity(Entity entity);
    }
}