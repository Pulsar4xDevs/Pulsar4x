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
                Type t = datablob.GetType();

                //if(sensorInfo.SensorEntity.HasDataBlob<t>
            }
        }

        internal static void CompareDatabob<T>(BaseDataBlob db, Entity sensorEntity) where T : BaseDataBlob
        {
            if (sensorEntity.HasDataBlob<T>())
            { }
        }
    }

    public interface ISensorMethods
    {
        void CompareDB(SensorInfo sensorInfo);
    }

    public class SomethingDB : BaseDataBlob, ISensorMethods
    {
        Random rnd = new Random();
        int someIntData;
        float someFloatData;

        public override object Clone()
        {
            throw new NotImplementedException();
        }

        public void CompareDB(SensorInfo sensorInfo)
        {
            if (sensorInfo.HighestDetectionQuality > 0.25)
            {
                if (sensorInfo.detectedEntity.HasDataBlob<SomethingDB>())
                {
                    var detectedEntityDB = sensorInfo.detectedEntity.GetDataBlob<SomethingDB>();
                    detectedEntityDB.someFloatData = this.someFloatData += (float)(-0.5 + (0.5 - -0.5) * rnd.NextDouble());
                    detectedEntityDB.someIntData = this.someIntData += rnd.Next(-500, 500); 
                }
            }
        }
    }
}
