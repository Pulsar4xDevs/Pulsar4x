namespace Pulsar4X.Interfaces
{
    public interface ICargoable
    {
        int ID { get; }
        string UniqueID { get; }
        string CargoTypeID { get; }
        string Name { get; }
        long MassPerUnit { get; }
        double VolumePerUnit { get; }
    }
}