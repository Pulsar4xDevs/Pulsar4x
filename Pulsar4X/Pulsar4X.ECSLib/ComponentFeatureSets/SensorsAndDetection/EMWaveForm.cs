using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// EM waveform, think of this of this as a triange wave, the average is the peak, with min and max defining the minimuam and maximium wavelengths 
    ///     . ---Height (volume) is changable and defined in the SensorSigDB.EMSig value. increasing the hight doesn't make it more detecable by sensors outside the min and max range in KiloWatts
    ///    ....
    ///   .......
    ///  ..........
    ///..................
    /// |   |       |
    /// |   |       Max
    /// |   Average
    /// Min
    ///  
    /// 
    /// because we're using this as a dictionary key in some places, it needs to be a class instead of a struct, 
    /// or TODO: we should use and override IEquality and GetHashCode, and maybe use a struct as well.   
    /// </summary>
    public class EMWaveForm 
    {
        /// <summary>
        /// The wavelength average of this emmission in nm
        /// </summary>
        public readonly double WavelengthAverage_nm;
        /// <summary>
        /// The min wavelength this will no longer emit at in nm
        /// </summary>
        public readonly double WavelengthMin_nm;
        /// <summary>
        /// The max wavelength this will no longer emit at in nm
        /// </summary>
        public readonly double WavelengthMax_nm;


        public EMWaveForm(double min, double avg, double max)
        {
            WavelengthMin_nm = min;
            WavelengthAverage_nm = avg;
            WavelengthMax_nm = max;
        }
    }

    /*
    public class EntityWaveforms
    {
        List<EMWaveForm> waveforms;
        double Magnatude;
        string EntityName;
    }*/


}
