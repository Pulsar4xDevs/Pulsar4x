namespace Pulsar4X.ECSLib
{
    public class PassiveThermalSensorAbilityDB : BaseDataBlob
    {
        public int ThermalSensitivity { get; internal set; }

        public override object Clone()
        {
            return new PassiveThermalSensorAbilityDB { ThermalSensitivity = ThermalSensitivity, OwningEntity = OwningEntity };
        }
    }
}