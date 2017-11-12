using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class SensorEntityFactory
    {
        public SensorEntityFactory()
        {
        }

        public static Entity CreateNewSensorContact(Entity fromEntity)
        {
            var posDB = (PositionDB)fromEntity.GetDataBlob<PositionDB>().Clone();
            var datablobs = new List<BaseDataBlob>()
            {
                posDB,
            };
            return new Entity(fromEntity.Manager);

        }
    }
}
