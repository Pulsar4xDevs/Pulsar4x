using System.Collections.Generic;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class GravSensorStrenghtDB : BaseDataBlob
    {

        [JsonProperty]
        private int _gravSensorStrength;

        public int GravSensorStrength { get { return _gravSensorStrength; } internal set { _gravSensorStrength = value; } } 

        public GravSensorStrenghtDB()
        {

        }

        public GravSensorStrenghtDB(GravSensorStrenghtDB db)
        {
            _gravSensorStrength = db.GravSensorStrength;
        }

        public override object Clone()
        {
            return new GravSensorStrenghtDB(this);
        }
    }
}