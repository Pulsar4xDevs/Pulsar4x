using System;
using System.Collections.Generic;
using System.Numerics;

namespace GameEngine.Damage;

public static class PressureMath
{
        public static void UpdatePressureMap(DamageMap damageMap)
    {
        float localDensity = 0;
        float pressureFactor = 0;
        for (int index = 0; index < damageMap.PMap.Length; index++)
        {
            PhysicalParticle? particle = damageMap.PMap[index];
            if (particle != null)
            {
                localDensity = CalculateLocalDensity(damageMap, particle.Position);
                pressureFactor = CalculatePressureFactor(particle, damageMap.PresMap[index]);

                // Adjust pressure based on local conditions
                damageMap.PresMap[index] += (localDensity * pressureFactor * particle.Mass) - damageMap.PresMap[index] * 0.1f; // Simple relaxation
            }
        }
    }
    private static float CalculateLocalDensity(DamageMap damageMap, Vector2 position)
    {
        float density = 0;
        float radius = 1.0f; // Define a radius for local density calculation
        List<PhysicalParticle> neighbors = DamageMapHelpers.GetNeighboringParticles(damageMap, position, radius);
    
        foreach (var neighbor in neighbors)
        {
            // Could get more complex, e.g., using distance for a smoother density fall-off
            density += neighbor.Mass;
        }
        return density / (MathF.PI * radius * radius); // Normalize by area for density
    }
    private static float CalculatePressureFactor(PhysicalParticle physicalParticle, float currentPressure)
    {
        float temperatureFactor = physicalParticle.Temperature / physicalParticle.MatType.MeltingZeroPoint; // Using melting point as a reference
        if (currentPressure > physicalParticle.MatType.TriplePoint.Bar)
        {
            // Here you could interpolate between triple and critical point for a more accurate model
            temperatureFactor = (physicalParticle.Temperature - physicalParticle.MatType.TriplePoint.Kelvin) / 
                                (physicalParticle.MatType.CriticalPoint.Kelvin - physicalParticle.MatType.TriplePoint.Kelvin);
        }
        else if (currentPressure < physicalParticle.MatType.TriplePoint.Bar)
        {
            // Below triple point, pressure might not affect phase transition as much
            temperatureFactor = Math.Max(0, temperatureFactor);
        }
        return temperatureFactor;
    }
}