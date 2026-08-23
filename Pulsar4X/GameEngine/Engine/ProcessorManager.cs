using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Pulsar4X.Interfaces;
using Pulsar4X.Datablobs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Pulsar4X.Engine
{
    [JsonConverter(typeof(ProcessManagerConverter))]
    internal class ProcessorManager
    {

        internal readonly Dictionary<Type, IHotloopProcessor> HotloopProcessors = new Dictionary<Type, IHotloopProcessor>();
        private readonly List<IRecalcProcessor> _recalcProcessors = new List<IRecalcProcessor>();
        //private readonly Dictionary<PulseActionEnum, IHotloopProcessor> _hotloopProcessorsByEnum = new Dictionary<PulseActionEnum, IHotloopProcessor>();
        private readonly Dictionary<string, IInstanceProcessor> _instanceProcessors = new Dictionary<string, IInstanceProcessor>();

        public int HotloopCount => HotloopProcessors.Count;
        public int RecalcCount => _recalcProcessors.Count;
        public int InstanceCount => _instanceProcessors.Count;

        private Game _game;
        internal ProcessorManager(Game game)
        {
            _game = game;
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
                IHotloopProcessor? processor = (IHotloopProcessor?)Activator.CreateInstance(hotloopType);
                if(processor == null) throw new NullReferenceException($"Unable to create instance of {hotloopType.Name}");
                processor.Init(game);
                Type type = processor.GetParameterType;
                HotloopProcessors.Add(type, processor);
            }

            var instanceTypes = GetDerivedTypesFor(typeof(IInstanceProcessor));
            foreach (var itemType in instanceTypes)
            {
                IInstanceProcessor? processor = (IInstanceProcessor?)Activator.CreateInstance(itemType);
                if(processor == null) throw new NullReferenceException($"Unable to create instance of {itemType.Name}");
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

    public class ProcessManagerConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(ProcessorManager);

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            // Save JObject to set it later in the second step
            JObject jsonObject = JObject.Load(reader);
            var gameProperty = serializer.Context.Context as Game;

            if (gameProperty != null)
            {
                var obj = new ProcessorManager(gameProperty);
                serializer.Populate(jsonObject.CreateReader(), obj);
                return obj;
            }

            return null;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            JObject obj = new JObject
            {
                // I don't think we need to serialize the processors
            };
            obj.WriteTo(writer);
        }
    }
}