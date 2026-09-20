using System;
using System.Collections.Generic;
using System.Numerics;
using Pulsar4X.Engine;

namespace GameEngine.Damage;

public static class TempratureMath
{
    private static void TransferHeat(PhysicalParticle a, PhysicalParticle b, float deltaTime, int neighborCount)
    {
        PhysicalParticle from;
        PhysicalParticle to;
        if (a.Temperature == b.Temperature)
            return;
        if(a.Temperature > b.Temperature)
        {
            from = a;
            to = b;
        }
        else
        {
            from = b;
            to = a;
        }
        
        float deltaTemp = (from.Temperature - to.Temperature) / neighborCount;

        double distance = Vector2.Distance(from.Position, to.Position);
    
        // Calculate average thermal conductivity
        float avgConductivity = (from.MatType.ThermalConductivity + to.MatType.ThermalConductivity) / 2;

        // Calculate the total energy available for transfer (in J)
        double totalEnergyToTransfer = deltaTemp * from.Mass * from.MatType.ThermalCapacity;

        // Time constant for exponential decay of heat transfer rate
        float timeConstant = (float)(distance * from.Mass * from.MatType.ThermalCapacity / avgConductivity); // Approximate time for energy to transfer

        // Calculate energy transfer with decay over time
        double energyTransfer = totalEnergyToTransfer * (1 - Math.Exp(-deltaTime / timeConstant));

        // Cap energy transfer to what's physically available
        energyTransfer = Math.Min(energyTransfer, totalEnergyToTransfer);

        // Convert energy transfer back to temperature change for both particles
        float tempChangeFrom = (float)(energyTransfer / (from.Mass * from.MatType.ThermalCapacity));
        float tempChangeTo = (float)(energyTransfer / (to.Mass * to.MatType.ThermalCapacity));

        // Apply temperature changes
        from.Temperature -= tempChangeFrom;
        to.Temperature += tempChangeTo;
        
    }
    
    public static void TransferHeat(DamageMap damageMap, float timeStep )
    {
        float baseRadius = 0.1f * damageMap.ParticlesPerMeter; // Base radius for 1 meter
        //float heatTransferRadius = baseRadius * MathF.Sqrt(timeStep);
        float heatTransferRadius = 1f;
        for (int index = 0; index < damageMap.PMap.Length; index++)
        {
            PhysicalParticle? particle = damageMap.PMap[index];
            if (particle != null)
            {
                List<PhysicalParticle> neighbors = DamageMapHelpers.GetNeighboringParticles(damageMap, particle.Position, heatTransferRadius);

                foreach (var neighbor in neighbors)
                {
                    TransferHeat(particle, neighbor, timeStep, neighbors.Count);
                }
            }
        }

        for (int index = 0; index < damageMap.PMap.Length; index++)
        {
            PhysicalParticle? particle = damageMap.PMap[index];
            var pressure = damageMap.PresMap[index];
            if(particle != null && pressure != null)
                particle.StateOfPhase = GetPhaseState(particle, pressure);
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="physicalParticle"></param>
    /// <param name="pressure"></param>
    /// <param name="temperature"></param>
    /// <returns></returns>
    public static PhaseState GetPhaseState(PhysicalParticle physicalParticle, float pressure)
    {
        var zeroPoint = physicalParticle.MatType.MeltingZeroPoint;
        var criticalPoint = physicalParticle.MatType.CriticalPoint;
        var tripplePoint = physicalParticle.MatType.TriplePoint;
        var temperature = physicalParticle.Temperature;

        var state = PhaseState.Solid;
        if (temperature > zeroPoint)
        {
            if (temperature < tripplePoint.Kelvin)
            {
                if (pressure < tripplePoint.Bar) //sublimation
                    state = PhaseState.Gas;
                else 
                    state = PhaseState.Solid;
            }
            else if(temperature < (criticalPoint.Kelvin * 0.5))
            {
                if(pressure > criticalPoint.Bar)
                    state = PhaseState.Solid;
                if(pressure < criticalPoint.Bar)
                    state = PhaseState.Liquid;
                if(pressure < tripplePoint.Bar)
                    state = PhaseState.Gas;
            }
            else if (temperature < criticalPoint.Kelvin)
            {
                if(pressure > criticalPoint.Bar)
                    state = PhaseState.Liquid;
                else 
                    state = PhaseState.Gas;
            }
            else 
                state = PhaseState.Plasma;
        }
        
        return state;
    }
    
    public static void PostCollisionTempratureChange(PhysicalParticle physicalParticleA, PhysicalParticle physicalParticleB, double keDelta, DamageMap map)
    {
        var m1 = physicalParticleA.Mass;
        var m2 = physicalParticleB.Mass;
        

        var totalMass = m1 + m2;
        // Distribute heat based on mass (more massive objects absorb more heat)
        double heatA = keDelta * (m1 / totalMass);
        double heatB = keDelta * (m2 / totalMass);

        // Convert energy to temperature increase
        float tempIncreaseA = (float)(heatA / (m1 * physicalParticleA.MatType.ThermalCapacity));
        float tempIncreaseB = (float)(heatB / (m2 * physicalParticleB.MatType.ThermalCapacity));

        // Ensure temperature increases are non-negative
        physicalParticleA.Temperature += tempIncreaseA; // Minimum temperature to avoid 0 in logs or divisions
        physicalParticleB.Temperature += tempIncreaseB; 
    }
    /*
    public static void HandleBoilOff(DamageMap damageMap)
    {
        List<(Particle, Particle)> newParticles = new List<(Particle, Particle)>();

        for (int index = 0; index < damageMap.PMap.Length; index++)
        {
            Particle? particle = damageMap.PMap[index];
            if (particle != null && particle.StateOfPhase == PhaseState.Liquid)
            {
                if (IsBoiling(particle, damageMap.PresMap[index]))
                {
                    float boilOffAmount = CalculateBoilOffAmount(particle, damageMap.PresMap[index]);
                    if (boilOffAmount > 0)
                    {
                        Particle steamParticle = CreateSteamParticle(particle, boilOffAmount);
                        newParticles.Add((particle, steamParticle));
                    }
                }
            }
        }

        // Apply changes after calculating for all particles
        foreach (var (originalParticle, steamParticle) in newParticles)
        {
            originalParticle.Mass -= steamParticle.Mass;
            if (originalParticle.Mass <= 0)
            {
                damageMap.PMap[damageMap.GetIndex(originalParticle)] = null;
            }
            else
            {
                originalParticle.StateOfPhase = GetPhaseState(originalParticle, damageMap.PresMap[damageMap.GetIndex(originalParticle)], originalParticle.Temperature);
            }
            AddParticleToMap(damageMap, steamParticle);
        }

        // Update pressure after boil-off
        UpdatePressureMap(damageMap);
    }
*/
    private static bool IsBoiling(PhysicalParticle physicalParticle, float pressure)
    {
        // Check if we're above the boiling curve for this pressure
        if (pressure < physicalParticle.MatType.CriticalPoint.Bar && pressure > physicalParticle.MatType.TriplePoint.Bar)
        {
            // Linear interpolation between triple and critical point for boiling temperature
            float boilingTemperature = physicalParticle.MatType.TriplePoint.Kelvin + 
                ((pressure - physicalParticle.MatType.TriplePoint.Bar) / 
                (physicalParticle.MatType.CriticalPoint.Bar - physicalParticle.MatType.TriplePoint.Bar)) * 
                (physicalParticle.MatType.CriticalPoint.Kelvin - physicalParticle.MatType.TriplePoint.Kelvin);
            return physicalParticle.Temperature >= boilingTemperature;
        }
        // If pressure is at or above critical point, we might consider it a supercritical fluid, not boiling
        return false;
    }
/*
    private static float CalculateBoilOffAmount(Particle particle, float pressure)
    {
        float excessTemperature = particle.Temperature - IsBoiling(particle, pressure) ? 
                                  (particle.MatType.TriplePoint.Kelvin + 
                                   ((pressure - particle.MatType.TriplePoint.Bar) / 
                                    (particle.MatType.CriticalPoint.Bar - particle.MatType.TriplePoint.Bar)) * 
                                   (particle.MatType.CriticalPoint.Kelvin - particle.MatType.TriplePoint.Kelvin)) 
                                  : 0;
        float boilOffRate = excessTemperature * particle.Mass / pressure; // Simplified model
        return Math.Min(particle.Mass, boilOffRate);
    }
*/
}