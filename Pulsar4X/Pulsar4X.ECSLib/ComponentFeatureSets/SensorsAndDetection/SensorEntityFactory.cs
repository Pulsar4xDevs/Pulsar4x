using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public static class SensorEntityFactory
    {


        internal static Entity UpdateSensorContact(SensorInfo sensorInfo)
        {
            Entity detectedEntity = sensorInfo.detectedEntity;

            if (sensorInfo.SensorEntity == null)
            {
                sensorInfo.SensorEntity = new Entity(detectedEntity.Manager);
                sensorInfo.SensorEntity.SetDataBlob<PositionDB>(SetPositionClone(sensorInfo));
            }

            Entity sensorContact = sensorInfo.SensorEntity;


            if (sensorInfo.detectedEntity.HasDataBlob<OrbitDB>())
            { SetOrbitClone(detectedEntity.GetDataBlob<OrbitDB>(), sensorInfo); }

            return sensorContact;        
        }

        private static PositionDB SetPositionClone( SensorInfo sensorInfo)
        {
            PositionDB position = sensorInfo.detectedEntity.GetDataBlob<PositionDB>();
            PositionDB sensorEntityPosition = GenericClone<PositionDB>(position, sensorInfo);
            //tweak add some random noise depending on quality; 
            return sensorEntityPosition;

        }

        private static void SetOrbitClone(OrbitDB detectedEntitesOrbit, SensorInfo sensorInfo)
        {

            //var quality = sensorInfo.HighestDetectionQuality.detectedSignalQuality.Percent; //quality shouldn't affect positioning. 
            double signalBestMagnatude = sensorInfo.HighestDetectionQuality.SignalStrength_kW;
            double signalNowMagnatude = sensorInfo.LatestDetectionQuality.SignalStrength_kW;
            if (signalNowMagnatude > 0) 
            {
                OrbitDB sensorEntityOrbit = GenericClone<OrbitDB>(detectedEntitesOrbit, sensorInfo);
                //tweak add some random noise to the ecentricity etc of the sensorEntityOrbit depending on magnatude; 
            }
        }

        private static void SetTranslateClone(TranslateMoveDB detectedEntitiesMove, SensorInfo sensorInfo)
        {
            //var quality = sensorInfo.HighestDetectionQuality.detectedSignalQuality.Percent; //quality shouldn't affect positioning. 
            //double signalBestMagnatude = sensorInfo.HighestDetectionQuality.detectedSignalStrength_kW;
            double signalNowMagnatude = sensorInfo.LatestDetectionQuality.SignalStrength_kW;
            if (signalNowMagnatude > 0.0)
            {
                var sensorEntityMove =  GenericClone<TranslateMoveDB>(detectedEntitiesMove, sensorInfo);

                sensorEntityMove.TargetPositionDB = null; //the sensorEntity shouldn't know the final destination. 

                Vector4 velocityDetectionInacuracy = new Vector4() { }; //some random noise depending on quality value

                sensorEntityMove.CurrentVector = detectedEntitiesMove.CurrentVector + velocityDetectionInacuracy;
            }


        }


        private static T GenericClone<T>(T datablob, SensorInfo sensorInfo) where T: BaseDataBlob
        {

            T sensorEntitesDB;
            if (sensorInfo.detectedEntity.HasDataBlob<T>())
            {
                sensorEntitesDB = sensorInfo.detectedEntity.GetDataBlob<T>();

            }
            else
            {
                sensorEntitesDB = (T)datablob.Clone();
                sensorInfo.detectedEntity.SetDataBlob(sensorEntitesDB);
            }

            return sensorEntitesDB;

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
}

/* basicaly an idea of how to maybe do an interface on the datablobs, currently trying to avoid doing this.  

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
*/