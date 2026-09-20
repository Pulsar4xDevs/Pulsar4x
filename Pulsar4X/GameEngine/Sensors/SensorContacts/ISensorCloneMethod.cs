using Pulsar4X.Datablobs;

namespace Pulsar4X.Sensors
{
    public interface ISensorCloneMethod
    {
        BaseDataBlob SensorClone(SensorInfoDB sensorInfo);
        void SensorUpdate(SensorInfoDB sensorInfo);
    }
}