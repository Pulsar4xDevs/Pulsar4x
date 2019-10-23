using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public static class SensorEntityFactory
    {


        internal static SensorContact UpdateSensorContact(Entity detectingFaction, SensorInfoDB sensorInfo)
        {
            Entity detectedEntity = sensorInfo.DetectedEntity;

            if (sensorInfo.SensorContact == null)
            {
                 
                //if()
                List<BaseDataBlob> datablobs = new List<BaseDataBlob>(){                    
                    sensorInfo,
                    SetPositionClone(sensorInfo),               
                    };
                //sensorInfo.SensorEntity = Entity.Create(detectedEntity.Manager.FactionSensorManagers[detectingFaction.Guid], detectingFaction.Guid, datablobs);
                var pos = SetPositionClone(sensorInfo);

                //detectingFaction.GetDataBlob<FactionOwnerDB>().SetOwned(sensorInfo.SensorContact);
            }

            foreach (ISensorCloneMethod db in sensorInfo.DetectedEntity.DataBlobs.OfType<ISensorCloneMethod>())
            {
                /*
                int typeIndex1 = EntityManager.DataBlobTypes[db.GetType()];
                int typeIndex;
                EntityManager.TryGetTypeIndex(db.GetType(), out typeIndex);
                if (!sensorInfo.SensorContact.HasDataBlob(typeIndex)) 
                {
                    var cloned = db.SensorClone(sensorInfo);
                    sensorInfo.SensorContact.SetDataBlob(cloned);  
                }
                else
                {
                    //TODO: Optimize, Networking: don't do this if there are not going to be any changes. (ie no new sensor data)
                    db.SensorUpdate(sensorInfo);    
                    //TODO: Networking: we need to send this DB to any listning network clients since it's a change that they wont(and shouldn't) know how to calculate on thier own. 
                    //TODO: Networking: write an EntityChangeListner to handle serverside DB change notification. 
                }*/
            }




            //if (sensorInfo.DetectedEntity.HasDataBlob<OrbitDB>())
            //{ SetOrbitClone(detectedEntity.GetDataBlob<OrbitDB>(), sensorInfo); }

            return sensorInfo.SensorContact;        
        }

        private static SensorPositionDB SetPositionClone( SensorInfoDB sensorInfo)
        {
            PositionDB position = sensorInfo.DetectedEntity.GetDataBlob<PositionDB>();
            SensorPositionDB sensorEntityPosition = new SensorPositionDB(position);
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

        private static void SetTranslateClone(WarpMovingDB detectedEntitiesMove, SensorInfoDB sensorInfo)
        {
            //var quality = sensorInfo.HighestDetectionQuality.detectedSignalQuality.Percent; //quality shouldn't affect positioning. 
            //double signalBestMagnatude = sensorInfo.HighestDetectionQuality.detectedSignalStrength_kW;
            double signalNowMagnatude = sensorInfo.LatestDetectionQuality.SignalStrength_kW;
            if (signalNowMagnatude > 0.0)
            {
                var sensorEntityMove =  GenericClone<WarpMovingDB>(detectedEntitiesMove, sensorInfo);

                sensorEntityMove.TargetPositionDB = null; //the sensorEntity shouldn't know the final destination. 

                Vector3 velocityDetectionInacuracy = new Vector3() { }; //some random noise depending on quality value

                sensorEntityMove.CurrentNonNewtonionVectorMS = detectedEntitiesMove.CurrentNonNewtonionVectorMS + velocityDetectionInacuracy;
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

