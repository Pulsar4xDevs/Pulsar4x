using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using Pulsar4X.Datablobs;


namespace GameEngine.Damage;

public static class DamagePhysicsSim
{
    public static int runCount = 0;
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
        if(map.PhMap != null)
        {
            foreach (var part in map.PhMap)
            {
                if (part == null)
                    continue;
                if (part.Velocity.Length() > mag)
                    mag = part.Velocity.Length();
            }
        }
        return mag;
    }
    
    public static void PhysicsLoop(DamageMap damageMap)
    {
        runCount++;
        damageMap.TotalEnergy = CalculateTotalEnergy(damageMap);
        damageMap.FastestSpeed = FindFastestParticle(damageMap);
        float timeStep = (float)(damageMap.Scale / damageMap.FastestSpeed);
        timeStep = Math.Min(timeStep, 1.0f);
        List<IDamageParticle> movingParticles = new(); //we could be more memory efficent and performant if we used an array buffer here for these.
        List<(IDamageParticle, PhysicalParticle)> collisions = new();
        List<PhotonParticle> spawnerParticles = new(); // Spawner particles
        // Collect all non-null and moving particles into a list
        for (int index = 0; index < damageMap.PMap.Length; index++)
        {
            PhysicalParticle? particle = damageMap.PMap[index];
            if (particle != null && particle.Velocity.Length() > 0)
            {
                movingParticles.Add((particle));
            }
            PhotonParticle? phParticle = damageMap.PhMap[index];
            if (phParticle != null)
            {
                if (phParticle.IsSpawner)
                {
                    // Add spawners to the new list
                    spawnerParticles.Add(phParticle);
                }
                else if (phParticle.Velocity.Length() > 0) 
                {
                    // Add moving photons to the movingParticles list
                    movingParticles.Add(phParticle);
                }
            }
        }
        
        PhotonMath.HandleSpawners(spawnerParticles, damageMap, ref movingParticles, timeStep);

        // Update positions of all moving particles
        foreach (var particle in movingParticles)
        {
            UpdateParticlePosition(particle, damageMap.Scale, timeStep);
        }
        HandleOutOfBounds(damageMap, ref movingParticles);

        // Detect collisions for all particles
        foreach (var particle in movingParticles)
        {
            KineticMath.DetectCollision(particle, damageMap, collisions);
        }
   
        // Here you would typically resolve collisions after detection
        foreach (var partPair in collisions)
        {
            if(partPair.Item1 is PhysicalParticle && partPair.Item2 is PhysicalParticle)
                KineticMath.ResolveCollision((PhysicalParticle)partPair.Item1, partPair.Item2, damageMap);
            else if(partPair.Item1 is PhotonParticle)
                PhotonMath.ResolveCollision((PhotonParticle)partPair.Item1, (PhysicalParticle)partPair.Item2, damageMap);
        }
         
        List<IDamageParticle> flatList = collisions.SelectMany(t => new[] { t.Item1, t.Item2 }).ToList();
        HandleOutOfBounds(damageMap, ref flatList);
  
        var mergedList = movingParticles.Union(flatList).ToList();
        foreach (var particle in mergedList)
        {
            UpdateParticleInMap(particle, damageMap);
        }
        
        foreach (var particle in movingParticles)
        {
            if(particle is PhysicalParticle)
                KineticMath.ReAssessDetachmentWithNeighbors(damageMap, (PhysicalParticle)particle, 0.1f);
        }
        
        PressureMath.UpdatePressureMap(damageMap);
        
        TempratureMath.TransferHeat(damageMap, timeStep);
        
        damageMap.RunTime += TimeSpan.FromSeconds(timeStep);
        
        DamageMapHelpers.FindBadData(damageMap);    
    }
    
    
    public static void HandleOutOfBounds(DamageMap damageMap, ref List<IDamageParticle> particlesToCheck)
    {
        for (int index = 0; index < particlesToCheck.Count; index++)
        {
            var particle = particlesToCheck[index];
            if (IsOutOfBounds(particle, damageMap) && particle is PhysicalParticle)
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
            else if (IsOutOfBounds(particle, damageMap) && particle is PhotonParticle)
            {
                //todo check if move to sister map
                particlesToCheck.RemoveAt(index);
                if (damageMap.PhMap[particle.mapIndex] == particle)
                {
                    damageMap.PhMap[particle.mapIndex] = null;
                    particle.IsDeleted = true;
                    index--;
                }
                else
                {
                    var pindex = Array.IndexOf(damageMap.PhMap, particle);
                    while (pindex != -1)
                    {
                        damageMap.PhMap[pindex] = null;
                        pindex = Array.IndexOf(damageMap.PhMap, particle);
                    }
                    particle.IsDeleted = true;
                    index--;
                }
            }
            
        }
    }
    
    
    public static void UpdateComponetHealth(DamageMap map, ComponentInstancesDB instanceDB)
    {
        foreach (var component in map.componentData)
        {
            string instanceID = component.Key;
            ((int X,int Y) position, (int X,int Y) size, int totalParticles) = component.Value;
                  
            int undamagedParts = 0;

            // Count how many particles are destroyed or missing
            for (int y = position.Y; y < position.Y + size.Y; y++)
            {
                for (int x = position.X; x < position.X + size.X; x++)
                {
                    int index = map.GetIndex(x, y);
                    var particle = map.PMap[index];
                    if(particle is null)
                        continue;
                    if (particle.compID == map.compIDMap[index])
                    {
                        if (!particle.IsComponentPartDestroyed)
                        {
                            undamagedParts++;
                        }
                    }
                }
            }

            int destroyedParticles = totalParticles - undamagedParts;
            
            // Calculate new damage based on the number of destroyed or missing particles
            float percentHealth = (float)undamagedParts / totalParticles; 
            // Update the damage in componentInatance
            instanceDB.AllComponents[instanceID].HealthPercent = percentHealth;
        }
    }
    
    public static void UpdateParticlePosition(IDamageParticle particle, int scale, float timeStep)
    {
        Vector2 movement = particle.Velocity * timeStep;
        particle.Position += movement / scale;
    }

    public static bool IsOutOfBounds(IDamageParticle particle, DamageMap damageMap)
    {
        int x = (int)Math.Round(particle.Position.X);
        int y = (int)Math.Round(particle.Position.Y);

        return x < 0 || y < 0 ||
               x >= damageMap.Width || y >= damageMap.Height;
    }
    
    // Helper method to update particle in map after position change
    private static void UpdateParticleInMap(IDamageParticle particle, DamageMap map)
    {
        int newX = (int)Math.Round(particle.Position.X);
        int newY = (int)Math.Round(particle.Position.Y);
        int newIndex = map.GetIndex(newX, newY); 
        int oldIndex = particle.mapIndex;
        if (newIndex != oldIndex && newIndex >= 0 && newIndex < map.PMap.Length)
        {
            if(map.PMap[newIndex] == null)
            {
                map.PMap[oldIndex] = null;
                if(particle is PhysicalParticle)
                    map.PMap[newIndex] = (PhysicalParticle)particle;
                else if(particle is PhotonParticle)
                    map.PhMap[newIndex] = (PhotonParticle)particle;
            }
        }
    }
    
    public static double CalculateTotalEnergy(DamageMap damageMap)
    {
        double totalEnergy = 0;

        for (int index = 0; index < damageMap.PMap.Length; index++)
        {
            PhysicalParticle? particle = damageMap.PMap[index];
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
        for (int index = 0; index < damageMap.PhMap.Length; index++)
        {
            PhotonParticle? particle = damageMap.PhMap[index];
            if (particle != null)
            {
                totalEnergy += particle.Power * 10e6;
            }
        }

        return totalEnergy;
    }

    


}