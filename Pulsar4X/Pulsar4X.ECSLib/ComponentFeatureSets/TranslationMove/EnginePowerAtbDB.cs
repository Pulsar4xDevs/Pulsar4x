using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class EnginePowerAtbDB : BaseDataBlob, IComponentDesignAttribute
    {
        [JsonProperty]
        public int EnginePower { get; internal set; }

        public EnginePowerAtbDB()
        {
        }

        public EnginePowerAtbDB(double power)
        {
            EnginePower = (int)power;
        }

        public EnginePowerAtbDB(int enginePower)
        {
            EnginePower = enginePower;
        }

        public EnginePowerAtbDB(EnginePowerAtbDB abilityDB)
        {
            EnginePower = abilityDB.EnginePower;
        }

        public override object Clone()
        {
            return new EnginePowerAtbDB(this);
        }

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            if (!parentEntity.HasDataBlob<PropulsionAbilityDB>())
                parentEntity.SetDataBlob(new PropulsionAbilityDB());
            ShipMovementProcessor.CalcMaxSpeedAndFuelUsage(parentEntity);
        }
    }
}