using Pulsar4X.Datablobs;

namespace Pulsar4X.Interfaces
{
    public interface ISensorCloneMethod
    {
        BaseDataBlob SensorClone(SensorInfoDB sensorInfo);
        void SensorUpdate(SensorInfoDB sensorInfo);
    }
}