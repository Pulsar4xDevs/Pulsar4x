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
            totalSolar += panelAbsorbed * panelAtb.Area_m2;
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

        foreach (var kvp in attenuatedSignal)
        {
            EMWaveForm signalWave = kvp.Key;
            double magnitude = kvp.Value;

            // No overlap: skip
            double minOverlap = Math.Max(signalWave.WavelengthMin_nm, panelAtb.AbsorptionWaveformCapability.WavelengthMin_nm);
            double maxOverlap = Math.Min(signalWave.WavelengthMax_nm, panelAtb.AbsorptionWaveformCapability.WavelengthMax_nm);
            if (minOverlap >= maxOverlap) continue;

            // Overlap fraction
            double overlapWidth = maxOverlap - minOverlap;
            double signalWidth = signalWave.WavelengthMax_nm - signalWave.WavelengthMin_nm;
            double overlapFraction = overlapWidth / signalWidth;

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
        }

        return totalAbsorbed;
    }

}

