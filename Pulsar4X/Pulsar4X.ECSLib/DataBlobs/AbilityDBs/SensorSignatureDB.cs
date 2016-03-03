using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class SensorSignatureDB : BaseDataBlob
    {
        [JsonProperty]
        public int ThermalSig { get; internal set; }

        [JsonProperty]
        public int ElectroMagneticSig { get; internal set; }

        public SensorSignatureDB() { }

        /// <summary>
        /// Constructor for Factory. note int cast.
        /// </summary>
        /// <param name="thermalSig"></param>
        /// <param name="electroMagneticSig"></param>
        public SensorSignatureDB(double thermalSig, double electroMagneticSig)
        {
            ThermalSig = (int)thermalSig;
            ElectroMagneticSig = (int)electroMagneticSig;
        }

        public SensorSignatureDB(SensorSignatureDB db)
        {
            ThermalSig = db.ThermalSig;
            ElectroMagneticSig = db.ElectroMagneticSig;
        }

        public override object Clone()
        {
            return new SensorSignatureDB(this);
        }
    }
}