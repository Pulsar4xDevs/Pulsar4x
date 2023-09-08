using System;

namespace Pulsar4X.ECSLib
{
    public class FleetOrderProcessor : IHotloopProcessor
    {
        public TimeSpan RunFrequency => TimeSpan.FromHours(1);

        public TimeSpan FirstRunOffset => TimeSpan.FromHours(1);

        public Type GetParameterType => typeof(FleetDB);

        public void Init(Game game)
        {

        }

        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            Console.WriteLine(entity.Guid.ToString());
        }

        public int ProcessManager(EntityManager manager, int deltaSeconds)
        {
            var entities = manager.GetAllEntitiesWithDataBlob<FleetDB>();
            foreach (var entity in entities)
            {
                ProcessEntity(entity, deltaSeconds);
            }
            return entities.Count;
        }
    }
}