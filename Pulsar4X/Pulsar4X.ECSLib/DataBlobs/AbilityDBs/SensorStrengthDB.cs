using System.Collections.Generic;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class GravSensorStrenghtDB : BaseDataBlob
    {

        [JsonProperty]
        private int _gravSensorStrenght;

        public int GravSensorStrenght { get { return _gravSensorStrenght; } internal set { _gravSensorStrenght = value; } } 

        public GravSensorStrenghtDB()
        {

        }

        public GravSensorStrenghtDB(GravSensorStrenghtDB db)
        {
            _gravSensorStrenght = db.GravSensorStrenght;
        }

        public override object Clone()
        {
            return new GravSensorStrenghtDB(this);
        }
    }
}