namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// Info on the sensors in a ship.
    /// </summary>
    public class SensorsDB : BaseDataBlob
    {
        public SensorsDB()
        {
        }

        public SensorsDB(SensorsDB sensorsDB)
        {
        }

        public override object Clone()
        {
            return new SensorsDB(this);
        }
    }
}