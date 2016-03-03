namespace Pulsar4X.ECSLib
{
    public class PassiveEMSensorAbilityDB : BaseDataBlob
    {
        public int EMSensitivity { get; internal set; }

        public override object Clone()
        {
            return new PassiveEMSensorAbilityDB {EMSensitivity = EMSensitivity, OwningEntity = OwningEntity};
        }
    }
}
