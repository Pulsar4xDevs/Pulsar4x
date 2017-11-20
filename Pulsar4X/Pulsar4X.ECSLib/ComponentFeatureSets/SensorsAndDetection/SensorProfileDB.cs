using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class SensorProfileDB : BaseDataBlob
    {
        //Currently Unused.
        /*
        internal double GravSig
        {
            get
            {
                if (OwningEntity.HasDataBlob<MassVolumeDB>())
                    return OwningEntity.GetDataBlob<MassVolumeDB>().Mass;
                else
                    return 0;
            }
        }*/

        private double? _targetCrossSection;
        internal double TargetCrossSection
        {
            get
            {
                if (_targetCrossSection != null)
                    return (double)_targetCrossSection;
                else if (this.OwningEntity.HasDataBlob<MassVolumeDB>())
                    return Math.PI * Math.Pow(this.OwningEntity.GetDataBlob<MassVolumeDB>().RadiusInKM, 2);
                else throw new Exception("Parent Entity does not have an MassVolumeDB");
            }
        }

        //TODO make this a bit more complex so it reflects different amounts at different wavelengths
        //this will define how effective active sensors are, and will increase a ships detection when its closer to a star. 
        //key is frequency, value is 0.0-1.0 for that freqency. for most entites this will create a wave type spectrum. 
        //internal Dictionary<double, float> Reflectivity { get; private set; } = new Dictionary<double, float>();
        internal double Reflectivity;

        /// <summary>
        /// This dictionary gets replaced frequently by SetReflectedEMSig()
        /// </summary>
        /// <value>The reflected EMS pectra.</value>
        internal Dictionary<EMWaveForm, double> ReflectedEMSpectra { get; set; } = new Dictionary<EMWaveForm, double>();
        internal DateTime LastDatetimeOfReflectionSet = new DateTime();
        internal Vector4 LastPositionOfReflectionSet = new Vector4();


        /// <summary>
        /// Multiple Emissions make up the signature of the entity.
        /// the volume of each emission can increase and decrease, ie by running engines for movement, or active sensors. 
        /// </summary>
        /// <key>defines the average and dropout wavelengths in nanometers</key>
        /// <value>the volume or magnatude of the spectra</value>
        internal Dictionary<EMWaveForm, double> EmittedEMSpectra { get; } = new Dictionary<EMWaveForm, double>();

        public SensorProfileDB() { }

        public SensorProfileDB(SensorProfileDB db)
        {
            EmittedEMSpectra = new Dictionary<EMWaveForm, double>(db.EmittedEMSpectra);
            ReflectedEMSpectra = new Dictionary<EMWaveForm, double>(db.ReflectedEMSpectra);
            _targetCrossSection = db._targetCrossSection;
        }

        public override object Clone()
        {
            return new SensorProfileDB(this);
        }



    }
}
