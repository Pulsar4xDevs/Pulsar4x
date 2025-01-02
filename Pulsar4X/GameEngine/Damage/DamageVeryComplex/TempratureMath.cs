using System;
using System.Collections.Generic;
using Pulsar4X.Orbital;

namespace GameEngine.Damage;

public static class TempratureMath
{
    private static void TransferHeat(Particle from, Particle to, float deltaTime)
    {
        float deltaTemp = from.Temperature - to.Temperature;
        if (deltaTemp > 0)
        {
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
    }
    
    public static void TransferHeat(DamageMap damageMap, float timeStep )
    {
        float baseRadius = 0.1f * damageMap.Scale; // Base radius for 1 meter
        float heatTransferRadius = baseRadius * MathF.Sqrt(timeStep);
        heatTransferRadius = 1;
        for (int index = 0; index < damageMap.PMap.Length; index++)
        {
            Particle? particle = damageMap.PMap[index];
            if (particle != null)
            {
                List<Particle> neighbors = DamageMapHelpers.GetNeighboringParticles(damageMap, particle.Position, heatTransferRadius);

                foreach (var neighbor in neighbors)
                {
                    TransferHeat(particle, neighbor, timeStep);
                }
            }
        }

        for (int index = 0; index < damageMap.PMap.Length; index++)
        {
            Particle? particle = damageMap.PMap[index];
            var pressure = damageMap.PresMap[index];
            if(particle != null && pressure != null)
                particle.StateOfPhase = GetPhaseState(particle, pressure);
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="particle"></param>
    /// <param name="pressure"></param>
    /// <param name="temperature"></param>
    /// <returns></returns>
    public static PhaseState GetPhaseState(Particle particle, float pressure)
    {
        var zeroPoint = particle.MatType.MeltingZeroPoint;
        var criticalPoint = particle.MatType.CriticalPoint;
        var tripplePoint = particle.MatType.TriplePoint;
        var temperature = particle.Temperature;

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
    
    public static void PostCollisionTempratureChange(Particle particleA, Particle particleB, double initialkE, DamageMap map)
    {
        var m1 = particleA.Mass;
        var m2 = particleB.Mass;
        var vA = particleA.Velocity;
        var vB = particleB.Velocity;
        
        double finalKineticEnergy = 0.5f * m1 * vA.LengthSquared() + 0.5f * m2 * vB.LengthSquared();
        double energyToHeat = initialkE - finalKineticEnergy; // Ensure non-negative
        var totalMass = m1 + m2;
        // Distribute heat based on mass (more massive objects absorb more heat)
        double heatA = energyToHeat * (m1 / totalMass);
        double heatB = energyToHeat * (m2 / totalMass);

        // Convert energy to temperature increase
        float tempIncreaseA = (float)(heatA / (m1 * particleA.MatType.ThermalCapacity));
        float tempIncreaseB = (float)(heatB / (m2 * particleB.MatType.ThermalCapacity));

        // Ensure temperature increases are non-negative
        particleA.Temperature += tempIncreaseA; // Minimum temperature to avoid 0 in logs or divisions
        particleB.Temperature += tempIncreaseB;
    }

}