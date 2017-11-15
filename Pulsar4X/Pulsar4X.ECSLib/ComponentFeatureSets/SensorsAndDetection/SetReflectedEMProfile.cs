using System;

namespace Pulsar4X.ECSLib
{
    internal class SetReflectedEMProfile : IHotloopProcessor
    {
        public TimeSpan RunFrequency => TimeSpan.FromMinutes(10);

        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            SetEntityProfile(entity);
        }

        public void ProcessManager(EntityManager manager, int deltaSeconds)
        {
            var entites = manager.GetAllEntitiesWithDataBlob<SensorProfileDB>();
            foreach (var entity in entites)
            {
                ProcessEntity(entity, deltaSeconds);
            }
        }

        internal static void SetEntityProfile(Entity entity)
        {
            var position = entity.GetDataBlob<PositionDB>();
            var sensorSig = entity.GetDataBlob<SensorProfileDB>();

            var emmiters = entity.Manager.GetAllEntitiesWithDataBlob<SensorProfileDB>();
            foreach (var emittingEntity in emmiters)
            {
                double distance = PositionDB.GetDistanceBetween(position, emittingEntity.GetDataBlob<PositionDB>());
                var emmissionDB = emittingEntity.GetDataBlob<SensorProfileDB>();

                sensorSig.ReflectedEMSpectra = SensorProcessorTools.AttenuatedForDistance(emmissionDB, distance);
            }
        }
    }
}
