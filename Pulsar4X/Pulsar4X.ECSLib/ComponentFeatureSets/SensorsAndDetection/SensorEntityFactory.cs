using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class SensorEntityFactory
    {
        public SensorEntityFactory()
        {
        }

        internal static Entity CreateNewSensorContact(Entity detectedEntity, SensorInfo sensorInfo)
        {
            var posDB = (PositionDB)detectedEntity.GetDataBlob<PositionDB>().Clone();

                
            var datablobs = new List<BaseDataBlob>()
            {
                posDB,
            };
            if (detectedEntity.HasDataBlob<OrbitDB>())
            {
                datablobs.Add((OrbitDB)detectedEntity.GetDataBlob<OrbitDB>().Clone());
            }
            if (detectedEntity.HasDataBlob<TranslateMoveDB>())
            {
                datablobs.Add((TranslateMoveDB)detectedEntity.GetDataBlob<TranslateMoveDB>().Clone());
            }


            return new Entity(detectedEntity.Manager);

        }
        internal static void UpdateSensorContact(Entity detectedEntity, SensorInfo sensorInfo)
        {
            foreach (var datablob in detectedEntity.DataBlobs)
            {

            }
        }
    }
}
