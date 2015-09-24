using System.Collections.Generic;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class ActiveSensorDB : BaseDataBlob
    {

        [JsonProperty]
        private int _gravSensorStrength;
        public int GravSensorStrength { get { return _gravSensorStrength; } internal set { _gravSensorStrength = value; } }

        [JsonProperty]
        private int _emSensitivity;
        public int EMSensitivity { get { return _emSensitivity; } internal set { _emSensitivity = value; } }

        [JsonProperty]
        private int _resolution;
        public int Resolution { get { return _resolution; } internal set { _resolution = value; } }

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
            _gravSensorStrength = (int)gravStrenght;
            _emSensitivity = (int)emSensitivity;
            _resolution = (int)resolution;
        }

        public ActiveSensorDB(ActiveSensorDB db)
        {
            _gravSensorStrength = db.GravSensorStrength;
        }

        public override object Clone()
        {
            return new ActiveSensorDB(this);
        }
    }
}