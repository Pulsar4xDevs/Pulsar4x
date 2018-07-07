using System;
using System.Collections.Generic;
using System.Linq;

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
                    };
                sensorInfo.SensorEntity = Entity.Create(detectedEntity.Manager, detectingFaction.Guid, datablobs);
                new OwnedDB(detectingFaction, sensorInfo.SensorEntity);
            }

            foreach (ISensorCloneMethod db in sensorInfo.DetectedEntity.DataBlobs.OfType<ISensorCloneMethod>())
            {

                int typeIndex1 = EntityManager.DataBlobTypes[db.GetType()];
                int typeIndex;
                EntityManager.TryGetTypeIndex(db.GetType(), out typeIndex);
                if (!sensorInfo.SensorEntity.HasDataBlob(typeIndex)) 
                {
                    var cloned = db.SensorClone(sensorInfo);
                    sensorInfo.SensorEntity.SetDataBlob(cloned);  
                }
                else
                {
                    //TODO: Optimize, Networking: don't do this if there are not going to be any changes. (ie no new sensor data)
                    db.SensorUpdate(sensorInfo);    
                    //TODO: Networking: we need to send this DB to any listning network clients since it's a change that they wont(and shouldn't) know how to calculate on thier own. 
                    //TODO: Networking: write an EntityChangeListner to handle serverside DB change notification. 
                }
            }




            //if (sensorInfo.DetectedEntity.HasDataBlob<OrbitDB>())
            //{ SetOrbitClone(detectedEntity.GetDataBlob<OrbitDB>(), sensorInfo); }

            return sensorInfo.SensorEntity;        
        }

        private static PositionDB SetPositionClone( SensorInfoDB sensorInfo)
        {
            PositionDB position = sensorInfo.DetectedEntity.GetDataBlob<PositionDB>();
            PositionDB sensorEntityPosition = GenericClone<PositionDB>(position, sensorInfo);
            //tweak add some random noise depending on quality; 
            return sensorEntityPosition;

        }
        /*
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
        */

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
        BaseDataBlob SensorClone(SensorInfoDB sensorInfo);
        void SensorUpdate(SensorInfoDB sensorInfo);
    }

}

