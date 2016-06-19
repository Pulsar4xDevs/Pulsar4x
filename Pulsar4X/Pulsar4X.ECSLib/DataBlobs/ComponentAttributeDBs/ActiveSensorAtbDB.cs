using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class ActiveSensorAtbDB : BaseDataBlob
    {
        [JsonProperty]
        public int GravSensorStrength { get; internal set; }

        [JsonProperty]
        public int EMSensitivity { get; internal set; }

        [JsonProperty]
        public int Resolution { get; internal set; }

        [JsonProperty]
        public bool IsSearchSensor { get; internal set; }
        public bool IsTrackingSensor => !IsSearchSensor;

        public ActiveSensorAtbDB(double gravStrenghth, double emSensitivity, double resolution, bool isSearchSensor) : this ((int) gravStrenghth, (int) emSensitivity, (int) resolution, isSearchSensor) { }
        
        [JsonConstructor]
        public ActiveSensorAtbDB(int gravStrength = 0, int emSensitivity = 0, int resolution = 0, bool isSearchSensor = true)
        {
            GravSensorStrength = gravStrength;
            EMSensitivity = emSensitivity;
            Resolution = resolution;
            IsSearchSensor = isSearchSensor;
        }

        public override object Clone()
        {
            return new ActiveSensorAtbDB(GravSensorStrength, EMSensitivity, Resolution, IsSearchSensor);
        }
    }

    public class ActiveSensorStateInfo
    {
        public Entity Target { get; internal set; } = Entity.InvalidEntity;
    }
}