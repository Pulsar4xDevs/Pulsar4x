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

        public SensorSignatureDB(double thermalSig, double electroMagneticSig) : this((int)thermalSig, (int)electroMagneticSig) { }

        public SensorSignatureDB(int thermalSig, int electroMagneticSig)
        {
            ThermalSig = thermalSig;
            ElectroMagneticSig = electroMagneticSig;
        }

        public override object Clone()
        {
            return new SensorSignatureDB(ThermalSig, ElectroMagneticSig);
        }
    }
}