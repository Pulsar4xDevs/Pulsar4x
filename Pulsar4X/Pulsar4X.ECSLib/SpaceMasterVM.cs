using System;
namespace Pulsar4X.ECSLib
{
    public class SpaceMasterVM
    {
        public SpaceMasterVM()
        {
        }

        public void SMSetOrbitToEntity(Entity entity, Entity parentEntity, double perihelionKM, DateTime time)
        {

            var db = ShipMovementProcessor.CreateOrbitHereWithPerihelion(entity, parentEntity, perihelionKM, time);
            entity.SetDataBlob(db);
        }
    }
}
