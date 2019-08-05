using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class SensorSignatureAtbDB : IComponentDesignAttribute
    {
        [JsonProperty]
        public int ThermalSig { get; internal set; }

        [JsonProperty]
        public int ElectroMagneticSig { get; internal set; }

        public SensorSignatureAtbDB() { }

        public SensorSignatureAtbDB(double thermalSig, double electroMagneticSig) : this((int)thermalSig, (int)electroMagneticSig) { }

        public SensorSignatureAtbDB(int thermalSig, int electroMagneticSig)
        {
            ThermalSig = thermalSig;
            ElectroMagneticSig = electroMagneticSig;
        }

        public object Clone()
        {
            return new SensorSignatureAtbDB(ThermalSig, ElectroMagneticSig);
        }

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            if(!parentEntity.HasDataBlob<SensorProfileDB>())
                parentEntity.SetDataBlob(new SensorProfileDB());
            
        }
    }
}