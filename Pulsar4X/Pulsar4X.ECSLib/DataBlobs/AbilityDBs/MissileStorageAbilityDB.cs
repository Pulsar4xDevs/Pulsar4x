namespace Pulsar4X.ECSLib
{
    public class MissileStorageAbilityDB : BaseDataBlob
    {
        public int StorageCapacity { get; internal set; }
        public override object Clone()
        {
            throw new System.NotImplementedException();
        }
    }
}
