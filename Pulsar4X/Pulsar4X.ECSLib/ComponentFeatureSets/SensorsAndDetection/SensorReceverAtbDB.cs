using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class SensorReceverAtbDB : BaseDataBlob
    {
        [JsonProperty]
        internal EMWaveForm RecevingWaveformCapabilty;
        [JsonProperty]
        internal double MaxSensitivity;//sensitivity at ideal wavelength
        [JsonProperty]
        internal double MinSensitivity;// sensitivity at worst detectable wavelengths
        [JsonProperty]
        internal float Resolution; //will give more details on the target. low res will detect *something* but not *what*
        [JsonProperty]
        internal int ScanTime; //the time it takes to complete a full 360 degree sweep. 
        //internal int Size; //basicly increases sensitivity at the cost of mass
        [JsonProperty]
        internal Dictionary<Guid, SensorInfo> KnownSensorContacts = new Dictionary<Guid, SensorInfo>();


        [JsonConstructor]
        public SensorReceverAtbDB() { }

        //ParserConstrutor
        public SensorReceverAtbDB(double peakWaveLength, double waveLengthWidth, double maxSensitivity, double minSensitivity, double resolution, double scanTime)
        {
            RecevingWaveformCapabilty = new EMWaveForm() { WavelengthMin = peakWaveLength - waveLengthWidth, WavelengthAverage = peakWaveLength, WavelengthMax = peakWaveLength + waveLengthWidth };
            MaxSensitivity = maxSensitivity;
            MinSensitivity = minSensitivity;
            Resolution = (float)resolution;
            ScanTime = (int)scanTime;
        }

        public SensorReceverAtbDB(SensorReceverAtbDB db)
        {
            RecevingWaveformCapabilty = db.RecevingWaveformCapabilty;
            MaxSensitivity = db.MaxSensitivity;
            MinSensitivity = db.MinSensitivity;
            Resolution = db.Resolution;
            ScanTime = db.ScanTime;
        }

        public override object Clone()
        {
            return new SensorReceverAtbDB(this);
        }
    }
}
