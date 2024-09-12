using System;
using System.Collections.Generic;
using System.Diagnostics;
using Pulsar4X.Orbital;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Extensions;

namespace Pulsar4X.Engine.Sensors
{

    public static class SensorTools
    {

        public static SensorReturnValues[] GetDetectedEntites(SensorReceiverAtbDB sensorAtb, Vector3 position, List<Entity> detectableEntities, DateTime atDate, int factionOwnerId,  bool filterSameFaction = true)
        {
            SensorReturnValues[] detectionValues = new SensorReturnValues[detectableEntities.Count];
            for (int i = 0; i < detectableEntities.Count; i++)
            {
                var detectableEntity = detectableEntities[i];
                if (filterSameFaction && detectableEntity.FactionOwnerID == factionOwnerId)
                    continue;
                else
                {
                    if(!detectableEntity.TryGetDatablob<PositionDB>(out var detectablePosDB))
                    {
                        continue;
                    }

                    //TODO: check if the below actualy saves us anything. it might be better just to seperatly loop through each of the entites and set the reflection profiles every so often..
                    // TimeSpan timeSinceLastCalc = atDate - detectableProfile.LastDatetimeOfReflectionSet;
                    // double distanceSinceLastCalc = detectablePosDB.GetDistanceTo_m(detectableProfile.LastPositionOfReflectionSet);
                    // if (timeSinceLastCalc > TimeSpan.FromMinutes(30) || distanceSinceLastCalc > 5000) //TODO: move the time and distance numbers here to settings?
                    //     SetReflectedEMProfile.SetEntityProfile(detectableEntity, atDate);

                    var detectableProfile = detectableEntity.GetDataBlob<SensorProfileDB>();
                    var distance = detectablePosDB.GetDistanceTo_m(position);
                    var attentuatedSignal = AttenuatedForDistance(detectableProfile, distance);
                    SensorReturnValues detectionValue = DetectonQuality(sensorAtb, attentuatedSignal);
                    //if(detectionValue.SignalStrength_kW > 0)
                        detectionValues[i] = detectionValue;
                }
            }

            return detectionValues;
        }

        /*
        internal static void DetectEntites(SystemSensorContacts sensorMgr, FactionInfoDB factionInfo, PositionDB receverPos, SensorReceiverAtbDB receverDB, Entity detectableEntity, DateTime atDate)
        {
            Entity receverFaction = sensorMgr.FactionId;
            //Entity receverFaction;// = receverDB.OwningEntity.GetDataBlob<OwnedDB>().OwnedByFaction;
            //detectableEntity.Manager.FindEntityByGuid(receverDB.OwningEntity.FactionOwner, out receverFaction);
            var knownContacts = factionInfo.SensorContacts; //receverFaction.GetDataBlob<FactionInfoDB>().SensorEntites;
            var knownContacts1 = sensorMgr.GetAllContacts();


            SensorProfileDB sensorProfile = detectableEntity.GetDataBlob<SensorProfileDB>();

            TimeSpan timeSinceLastCalc = atDate - sensorProfile.LastDatetimeOfReflectionSet;
            PositionDB positionOfSensorProfile = detectableEntity.GetDataBlob<PositionDB>();//sensorProfile.OwningEntity.GetDataBlob<ComponentInstanceInfoDB>().ParentEntity.GetDataBlob<PositionDB>();
            double distanceSinceLastCalc = sensorProfile.LastPositionOfReflectionSet.GetDistanceTo_m(positionOfSensorProfile);

            //Only set the reflectedEMProfile of the target if it's not been done recently:
            //TODO: is this still neccicary now that I've found and fixed the loop? (refelctions were getting bounced around)
            if (timeSinceLastCalc > TimeSpan.FromMinutes(30) || distanceSinceLastCalc > 5000) //TODO: move the time and distance numbers here to settings?
                sensorProfile.SetReflectionProfile(atDate);



            PositionDB targetPosition;
            if (detectableEntity.HasDataBlob<PositionDB>())
                targetPosition = detectableEntity.GetDataBlob<PositionDB>();
            else throw new Exception("This Object does not have a position.");

            var distance = receverPos.GetDistanceTo_m(targetPosition);
            SensorReturnValues detectionValues = DetectonQuality(receverDB, AttenuatedForDistance(sensorProfile, distance));
            SensorInfoDB sensorInfo;
            if (detectionValues.SignalStrength_kW > 0.0)
            {
                if (sensorMgr.SensorContactExists(detectableEntity.Id))
                {
                    //sensorInfo = knownContacts[detectableEntity.ID].GetDataBlob<SensorInfoDB>();
                    sensorInfo = sensorMgr.GetSensorContact(detectableEntity.Id).SensorInfo;
                    sensorInfo.LatestDetectionQuality = detectionValues;
                    sensorInfo.LastDetection = atDate;
                    if (sensorInfo.HighestDetectionQuality.SignalQuality < detectionValues.SignalQuality)
                        sensorInfo.HighestDetectionQuality.SignalQuality = detectionValues.SignalQuality;

                    if (sensorInfo.HighestDetectionQuality.SignalStrength_kW < detectionValues.SignalStrength_kW)
                        sensorInfo.HighestDetectionQuality.SignalStrength_kW = detectionValues.SignalStrength_kW;
                    SensorEntityFactory.UpdateSensorContact(receverFaction, sensorInfo);
                }
                else
                {
                    SensorContact contact = new SensorContact(receverFaction, detectableEntity, atDate);
                    sensorMgr.AddContact(contact);


                    //knownContacts.Add(detectableEntity.ID, SensorEntityFactory.UpdateSensorContact(receverFaction, sensorInfo)); moved this line to the SensorInfoDB constructor
                }

            }
        }
        */

        public static SensorReturnValues DetectonQuality(SensorReceiverAtbDB recever, Dictionary<EMWaveForm, double> signalAtPosition)
        {
            /*
             * Thoughts (spitballing):
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
             * it'd be nifty if we could include background noise in there too, ie so ships close to a sun would be hidden.
             * also have resoulution be required to pick out multiple ships close together instead of just one big signal.
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
             * compare angles of the detected intersection and the target signal to see if the shape is simular?
             * if range is known acurately, this could affect the intel gathered.
             */

            /*
            var myPosition = recever.OwningEntity.GetDataBlob<ComponentInstanceInfoDB>().ParentEntity.GetDataBlob<PositionDB>();//recever is a componentDB. not a shipDB
            if (myPosition == null) //then it's probilby a colony
                myPosition = recever.OwningEntity.GetDataBlob<ComponentInstanceInfoDB>().ParentEntity.GetDataBlob<ColonyInfoDB>().PlanetEntity.GetDataBlob<PositionDB>();
            PositionDB targetPosition;
            if( target.OwningEntity.HasDataBlob<PositionDB>())
                targetPosition = target.OwningEntity.GetDataBlob<PositionDB>();
            else
                targetPosition = target.OwningEntity.GetDataBlob<ComponentInstanceInfoDB>().ParentEntity.GetDataBlob<PositionDB>();//target may be a componentDB. not a shipDB
            double distance = PositionDB.GetDistanceBetween(myPosition, targetPosition);

            var detectionResolution = recever.Resolution;

            var signalAtPosition = AttenuatedForDistance(target, distance);
*/
            double receverSensitivityFreqMin = recever.RecevingWaveformCapabilty.WavelengthMin_nm;
            double receverSensitivityFreqAvg = recever.RecevingWaveformCapabilty.WavelengthAverage_nm;
            double receverSensitivityFreqMax = recever.RecevingWaveformCapabilty.WavelengthMax_nm;
            double receverSensitivityBest = recever.BestSensitivity_kW;
            double receverSensitivityAltitiude = recever.BestSensitivity_kW - recever.WorstSensitivity_kW;
            PercentValue quality = new PercentValue(0.0f);
            double detectedMagnatude = 0;
            foreach (var waveSpectra in signalAtPosition)
            {
                double signalWaveSpectraFreqMin = waveSpectra.Key.WavelengthMin_nm;
                double signalWaveSpectraFreqAvg = waveSpectra.Key.WavelengthAverage_nm;
                double signalWaveSpectraFreqMax = waveSpectra.Key.WavelengthMax_nm;
                double signalWaveSpectraMagnatude_kW = waveSpectra.Value;



                if (signalWaveSpectraMagnatude_kW > recever.BestSensitivity_kW) //check if the sensitivy is enough to pick anything up at any frequency.
                {
                    if (Math.Max(receverSensitivityFreqMin, signalWaveSpectraFreqMin) < Math.Max(signalWaveSpectraFreqMin, signalWaveSpectraFreqMax))
                    {
                        //we've got something we can detect
                        double minDetectableWavelength = Math.Min(receverSensitivityFreqMin, signalWaveSpectraFreqMin);
                        double maxDetectableWavelenght = Math.Min(receverSensitivityFreqMax, signalWaveSpectraFreqMax);

                        double detectedAngleA = Math.Atan(receverSensitivityAltitiude / (receverSensitivityFreqAvg - receverSensitivityFreqMin ));
                        double receverBaseLen = maxDetectableWavelenght - minDetectableWavelength;
                        double detectedAngleB = Math.Atan(signalWaveSpectraMagnatude_kW / (signalWaveSpectraFreqAvg - signalWaveSpectraFreqMax));

                        bool doesIntersect;
                        double intersectPointX;
                        double intersectPointY;
                        double distortion;

                        if (signalWaveSpectraFreqAvg < receverSensitivityFreqAvg)  //RightsideDetection (recever's ideal wavelenght is higher than the signal wavelenght at it's loudest)
                        {
                            doesIntersect = Get_line_intersection(
                                signalWaveSpectraFreqAvg, signalWaveSpectraMagnatude_kW,
                                signalWaveSpectraFreqMin, 0,

                                receverSensitivityFreqAvg, recever.BestSensitivity_kW,
                                receverSensitivityFreqMax, recever.WorstSensitivity_kW,

                                out intersectPointX, out intersectPointY);
                            //offsetFromCenter = intersectPointX - signalWaveSpectraFreqAvg; //was going to use this for distortion but decided to simplify.
                            distortion = receverSensitivityFreqAvg - signalWaveSpectraFreqAvg;

                        }
                        else                                                        //LeftSideDetection
                        {
                            doesIntersect = Get_line_intersection(
                                signalWaveSpectraFreqAvg, signalWaveSpectraMagnatude_kW,
                                signalWaveSpectraFreqMax, 0,

                                receverSensitivityFreqAvg, recever.BestSensitivity_kW,
                                receverSensitivityFreqMin, recever.WorstSensitivity_kW,

                                out intersectPointX, out intersectPointY);
                            //offsetFromCenter = intersectPointX - signalWaveSpectraFreqAvg;
                            distortion = signalWaveSpectraFreqAvg - receverSensitivityFreqAvg;

                        }

                        if (doesIntersect) // then we're not detecting the peak of the signal
                        {
                            detectedMagnatude = intersectPointY - recever.BestSensitivity_kW;
                            distortion *= 2; //pentalty to quality of signal
                        }
                        else
                            detectedMagnatude = signalWaveSpectraMagnatude_kW - recever.BestSensitivity_kW;

                        quality = new PercentValue((float)(100 - distortion / signalWaveSpectraFreqMax));

                    }
                }
            }



            return new SensorReturnValues()
            {
                SignalStrength_kW = detectedMagnatude,
                SignalQuality = quality
            };
        }


        /// <summary>
        /// Gets the line intersection.
        /// </summary>
        /// <returns><c>true</c>, if lines intesect, <c>false</c> otherwise.</returns>
        /// <param name="p0_x">P0 x.</param>
        /// <param name="p0_y">P0 y.</param>
        /// <param name="p1_x">P1 x.</param>
        /// <param name="p1_y">P1 y.</param>
        /// <param name="p2_x">P2 x.</param>
        /// <param name="p2_y">P2 y.</param>
        /// <param name="p3_x">P3 x.</param>
        /// <param name="p3_y">P3 y.</param>
        /// <param name="i_x">the x position of the intersection</param>
        /// <param name="i_y">the y position of the intersection</param>
        internal static bool Get_line_intersection(double p0_x, double p0_y, double p1_x, double p1_y,
            double p2_x, double p2_y, double p3_x, double p3_y, out double i_x, out double i_y)
        {
            double s1_x, s1_y, s2_x, s2_y;
            s1_x = p1_x - p0_x; s1_y = p1_y - p0_y;
            s2_x = p3_x - p2_x; s2_y = p3_y - p2_y;

            double s, t;
            s = (-s1_y * (p0_x - p2_x) + s1_x * (p0_y - p2_y)) / (-s2_x * s1_y + s1_x * s2_y);
            t = (s2_x * (p0_y - p2_y) - s2_y * (p0_x - p2_x)) / (-s2_x * s1_y + s1_x * s2_y);

            i_x = 0;
            i_y = 0;

            if (s >= 0 && s <= 1 && t >= 0 && t <= 1)
            {
                // Collision detected

                    i_x = p0_x + (t * s1_x);

                    i_y = p0_y + (t * s1_y);

                return true;
            }

            return false; // No collision
        }

        /// <summary>
        /// returns a dictionary of all emmisions including reflected emmisions.
        /// </summary>
        /// <returns>The for distance.</returns>
        /// <param name="emissionProfile">Emission.</param>
        /// <param name="distance">Distance.</param>
        public static Dictionary<EMWaveForm, double> AttenuatedForDistance(SensorProfileDB emissionProfile, double distance)
        {
            var dict = new Dictionary<EMWaveForm, double>();
            foreach (var emitedItem in emissionProfile.EmittedEMSpectra)
            {
                var powerAtDistance = AttenuationCalc(emitedItem.Value, distance);
                dict.Add(emitedItem.Key, powerAtDistance);
            }
            foreach (var reflectedItem in emissionProfile.ReflectedEMSpectra)
            {
                var reflectedValue = AttenuationCalc(reflectedItem.Value, distance);
                if(!dict.ContainsKey(reflectedItem.Key))
                    dict.Add(reflectedItem.Key, reflectedValue);
                else
                {
                    dict[reflectedItem.Key] += reflectedValue;
                }

            }
            return dict;
        }

        /// <summary>
        /// Power per unit of area.
        /// note that this is *not* a decebel mesurment, decebels are mesured logrithmicaly.
        /// </summary>
        /// <returns>souce / (4 pi r^2)</returns>
        /// <param name="sourceValue">Source value.</param>
        /// <param name="distance">Distance. must be > 0.3 or it'll just return the source value</param>
        public static double AttenuationCalc(double sourceValue, double distance)
        {
            // source / (4 pi r^2)
            if (distance < 1) //if distance is too small, 4 pi r^2 ends up being < 1
                distance = 1;

            // TODO: need to rebalance this
            // dividing by 4pi r^2 makes it incredibly hard to detect things from far away
            // even if they have large signatures. For example, using this formula the default
            // sensor on Earth doesn't detect Uranus or Neptune.
            //return sourceValue / (4 * Math.PI * distance * distance);
            return sourceValue;
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
            double b = 2898000; //Wien's displacement constant for nanometers.
            var wavelength = b / kelvin; //Wien's displacement law https://en.wikipedia.org/wiki/Wien%27s_displacement_law
            var magnitudeInKW = starInfoDB.Luminosity * 3.827e23; //tempDegreesC / starMassVolumeDB.Volume_km3; //maybe this should be lum / volume?

            //-300, + 600, semi arbitrary number pulled outa my ass from 10min of internet research.
            EMWaveForm waveform = new EMWaveForm(wavelength - 300, wavelength, wavelength + 600);


            var emisionSignature = new SensorProfileDB() {

            };
            emisionSignature.EmittedEMSpectra.Add(waveform, magnitudeInKW);// this will need adjusting...

            return emisionSignature;
        }

        /// <summary>
        /// probibly only needs to be done at entity creation, once the bodies mass is set.
        /// </summary>
        /// <returns>The emmision sig.</returns>
        /// <param name="sysBodyInfoDB">Sys body info db.</param>
        /// <param name="massVolDB">Mass vol db.</param>
        internal static void PlanetEmmisionSig(SensorProfileDB profile, SystemBodyInfoDB sysBodyInfoDB, MassVolumeDB massVolDB)
        {
            var tempDegreesC = sysBodyInfoDB.BaseTemperature;
            var kelvin = tempDegreesC + 273.15;
            double b = 2898000; //Wien's displacement constant for nanometers.
            var wavelength = b / kelvin; //Wien's displacement law https://en.wikipedia.org/wiki/Wien%27s_displacement_law


            var cop = 5.670373E-8;
            var emisivity = 1 - sysBodyInfoDB.Albedo;
            var j = emisivity * cop * Math.Pow(kelvin, 4);
            var surfaceArea = 4 * Math.PI * massVolDB.RadiusInM * massVolDB.RadiusInM;
            var magnitude = j * surfaceArea * 0.001;

            //-400 & +600, semi arbitrary number pulled outa my ass from 0min of internet research.
            EMWaveForm waveform = new EMWaveForm(wavelength - 400, wavelength, wavelength + 600);


            profile.EmittedEMSpectra.Add(waveform, magnitude);//TODO this may need adjusting to make good balanced detections.
            profile.Reflectivity = sysBodyInfoDB.Albedo;
        }


        /// <summary>
        /// TODO: Refactor: each entity (or parent) should have thier own Random based off a seed.
        /// all random should be psudo random and threadsafe. or at least, we need to be aware of higher level randoms which can be called by any thread. ie avoid this.
        /// some random should be able to be figured out by remote clients, and some not.
        /// </summary>
        /// <returns>The sigmoid.</returns>
        /// <param name="acurateNumber">Acurate number.</param>
        /// <param name="acuracy">Acuracy.</param>
        public static double RndSigmoid(double acurateNumber, PercentValue acuracy, Random rng)
        {
            double sigmoid = Math.Tanh(acuracy * 10);
            double maxRand = Rnd(1, sigmoid, rng);
            double result = Rnd(acurateNumber + (acurateNumber * maxRand), acurateNumber - (acurateNumber * maxRand), rng);
            return result;
        }

        public static double Rnd(double max, double min, Random rng)
        {
            return rng.NextDouble() * (max - min) + max;
        }
    }
}
