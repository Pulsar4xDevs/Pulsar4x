namespace Pulsar4X.ECSLib
{
    public class PassiveThermalSensorAtbDB : BaseDataBlob
    {
        public int ThermalSensitivity { get; internal set; }

        public override object Clone()
        {
            return new PassiveThermalSensorAtbDB { ThermalSensitivity = ThermalSensitivity, OwningEntity = OwningEntity };
        }
    }
}