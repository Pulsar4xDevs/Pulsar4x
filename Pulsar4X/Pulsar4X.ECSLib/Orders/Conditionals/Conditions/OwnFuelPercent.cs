namespace Pulsar4X.ECSLib
{
    partial class Condition
    {
        public class OwnFuelPercent : Condition
        {
            public OwnFuelPercent(ProtoEntity fuelCarrier, float fuelPercent)
            {
                Type = ConditionType.OwnFuelPercet;
                Entities[0] = fuelCarrier;
                Floats[0] = fuelPercent;
            }

            public override bool IsMet()
            {
                ProtoEntity self = Entities[0];
                float fuelPercent = Floats[0];

                // Get ship fuel
                var propulsionDB = self.GetDataBlob<PropulsionDB>();
                float shipFuel = (float)propulsionDB.CurrentFuelStored / propulsionDB.FuelStorageCapicity;

                return shipFuel <= fuelPercent;
            }
        }
    }
}
