using System.Collections.Generic;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Movement;
using Pulsar4X.Sensors;

namespace Pulsar4X.Energy;
using System;
using Pulsar4X.Orbital;
using Pulsar4X.Interfaces;
using Pulsar4X.Engine;


public class EnergyGenHotloopProcessor : IHotloopProcessor
{
    public TimeSpan RunFrequency => TimeSpan.FromHours(1);

    public TimeSpan FirstRunOffset => TimeSpan.FromSeconds(1);

    public Type GetParameterType => typeof(EnergyGenAbilityDB);
    public void Init(Game game)
    {
    }
    public int ProcessManager(EntityManager manager, int deltaSeconds)
    {
        var entities = manager.GetAllEntitiesWithDataBlob<EnergyGenAbilityDB>();
        foreach (var entity in entities)
        {
            ProcessEntity(entity, deltaSeconds);
        }
        return entities.Count;
    }
    
    public void ProcessEntity(Entity entity, int deltaSeconds)
    {
        EnergyGenAbilityDB _energyGenDB = entity.GetDataBlob<EnergyGenAbilityDB>();
        _energyGenDB.MaxOutputFromSolar = ComputeSolarMax(entity);
    }
    
    private static double ComputeSolarMax(Entity entity)
    {
        double totalSolar = 0;
        var genDB = entity.GetDataBlob<EnergyGenAbilityDB>();

        var position = entity.GetDataBlob<PositionDB>();
        var stars = entity.Manager.GetAllEntitiesWithDataBlob<SensorProfileDB>();

        foreach (var panelAtb in genDB.SolarPanels)
        {
            double panelAbsorbed = 0;
            foreach (var star in stars)
            {
                var starProfile = star.GetDataBlob<SensorProfileDB>();
                var starPos = star.GetDataBlob<PositionDB>();
                double distance = Vector3.Distance(position.AbsolutePosition, starPos.AbsolutePosition);
                if (distance <= 0) continue;

                var attenuated = SensorTools.AttenuatedForDistance(starProfile, distance);
                panelAbsorbed += AbsorbedPower(panelAtb, attenuated); // Reuse/adapt DetectonQuality
            }
            totalSolar += panelAbsorbed;
        }
        return totalSolar;
    }
    
    /// <summary>
    /// Calculates the absorbed power for a solar panel based on attenuated star emissions.
    /// Adapts DetectonQuality logic: computes overlap, interpolates efficiency (higher better),
    /// and multiplies attenuated magnitude by efficiency for absorbed kW.
    /// </summary>
    /// <param name="panelAtb">The solar panel attribute with waveform and efficiencies.</param>
    /// <param name="attenuatedSignal">Attenuated emissions from star (waveform to magnitude in kW).</param>
    /// <returns>Total absorbed power in kW.</returns>
    public static double AbsorbedPower(EnergySolarGenerationAtb panelAtb, Dictionary<EMWaveForm, double> attenuatedSignal)
    {
        double totalAbsorbed = 0.0;

        var sol = SolarFraction(panelAtb, attenuatedSignal);

        totalAbsorbed = sol.SignalStrength_kW * sol.SignalQuality * panelAtb.Area_m2; 
        /*
            
            double magnitude = sol.SignalStrength_kW;

            

            // Falloff: normalized distance from panel peak (0 at peak, 1 at edges)
            double panelPeak = panelAtb.AbsorptionWaveformCapability.WavelengthAverage_nm;
            double overlapCenter = (minOverlap + maxOverlap) / 2.0;
            double distFromPeak = Math.Abs(overlapCenter - panelPeak);
            double halfBandwidth = (panelAtb.AbsorptionWaveformCapability.WavelengthMax_nm - panelAtb.AbsorptionWaveformCapability.WavelengthMin_nm) / 2.0;
            double falloff = halfBandwidth > 0 ? distFromPeak / halfBandwidth : 0.0;
            falloff = Math.Clamp(falloff, 0.0, 1.0);

            // Interpolate efficiency: Best at peak (falloff=0), Worst at edges (falloff=1)
            double interpolatedEff = panelAtb.BestEfficiency * (1 - falloff) + panelAtb.WorstEfficiency * falloff;

            // Absorbed: fraction of magnitude * efficiency
            totalAbsorbed += magnitude * overlapFraction * interpolatedEff;
        */

        return totalAbsorbed;
    }
    
        public static SensorReturnValues SolarFraction(EnergySolarGenerationAtb recever, Dictionary<EMWaveForm, double> signalAtPosition)
        {

            double receverSensitivityFreqMin = recever.AbsorptionWaveformCapability.WavelengthMin_nm;
            double receverSensitivityFreqAvg = recever.AbsorptionWaveformCapability.WavelengthAverage_nm;
            double receverSensitivityFreqMax = recever.AbsorptionWaveformCapability.WavelengthMax_nm;
            double receverSensitivityBest = recever.BestEfficiency;
            double receverSensitivityAltitiude = recever.BestEfficiency - recever.WorstEfficiency;
            PercentValue quality = new PercentValue(0.0f);
            double detectedMagnatude = 0;
            foreach (var waveSpectra in signalAtPosition)
            {
                double signalWaveSpectraFreqMin = waveSpectra.Key.WavelengthMin_nm;
                double signalWaveSpectraFreqAvg = waveSpectra.Key.WavelengthAverage_nm;
                double signalWaveSpectraFreqMax = waveSpectra.Key.WavelengthMax_nm;
                double signalWaveSpectraMagnatude_kW = waveSpectra.Value;
                
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
                        doesIntersect = SensorTools.Get_line_intersection(
                            signalWaveSpectraFreqAvg, signalWaveSpectraMagnatude_kW,
                            signalWaveSpectraFreqMin, 0,

                            receverSensitivityFreqAvg, recever.BestEfficiency,
                            receverSensitivityFreqMax, recever.WorstEfficiency,

                            out intersectPointX, out intersectPointY);
                        //offsetFromCenter = intersectPointX - signalWaveSpectraFreqAvg; //was going to use this for distortion but decided to simplify.
                        distortion = receverSensitivityFreqAvg - signalWaveSpectraFreqAvg;

                    }
                    else                                                        //LeftSideDetection
                    {
                        doesIntersect = SensorTools.Get_line_intersection(
                            signalWaveSpectraFreqAvg, signalWaveSpectraMagnatude_kW,
                            signalWaveSpectraFreqMax, 0,

                            receverSensitivityFreqAvg, recever.BestEfficiency,
                            receverSensitivityFreqMin, recever.WorstEfficiency,

                            out intersectPointX, out intersectPointY);
                        //offsetFromCenter = intersectPointX - signalWaveSpectraFreqAvg;
                        distortion = signalWaveSpectraFreqAvg - receverSensitivityFreqAvg;

                    }

                    if (doesIntersect) // then we're not detecting the peak of the signal
                    {
                        detectedMagnatude = intersectPointY - recever.BestEfficiency;
                        distortion *= 2; //pentalty to quality of signal
                    }
                    else
                        detectedMagnatude = signalWaveSpectraMagnatude_kW - recever.BestEfficiency;

                    quality = new PercentValue((float)(100 - distortion / signalWaveSpectraFreqMax));

                }
            }
            return new SensorReturnValues()
            {
                SignalStrength_kW = detectedMagnatude,
                SignalQuality = quality
            };
        }

}

