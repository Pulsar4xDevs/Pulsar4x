using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class SensorReceverAtbDB : BaseDataBlob, IComponentDesignAttribute
    {
        [JsonProperty]
        internal EMWaveForm RecevingWaveformCapabilty;
        [JsonProperty]
        /// <summary>
        /// Sensitivity at the ideal wavelength, lower is better, 0 is (imposible) best. should not be negitive. 
        /// </summary>
        internal double BestSensitivity_kW;//sensitivity at ideal wavelength
        [JsonProperty]
        /// <summary>
        /// The sensitivity at worst detectable wavelengths, lower is better, should be higher than BestSensitivity_kW
        /// </summary>
        internal double WorstSensitivity_kW;// sensitivity at worst detectable wavelengths
        [JsonProperty]
        internal float Resolution; //will give more details on the target. low res will detect *something* but not *what*
        [JsonProperty]
        internal int ScanTime; //the time it takes to complete a full 360 degree sweep. 
        //internal int Size; //basicly increases sensitivity at the cost of mass
        [JsonProperty]
        internal Dictionary<Guid, SensorInfoDB> KnownSensorContacts = new Dictionary<Guid, SensorInfoDB>();


        [JsonConstructor]
        public SensorReceverAtbDB() { }

        //ParserConstrutor
        public SensorReceverAtbDB(double peakWaveLength, double waveLengthWidth, double bestSensitivity, double worstSensitivity, double resolution, double scanTime)
        {
            //TODO:  should make this component invalid. 
            if (bestSensitivity > 0)
            {
                var ev = new Event("Sensitivity is" + bestSensitivity + " *Must* be a positiveNumber");
                StaticRefLib.EventLog.AddEvent(ev);
                bestSensitivity = 0;

            }
            if (bestSensitivity < worstSensitivity) 
            {
                var ev = new Event("bestSensitivity " + bestSensitivity + " *Must* be < than worstSensitivity" + worstSensitivity + "(lower is better)");
                StaticRefLib.EventLog.AddEvent(ev);
                worstSensitivity = bestSensitivity;
            }
            RecevingWaveformCapabilty = new EMWaveForm() { WavelengthMin_nm = peakWaveLength - waveLengthWidth, WavelengthAverage_nm = peakWaveLength, WavelengthMax_nm = peakWaveLength + waveLengthWidth };
            BestSensitivity_kW = bestSensitivity;
            WorstSensitivity_kW = worstSensitivity;
            Resolution = (float)resolution;
            ScanTime = (int)scanTime;


        }

        public SensorReceverAtbDB(SensorReceverAtbDB db)
        {
            RecevingWaveformCapabilty = db.RecevingWaveformCapabilty;
            BestSensitivity_kW = db.BestSensitivity_kW;
            WorstSensitivity_kW = db.WorstSensitivity_kW;
            Resolution = db.Resolution;
            ScanTime = db.ScanTime;
        }

        public override object Clone()
        {
            return new SensorReceverAtbDB(this);
        }

        public void OnComponentInstalation(Entity parentEntity, Entity componentInstance)
        {
            //we're cloning the design to the instance here. when we do another pass on the sensors we'll likely change this. 
            if (!componentInstance.HasDataBlob<SensorReceverAtbDB>())
                componentInstance.SetDataBlob(new SensorReceverAtbDB(this));//'this' should be the instance's designs db.
            //SensorProcessorTools.(componentInstance);

        }
    }
}
