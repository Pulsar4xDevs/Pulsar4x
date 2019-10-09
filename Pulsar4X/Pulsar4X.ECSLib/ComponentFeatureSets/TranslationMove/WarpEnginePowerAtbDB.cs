using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class WarpEnginePowerAtbDB : BaseDataBlob, IComponentDesignAttribute
    {
        [JsonProperty]
        public int EnginePower { get; internal set; }

        public WarpEnginePowerAtbDB()
        {
        }

        public WarpEnginePowerAtbDB(double power)
        {
            EnginePower = (int)power;
        }

        public WarpEnginePowerAtbDB(int enginePower)
        {
            EnginePower = enginePower;
        }

        public WarpEnginePowerAtbDB(WarpEnginePowerAtbDB abilityDB)
        {
            EnginePower = abilityDB.EnginePower;
        }

        public override object Clone()
        {
            return new WarpEnginePowerAtbDB(this);
        }

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            if (!parentEntity.HasDataBlob<PropulsionAbilityDB>())
                parentEntity.SetDataBlob(new PropulsionAbilityDB());
            ShipMovementProcessor.CalcMaxSpeedAndFuelUsage(parentEntity);
        }
    }
}