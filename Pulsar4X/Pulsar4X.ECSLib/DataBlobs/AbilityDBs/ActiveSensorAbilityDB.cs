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

        public ActiveSensorDB()
        {
        }

        /// <summary>
        /// This is the constructor for the component DesignToEntity factory.
        /// note that while the params are doubles, it casts them to ints.
        /// </summary>
        /// <param name="gravStrenght"></param>
        /// <param name="emSensitivity"></param>
        /// <param name="resolution"></param>
        public ActiveSensorDB(double gravStrenght, double emSensitivity, double resolution)
        {
            GravSensorStrength = (int)gravStrenght;
            EMSensitivity = (int)emSensitivity;
            Resolution = (int)resolution;
        }

        public ActiveSensorDB(ActiveSensorDB db)
        {
            GravSensorStrength = db.GravSensorStrength;
        }

        public override object Clone()
        {
            return new ActiveSensorDB(this);
        }
    }
}