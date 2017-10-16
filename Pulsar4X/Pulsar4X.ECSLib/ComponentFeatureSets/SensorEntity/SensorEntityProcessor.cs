using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class SensorEntityProcessor
    {
        public SensorEntityProcessor()
        {
        }


        void Sensor()
        {
            int sensorStrength = 10;

            List<Entity> emitters = new List<Entity>();


            foreach (var item in emitters)
            {
                int emmisionStr = 10;
                if (emmisionStr >= sensorStrength)
                { }
            }

        }




        void SetSensorsSig(SensorSigDB sig)
        {
            
        }


    }


    public class SensorSigDB : BaseDataBlob
    {
        internal double GravSig
        {
            get
            {
                if (OwningEntity.HasDataBlob<MassVolumeDB>())
                    return OwningEntity.GetDataBlob<MassVolumeDB>().Mass;
                else
                    return 0;
            }
        }

        internal Dictionary<double, double> EMSig { get; } = new Dictionary<double, double>();

        public SensorSigDB() { }

        public SensorSigDB(SensorSigDB db)
        {
            EMSig = new Dictionary<double, double>(db.EMSig);
        }

        public override object Clone()
        {
            return new SensorSigDB(this);
        }


    }

    public class FactionSensorInfoDB
    {
        Dictionary<Entity,EntityKnowledge> knownEntitys;


    }

    public struct EntityKnowledge
    {
        Entity entity;
        List<BaseDataBlob> knownDatablobs;
    }
}
