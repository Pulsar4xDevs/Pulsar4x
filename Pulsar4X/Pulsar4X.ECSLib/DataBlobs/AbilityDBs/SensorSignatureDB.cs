
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class SensorSignatureDB : BaseDataBlob
    {
        [JsonProperty]
        private int _thermalSig;
        public int ThermalSig { get { return _thermalSig; } internal set { _thermalSig = value; } }

        [JsonProperty]
        private int _electroMagneticSig;
        public int ElectroMagneticSig { get { return _electroMagneticSig; } internal set { _electroMagneticSig = value; } }

        public SensorSignatureDB(int thermalSig, int electroMagneticSig)
        {
            _thermalSig = thermalSig;
            _electroMagneticSig = electroMagneticSig;
        }

        public SensorSignatureDB(SensorSignatureDB db)
        {
            _thermalSig = db.ThermalSig;
            _electroMagneticSig = db.ElectroMagneticSig;
        }

        public override object Clone()
        {
            return new SensorSignatureDB(this);
        }
    }
}