using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public static class SensorEntityFactory
    {


        internal static Entity UpdateSensorContact(Entity detectingFaction, SensorInfoDB sensorInfo)
        {
            Entity detectedEntity = sensorInfo.DetectedEntity;

            if (sensorInfo.SensorEntity == null)
            {
                List<BaseDataBlob> datablobs = new List<BaseDataBlob>(){
                sensorInfo,
                SetPositionClone(sensorInfo),
                new OwnedDB(detectingFaction),
                };
                sensorInfo.SensorEntity = new Entity(detectedEntity.Manager, datablobs);
            }
            foreach (var db in sensorInfo.DetectedEntity.DataBlobs)
            {
                if (db is ISensorCloneMethod)
                {
                    ISensorCloneMethod sensorcloneMethdDB = (ISensorCloneMethod)db;
                    var cloned = sensorcloneMethdDB.Clone(sensorInfo);
                    sensorInfo.SensorEntity.SetDataBlob(cloned);
                }
            }




            if (sensorInfo.DetectedEntity.HasDataBlob<OrbitDB>())
            { SetOrbitClone(detectedEntity.GetDataBlob<OrbitDB>(), sensorInfo); }

            return sensorInfo.SensorEntity;        
        }

        private static PositionDB SetPositionClone( SensorInfoDB sensorInfo)
        {
            PositionDB position = sensorInfo.DetectedEntity.GetDataBlob<PositionDB>();
            PositionDB sensorEntityPosition = GenericClone<PositionDB>(position, sensorInfo);
            //tweak add some random noise depending on quality; 
            return sensorEntityPosition;

        }

        private static void SetOrbitClone(OrbitDB detectedEntitesOrbit, SensorInfoDB sensorInfo)
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

        private static void SetTranslateClone(TranslateMoveDB detectedEntitiesMove, SensorInfoDB sensorInfo)
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



        private static T GenericClone<T>(T datablob, SensorInfoDB sensorInfo) where T: BaseDataBlob
        {

            T sensorEntitesDB;
            if (sensorInfo.DetectedEntity.HasDataBlob<T>())
            {
                sensorEntitesDB = sensorInfo.DetectedEntity.GetDataBlob<T>();

            }
            else
            {
                sensorEntitesDB = (T)datablob.Clone();
                sensorInfo.DetectedEntity.SetDataBlob(sensorEntitesDB);
            }

            return sensorEntitesDB;

        }


        internal static void CompareDatabob<T>(BaseDataBlob db, Entity sensorEntity) where T : BaseDataBlob
        {
            if (sensorEntity.HasDataBlob<T>())
            { }
        }
    }


    public interface ISensorCloneMethod
    {
        BaseDataBlob Clone(SensorInfoDB sensorInfo);
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