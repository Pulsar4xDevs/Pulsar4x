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