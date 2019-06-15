using System;

namespace Pulsar4X.ECSLib.ComponentFeatureSets.CargoStorage
{
    public class CargoCapacityCheckResult
    {
        public Guid IdOfItemChecked { get; private set; }
        public long FreeCapacityItem { get; private set; }
        public long FreeCapacityKg { get; private set; }

        public CargoCapacityCheckResult(Guid item, long count, long kg)
        {
            IdOfItemChecked = item;
            FreeCapacityItem = count;
            FreeCapacityKg = kg;
        }
    }
}
