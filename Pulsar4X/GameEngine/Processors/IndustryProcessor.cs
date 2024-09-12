using System;
using Pulsar4X.Interfaces;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine.Industry;

namespace Pulsar4X.Engine
{
    public class IndustryProcessor : IHotloopProcessor
    {
        public TimeSpan RunFrequency
        {
            get { return TimeSpan.FromDays(1); }
        }

        public TimeSpan FirstRunOffset => TimeSpan.FromHours(3);

        public Type GetParameterType => typeof(IndustryAbilityDB);

        public void Init(Game game)
        {
            //unneeded.
        }

        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            IndustryTools.ConstructStuff(entity);
        }

        public int ProcessManager(EntityManager manager, int deltaSeconds)
        {
            var entities = manager.GetAllEntitiesWithDataBlob<IndustryAbilityDB>();
            foreach (var entity in entities)
            {
                ProcessEntity(entity, deltaSeconds);
            }

            return entities.Count;

        }
    }
}