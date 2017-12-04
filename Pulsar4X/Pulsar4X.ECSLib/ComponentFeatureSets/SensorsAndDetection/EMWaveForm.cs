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
    /// </summary>
    public struct EMWaveForm
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
    }
}
