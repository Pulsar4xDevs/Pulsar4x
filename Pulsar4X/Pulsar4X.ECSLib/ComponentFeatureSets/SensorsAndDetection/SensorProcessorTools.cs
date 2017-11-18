using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{

    class SensorInfo
    {
        internal Entity detectedEntity;
        internal DateTime LastDetection;
    }

    public static class SensorProcessorTools
    {


        internal static void DetectEntites(SensorReceverAtbDB receverDB, SensorProfileDB sensorProfile, DateTime atDate)
        {
            var knownContacts = receverDB.KnownSensorContacts;

            TimeSpan timeSinceLastCalc = atDate - sensorProfile.LastDatetimeOfReflectionSet;
            PositionDB positionOfSensorProfile = sensorProfile.OwningEntity.GetDataBlob<PositionDB>();//sensorProfile.OwningEntity.GetDataBlob<ComponentInstanceInfoDB>().ParentEntity.GetDataBlob<PositionDB>();
            double distanceInAUSinceLastCalc = PositionDB.GetDistanceBetween(sensorProfile.LastPositionOfReflectionSet, positionOfSensorProfile);
            if (timeSinceLastCalc > TimeSpan.FromMinutes(30) || distanceInAUSinceLastCalc > 0.1) //TODO: move the time and distance numbers here to settings?
                SetReflectedEMProfile.SetEntityProfile(sensorProfile.OwningEntity, atDate);

            if (IsDetected(receverDB, sensorProfile))
            {
                if (knownContacts.ContainsKey(sensorProfile.OwningEntity.Guid))
                {
                    knownContacts[sensorProfile.OwningEntity.Guid].LastDetection = atDate;
                }
                else
                {
                    knownContacts.Add(sensorProfile.OwningEntity.Guid, new SensorInfo()
                    {
                        detectedEntity = sensorProfile.OwningEntity,
                        LastDetection = atDate
                    });
                }
            }
        }



        //eventualy not a bool, return more info than a bool. (a percentage? or even more info than that?) 
        internal static bool IsDetected(SensorReceverAtbDB recever, SensorProfileDB target)
        {
            /*
             * Thoughts:
             * 
             * What we need:
             * detect enough of a signal to get a position 
             * decide what "enough" is. probibly get this from the signal strength. - should the target SensorSigDB define what enough is?
             * we could require more than one detection (ie two ships in different locations) to get an acurate position, but that could get tricky to code. 
             * and how would we display a non acurate position? maybe a line out to a question mark, showing the angle of detection but not range?
             * 
             * detect enough of a signal to get intel if it's a ship
             * decide what "enough" for this is. maybe compare the detected waveform and the emited waveform and compare the angles to see if the triangle is simular. 
             * 
             * 
             * With range attenuation, we'll never get the full signal uneless we're right ontop of it. 
             * maybe if we get half the emited strength and its a simular triange (all same angles) we get "Full" intel?
             * 
             * should we add time into the mix as well? multiple detections over a given time period to get position/velocity/orbitDB?
             * 
             * 
             * how are multiple components on a ship going to work? they are entitys in and of themselfs, so they could have a SensorSigDB all of thier own.
             * that could help with getting intel on individual components of a target. 
             * 
             * recever resolution should play into how much gets detected. 
             * 
             * Note that each entity will(may) have multiple waveforms. 
             * 
             * Data that can be glened from this detection system:
             * detectedStrength (altitide of the intersecting triangle)
             * detectedArea - the area of the detected intersection, could compare this to the target signal as well. 
             * compare angles of the detected intersection and the target signal to see if the shape is simular. 
             * if range is known acurately, this could affect the intel gathered. 
             */
            var myPosition = recever.OwningEntity.GetDataBlob<ComponentInstanceInfoDB>().ParentEntity.GetDataBlob<PositionDB>();//recever is a componentDB. not a shipDB
            PositionDB targetPosition;
            if( target.OwningEntity.HasDataBlob<PositionDB>()) 
                targetPosition = target.OwningEntity.GetDataBlob<PositionDB>();
            else 
                targetPosition = target.OwningEntity.GetDataBlob<ComponentInstanceInfoDB>().ParentEntity.GetDataBlob<PositionDB>();//target may be a componentDB. not a shipDB
            double distance = PositionDB.GetDistanceBetween(myPosition, targetPosition);

            var detectionResolution = recever.Resolution;

            var signalAtPosition = AttenuatedForDistance(target, distance);

            double receverSensitivityFreqMin = recever.RecevingWaveformCapabilty.WavelengthMin;
            double receverSensitivityFreqAvg = recever.RecevingWaveformCapabilty.WavelengthAverage;
            double receverSensitivityFreqMax = recever.RecevingWaveformCapabilty.WavelengthMax;
            double receverSensitivityAltitiude = recever.MaxSensitivity - recever.MinSensitivity;

            foreach (var waveSpectra in signalAtPosition)
            {
                double signalWaveSpectraFreqMin = waveSpectra.Key.WavelengthMin;
                double signalWaveSpectraFreqAvg = waveSpectra.Key.WavelengthAverage;
                double signalWaveSpectraFreqMax = waveSpectra.Key.WavelengthMax;
                double signalWaveSpectraAltitide = waveSpectra.Value;



                if (signalWaveSpectraAltitide > recever.MinSensitivity) //check if the sensitivy is enough to pick anything up at any frequency. 
                {
                    if (receverSensitivityFreqMin < signalWaveSpectraFreqMin //may need to check I'm not missing edge cases here. 
                       && receverSensitivityFreqMax < signalWaveSpectraFreqMin)
                    {
                        //we've got something we can detect
                        var minDetectableWavelength = signalWaveSpectraFreqMin;
                        var maxDetectableWavelenght = Math.Min(receverSensitivityFreqMax, signalWaveSpectraFreqMax);

                        double angleA = Math.Atan(receverSensitivityAltitiude / receverSensitivityFreqAvg - receverSensitivityFreqMin );
                        double sideL = maxDetectableWavelenght - minDetectableWavelength;
                        double angleB = Math.Atan(signalWaveSpectraAltitide / signalWaveSpectraFreqAvg - signalWaveSpectraFreqMax);

                        double detectedStrength = ((Math.Sin(angleA) * Math.Sin(angleB)) / (Math.Sin(angleA) + Math.Sin(angleB))) * sideL;
                        double detectedArea = detectedStrength * sideL / 2;

                        double signalAngleA = angleB;
                        double signalAngleB = Math.Atan(signalWaveSpectraAltitide / signalWaveSpectraFreqAvg - signalWaveSpectraFreqMin);
                        double signalAngleC = 180 - signalAngleA - signalAngleB;
                        return true; //just return true for now, expand on the above later. 
                    }
                }
            }
            return false; //we didn't detect it at all. 
        }

        /// <summary>
        /// returns a dictionary of all emmisions including reflected emmisions. 
        /// </summary>
        /// <returns>The for distance.</returns>
        /// <param name="emission">Emission.</param>
        /// <param name="distance">Distance.</param>
        internal static Dictionary<EMWaveForm, double> AttenuatedForDistance(SensorProfileDB emission, double distance)
        {
            var dict = new Dictionary<EMWaveForm, double>();

            foreach (var emitedItem in emission.EmittedEMSpectra)
            {
                dict.Add(emitedItem.Key, emitedItem.Value / 4 * Math.PI * Math.Pow(distance, 2));
            }
            foreach (var reflectedItem in emission.ReflectedEMSpectra)
            {
                if (dict.ContainsKey(reflectedItem.Key))
                {
                    var reflectedSpectra = reflectedItem.Value / 4 * Math.PI * Math.Pow(distance, 2) * emission.TargetCrossSection;
                    dict.Add(reflectedItem.Key, reflectedSpectra);
                }
                else {
                    //this shouldn't happen
                }
            }
            return dict;
        }



        /// <summary>
        /// Probibly only needs to be done at star creation, unless we do funky stuff like change a stars temprature and stuff. 
        /// </summary>
        /// <returns>The star emmision sig.</returns>
        /// <param name="starInfoDB">Star info db.</param>
        /// <param name="starMassVolumeDB">Star mass volume db.</param>
        internal static SensorProfileDB SetStarEmmisionSig(StarInfoDB starInfoDB, MassVolumeDB starMassVolumeDB)
        {

            var tempDegreesC = starInfoDB.Temperature;
            var kelvin = tempDegreesC + 273.15;
            var wavelength = 2.9 * Math.Pow(10, 6) / kelvin;
            var magnitude = tempDegreesC / starMassVolumeDB.Volume; //maybe this should be lum / volume?
            EMWaveForm waveform = new EMWaveForm()
            {
                WavelengthAverage = wavelength,
                WavelengthMin = wavelength - 300, //3k angstrom, semi arbitrary number pulled outa my ass from 10min of internet research. 
                WavelengthMax = wavelength + 600
            };

            var emisionSignature = new SensorProfileDB() {
                
            };
            emisionSignature.EmittedEMSpectra.Add(waveform, magnitude);// this will need adjusting...

            return emisionSignature;
        }

        /// <summary>
        /// probibly only needs to be done at entity creation, once the bodies mass is set.
        /// </summary>
        /// <returns>The emmision sig.</returns>
        /// <param name="sysBodyInfoDB">Sys body info db.</param>
        /// <param name="massVolDB">Mass vol db.</param>
        internal static void PlanetEmmisionSig(SensorProfileDB profile, SystemBodyInfoDB sysBodyInfoDB, MassVolumeDB massVolDB, AtmosphereDB atmoDB)
        {
            var tempDegreesC = sysBodyInfoDB.BaseTemperature;
            var kelvin = tempDegreesC + 273.15;
            var wavelength = 2.9 * Math.Pow(10, 6) / kelvin;
            var magnitude = tempDegreesC / massVolDB.Volume;
            EMWaveForm waveform = new EMWaveForm()
            {
                WavelengthAverage = wavelength,
                WavelengthMin = wavelength - 400, //4k angstrom, semi arbitrary number pulled outa my ass from 0min of internet research. 
                WavelengthMax = wavelength + 600
            };

            profile.EmittedEMSpectra.Add(waveform, magnitude);//TODO this may need adjusting to make good balanced detections.
            profile.Reflectivity = atmoDB.Albedo;
        }



    }


    /// <summary>
    /// EM waveform, think of this of this as a triange wave, the average is the peak, with min and max defining the minimuam and maximium wavelengths 
    ///     . ---Height (volume) is changable and defined in the SensorSigDB.EMSig value. increasing the hight doesn't make it more detecable by sensors outside the min and max range 
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
        public double WavelengthAverage;
        /// <summary>
        /// The min wavelength this will no longer emit at in nm
        /// </summary>
        public double WavelengthMin;
        /// <summary>
        /// The max wavelength this will no longer emit at in nm
        /// </summary>
        public double WavelengthMax;
    }

    public class SensorTransmitterDB : BaseDataBlob
    {
        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }

    public class FactionSensorInfoDB
    {
        Dictionary<Entity,EntityKnowledge> knownEntitys;
    }

    public struct EntityKnowledge
    {
        Entity entity;
        List<BaseDataBlob> knownDatablobs;
    }
}
