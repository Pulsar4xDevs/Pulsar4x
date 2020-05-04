using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{

    public class SensorReceverAbility : ComponentAbilityState
    {
        [JsonProperty]
        public Dictionary<Guid, SensorProcessorTools.SensorReturnValues> CurrentContacts = new Dictionary<Guid, SensorProcessorTools.SensorReturnValues>();
        public Dictionary<Guid, SensorProcessorTools.SensorReturnValues> OldContacts = new Dictionary<Guid, SensorProcessorTools.SensorReturnValues>();

        public SensorReceverAbility(ComponentInstance componentInstance) : base(componentInstance)
        {
        }
    }

    public class SensorAbilityDB : BaseDataBlob
    {
        internal Dictionary<Guid, SensorProcessorTools.SensorReturnValues> CurrentContacts = new Dictionary<Guid, SensorProcessorTools.SensorReturnValues>();
        internal Dictionary<Guid, SensorProcessorTools.SensorReturnValues> OldContacts = new Dictionary<Guid, SensorProcessorTools.SensorReturnValues>();

        public SensorAbilityDB()
        {
        }

        public SensorAbilityDB(SensorAbilityDB db)
        {
            CurrentContacts = new Dictionary<Guid, SensorProcessorTools.SensorReturnValues>(db.CurrentContacts);
            OldContacts = new Dictionary<Guid, SensorProcessorTools.SensorReturnValues>(db.OldContacts);
        }

        public override object Clone()
        {
            return new SensorAbilityDB(this);
        }
    }

    public class SensorReceverAtbDB : BaseDataBlob, IComponentDesignAttribute
    {
        [JsonProperty]
        public EMWaveForm RecevingWaveformCapabilty { get; internal set; }
        
        /// <summary>
        /// Sensitivity at the ideal wavelength, lower is better, 0 is (imposible) best. should not be negitive. 
        /// </summary>
        [JsonProperty]
        public double BestSensitivity_kW { get; internal set; }//sensitivity at ideal wavelength
        
        /// <summary>
        /// The sensitivity at worst detectable wavelengths, lower is better, should be higher than BestSensitivity_kW
        /// </summary>
        [JsonProperty]
        public double WorstSensitivity_kW { get; internal set; } // sensitivity at worst detectable wavelengths
        /// <summary>
        /// In MegaPixels
        /// </summary>
        [JsonProperty]
        public float Resolution { get; internal set; } //will give more details on the target. low res will detect *something* but not *what*
        /// <summary>
        /// In Seconds
        /// </summary>
        [JsonProperty]
        public int ScanTime { get; internal set; } //the time it takes to complete a full 360 degree sweep. 
        //internal int Size; //basicly increases sensitivity at the cost of mass



        [JsonConstructor]
        public SensorReceverAtbDB() { }

        //ParserConstrutor
        /// <summary>
        /// 
        /// </summary>
        /// <param name="peakWaveLength">nm</param>
        /// <param name="bandwidth">nm</param>
        /// <param name="bestSensitivity">watts</param>
        /// <param name="worstSensitivity">watts</param>
        /// <param name="resolution">mp</param>
        /// <param name="scanTime">sec</param>
        public SensorReceverAtbDB(double peakWaveLength, double bandwidth, double bestSensitivity, double worstSensitivity, double resolution, double scanTime)
        {
            //TODO:  should make this component invalid. 
            if (bestSensitivity < 0)
            {
                var ev = new Event("Sensitivity is" + bestSensitivity + " *Must* be a positiveNumber Sensitivity is the kilowatt threshhold");
                StaticRefLib.EventLog.AddEvent(ev);
                bestSensitivity = 0;

            }
            if (bestSensitivity > worstSensitivity) 
            {
                var ev = new Event("bestSensitivity " + bestSensitivity + " *Must* be < than worstSensitivity" + worstSensitivity + 
                                   "(lower is better) Sensitivity is the kilowatt threshhold");
                StaticRefLib.EventLog.AddEvent(ev);
                worstSensitivity = bestSensitivity;
            }
            RecevingWaveformCapabilty = new EMWaveForm(peakWaveLength - bandwidth * 0.5,peakWaveLength, peakWaveLength + bandwidth * 0.5);
            BestSensitivity_kW = bestSensitivity * 0.001;
            WorstSensitivity_kW = worstSensitivity * 0.001;
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

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            //we're cloning the design to the instance here. when we do another pass on the sensors we'll likely change this. 
            if (!componentInstance.HasAblity<SensorReceverAbility>())
                componentInstance.SetAbilityState<SensorReceverAbility>(new SensorReceverAbility(componentInstance));//'this' should be the instance's designs db.
            if (!parentEntity.HasDataBlob<SensorAbilityDB>())
            {
                parentEntity.SetDataBlob(new SensorAbilityDB());
            }
            //SensorProcessorTools.(componentInstance);

        }
    }
}
