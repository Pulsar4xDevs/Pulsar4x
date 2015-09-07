
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class EnginePowerDB : BaseDataBlob
    {
        [JsonProperty]
        private int _enginePower;

        public int EnginePower { get { return _enginePower; } internal set { _enginePower = value; } }

        public EnginePowerDB()
        {
        }

        public EnginePowerDB(int enginePower)
        {
            _enginePower = enginePower;
        }

        public EnginePowerDB(EnginePowerDB db)
        {
            _enginePower = db.EnginePower;
        }

        public override object Clone()
        {
            return new EnginePowerDB(this);
        }
    }
}