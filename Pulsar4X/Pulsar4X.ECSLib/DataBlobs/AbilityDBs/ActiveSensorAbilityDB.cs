using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class ActiveSensorDB : BaseDataBlob
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
        
        public ActiveSensorDB() { }

        public ActiveSensorDB(ActiveSensorDB db) : this (db.GravSensorStrength, db.EMSensitivity, db.Resolution) { }

        /// <summary>
        /// This is the constructor for the component DesignToEntity factory.
        /// note that while the params are doubles, it casts them to ints.
        /// </summary>
        /// <param name="gravStrenghth"></param>
        /// <param name="emSensitivity"></param>
        /// <param name="resolution"></param>
        public ActiveSensorDB(double gravStrenghth, double emSensitivity, double resolution) : this ((int) gravStrenghth, (int) emSensitivity, (int) resolution) { }
        
        public ActiveSensorDB(int gravStrength, int emSensitivity, int resolution)
        {
            GravSensorStrength = gravStrength;
            EMSensitivity = emSensitivity;
            Resolution = resolution;
        }

        public override object Clone()
        {
            return new ActiveSensorDB(this);
        }
    }

    public class ActiveSensorStateInfo
    {
        public Entity Target { get; internal set; } = Entity.InvalidEntity;
    }
}