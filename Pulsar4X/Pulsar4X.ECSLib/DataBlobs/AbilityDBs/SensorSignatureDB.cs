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

        public SensorSignatureDB()
        {
        }

        /// <summary>
        /// Constructor for Factory. note int cast.
        /// </summary>
        /// <param name="thermalSig"></param>
        /// <param name="electroMagneticSig"></param>
        public SensorSignatureDB(double thermalSig, double electroMagneticSig)
        {
            _thermalSig = (int)thermalSig;
            _electroMagneticSig = (int)electroMagneticSig;
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