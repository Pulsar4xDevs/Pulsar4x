namespace Pulsar4X.ECSLib
{
    public class PassiveEMSensorAtbDB : BaseDataBlob
    {
        public int EMSensitivity { get; internal set; }

        public override object Clone()
        {
            return new PassiveEMSensorAtbDB {EMSensitivity = EMSensitivity, OwningEntity = OwningEntity};
        }
    }
}
