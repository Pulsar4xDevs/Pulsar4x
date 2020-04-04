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
    /// or we could override the euality 
    /// or  we could not use it as a key. 
    /// </summary>
    public class EMWaveForm 
    {
        /// <summary>
        /// The wavelength average of this emmission in nm
        /// </summary>
        public double WavelengthAverage_nm;
        /// <summary>
        /// The min wavelength this will no longer emit at in nm
        /// </summary>
        public double WavelengthMin_nm;
        /// <summary>
        /// The max wavelength this will no longer emit at in nm
        /// </summary>
        public double WavelengthMax_nm;
        public EMWaveForm()
        {

        }
        public EMWaveForm(double _WavelengthAverage_nm, double _WavelengthMin_nm, double _WavelengthMax_nm)
        {
            WavelengthAverage_nm = _WavelengthAverage_nm;
            WavelengthMin_nm = _WavelengthMin_nm;
            WavelengthMax_nm = _WavelengthMax_nm;

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
