using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Pulsar4X.Components;
using Pulsar4X.Orbital;

namespace GameEngine.Damage;

public static class DamagePhysicsSim
{
    public static double FindFastestParticle(DamageMap map)
    {
        double mag = 0;
        foreach (var part in map.PMap)
        {
            if(part == null)
                continue;
            if( part.Velocity.Length() > mag)
                mag = part.Velocity.Length();
        }
        return mag;
    }
    
    public static void PhysicsLoop(DamageMap damageMap)
    {
        damageMap.TotalEnergy = CalculateTotalEnergy(damageMap);
        DamageMapHelpers.FindBadData(damageMap);    
        damageMap.FastestSpeed = FindFastestParticle(damageMap);
        float timeStep = (float)(damageMap.Scale / damageMap.FastestSpeed);
        List<Particle> movingParticles = new(); //we could be more memory efficent and performant if we used an array buffer here for these.
        List<(Particle, Particle)> collisions = new();
        // Collect all non-null and moving particles into a list
        for (int index = 0; index < damageMap.PMap.Length; index++)
        {
            Particle? particle = damageMap.PMap[index];
            if (particle != null && particle.Velocity.Length() > 0)
            {
                movingParticles.Add((particle));
            }
        }
        DamageMapHelpers.FindBadData(damageMap);
        // Update positions of all moving particles
        foreach (var particle in movingParticles)
        {
            UpdateParticlePosition(particle, damageMap.Scale, timeStep);
        }
        HandleOutOfBounds(damageMap, movingParticles);
        DamageMapHelpers.FindBadData(damageMap);    
        // Detect collisions for all particles
        foreach (var particle in movingParticles)
        {
            KineticMath.DetectCollision(particle, damageMap, collisions);
        }
        DamageMapHelpers.FindBadData(damageMap);   
        // Here you would typically resolve collisions after detection
        foreach (var partPair in collisions)
        {
            KineticMath.ResolveCollision(partPair.Item1, partPair.Item2, damageMap);
        }
         
        List<Particle> flatList = collisions.SelectMany(t => new[] { t.Item1, t.Item2 }).ToList();
        HandleOutOfBounds(damageMap, flatList);
        DamageMapHelpers.FindBadData(damageMap);    
        var mergedList = movingParticles.Union(flatList).ToList();
        foreach (var particle in mergedList)
        {
            UpdateParticleInMap(particle, damageMap);
        }
        
        PressureMath.UpdatePressureMap(damageMap);
        
        TempratureMath.TransferHeat(damageMap, timeStep);
        
        DamageMapHelpers.FindBadData(damageMap);    
    }

    public static void HandleOutOfBounds(DamageMap damageMap, List<Particle> particlesToCheck)
    {
        for (int index = 0; index < particlesToCheck.Count; index++)
        {
            var particle = particlesToCheck[index];
            if (IsOutOfBounds(particle, damageMap))
            {
                //todo check if move to sister map
                particlesToCheck.RemoveAt(index);
                if (damageMap.PMap[particle.mapIndex] == particle)
                {
                    damageMap.PMap[particle.mapIndex] = null;
                    particle.IsDeleted = true;
                    index--;
                }
                else
                {
                    var pindex = Array.IndexOf(damageMap.PMap, particle);
                    while (pindex != -1)
                    {
                        damageMap.PMap[pindex] = null;
                        pindex = Array.IndexOf(damageMap.PMap, particle);
                    }
                    particle.IsDeleted = true;
                    index--;
                }
            }
        }
    }


    public static void UpdateParticlePosition(Particle particle, int scale, float timeStep)
    {
        Vector2 movement = particle.Velocity * timeStep;
        particle.Position += movement / scale;
    }

    public static bool IsOutOfBounds(Particle particle, DamageMap damageMap)
    {
        int x = (int)Math.Round(particle.Position.X);
        int y = (int)Math.Round(particle.Position.Y);

        return x < 0 || y < 0 ||
               x >= damageMap.Width || y >= damageMap.Height;
    }
    
    // Helper method to update particle in map after position change
    private static void UpdateParticleInMap(Particle particle, DamageMap map)
    {
        int newX = (int)Math.Round(particle.Position.X);
        int newY = (int)Math.Round(particle.Position.Y);
        int newIndex = newY * map.Width + newX;
        int oldIndex = particle.mapIndex;
        if (newIndex != oldIndex && newIndex >= 0 && newIndex < map.PMap.Length)
        {
            if(map.PMap[newIndex] == null)
            {
                map.PMap[oldIndex] = null;
                map.PMap[newIndex] = particle;
            }
        }
    }
    
    public static double CalculateTotalEnergy(DamageMap damageMap)
    {
        double totalEnergy = 0;

        for (int index = 0; index < damageMap.PMap.Length; index++)
        {
            Particle? particle = damageMap.PMap[index];
            if (particle != null)
            {
                // Kinetic Energy: 0.5 * mass * velocity^2
                double kineticEnergy = 0.5 * particle.Mass * Math.Pow(particle.Velocity.Length(), 2);
            
                // Thermal Energy: Mass * Specific Heat Capacity * Temperature
                // Here, we'll use ThermalCapacity as the specific heat capacity for simplicity
                double thermalEnergy = particle.Mass * particle.MatType.ThermalCapacity * particle.Temperature;

                totalEnergy += kineticEnergy + thermalEnergy;
            }
        }

        return totalEnergy;
    }

    


}