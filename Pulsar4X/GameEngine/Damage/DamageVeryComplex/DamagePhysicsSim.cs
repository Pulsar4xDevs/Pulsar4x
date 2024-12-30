using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        DamageMapHelpers.FindBadData(damageMap);
        damageMap.FastestSpeed = FindFastestParticle(damageMap);
        float timeStep = (float)(damageMap.Scale / damageMap.FastestSpeed);
        List<(Particle particle,int origIndex)> movingParticles = new(); //we could be more memory efficent and performant if we used an array buffer here for these.
        List<(Particle, Particle)> collisions = new();
        List<(Particle particle,int origIndex)> deleteParticles = new();
        // Collect all non-null and moving particles into a list
        for (int index = 0; index < damageMap.PMap.Length; index++)
        {
            Particle? particle = damageMap.PMap[index];
            if (particle != null && particle.Velocity.Length() > 0)
            {
                movingParticles.Add((particle, index));
            }
        }

        // Update positions of all moving particles
        foreach (var partTup in movingParticles)
        {
            UpdateParticlePosition(partTup.particle, damageMap.Scale, timeStep);
        }

        for (int index = 0; index < movingParticles.Count; index++)
        {
            (Particle particle, int origIndex) partTup = movingParticles[index];
            if (IsOutOfBounds(partTup.particle, damageMap))
            {
                //todo check if move to sister map
                movingParticles.RemoveAt(index);
                deleteParticles.Add(partTup);
                index--;
            }
        }

        // Detect collisions for all particles
        foreach (var partTup in movingParticles)
        {
            DetectCollision(partTup.particle, damageMap, collisions);
        }

        // Here you would typically resolve collisions after detection
        foreach (var partPair in collisions)
        {
            ResolveCollision(partPair.Item1, partPair.Item2, damageMap);
        }

        foreach (var partTup in movingParticles)
        {
            UpdateParticleInMap(partTup.particle, damageMap, partTup.origIndex);
        }
        
    }

    public static void UpdateParticlePosition(Particle particle, int scale, float timeStep)
    {
        Vector2 movement = particle.Velocity * timeStep;
        particle.Position += movement / scale;
    }

    public static bool IsOutOfBounds(Particle particle, DamageMap damageMap)
    {
        return particle.Position.X < 0 || particle.Position.Y < 0 ||
               particle.Position.X >= damageMap.Width || particle.Position.Y >= damageMap.Height;
    }
    
    public static void DetectCollision(Particle particle, DamageMap map, List<(Particle,Particle)> collidedParticles)
    {
        int x = (int)Math.Round(particle.Position.X);
        int y = (int)Math.Round(particle.Position.Y);
        int index = y * map.Width + x;

        // Check only the cell the particle has moved into
        if (index >= 0 && index < map.PMap.Length && map.PMap[index] != null && map.PMap[index] != particle)
        {
            collidedParticles.Add((particle, map.PMap[index])); // Add the particle we've collided with
        }
    }
    private static void ResolveCollision(Particle particleA, Particle particleB, DamageMap map)
    {
        // Calculate initial properties
        Vector2 vA = particleA.Velocity;
        Vector2 vB = particleB.Velocity;
        float m1 = particleA.Mass, m2 = particleB.Mass;
        Vector2 relativeVelocity = vA - vB;
        double relativeSpeed = relativeVelocity.Length();

        // Define elasticity based on speed
        float maxVelocity = 3000f; // Example threshold for high-speed
        double elasticity = Math.Max(0, 1 - (relativeSpeed / maxVelocity));

        // Calculate new velocities post-collision (partially elastic)
        float totalMass = m1 + m2;
        Vector2 vA_new = vA - ((2 * m2 / totalMass) * (Vector2.Dot(relativeVelocity, vA - vB) / relativeSpeed) * relativeVelocity) * elasticity;
        Vector2 vB_new = vB - ((2 * m1 / totalMass) * (Vector2.Dot(relativeVelocity, vB - vA) / relativeSpeed) * -relativeVelocity) * elasticity;

        // Update particle velocities
        particleA.Velocity = vA_new;
        particleB.Velocity = vB_new;

        // Calculate energy lost to heat
        double initialKineticEnergy = 0.5f * m1 * vA.LengthSquared() + 0.5f * m2 * vB.LengthSquared();
        double finalKineticEnergy = 0.5f * m1 * vA_new.LengthSquared() + 0.5f * m2 * vB_new.LengthSquared();
        double energyToHeat = initialKineticEnergy - finalKineticEnergy;

        // Distribute heat based on mass (more massive objects absorb more heat)
        double heatA = energyToHeat * (m1 / totalMass);
        double heatB = energyToHeat * (m2 / totalMass);

        // Convert energy to temperature increase
        particleA.Temperature += (float)(heatA / (m1 * particleA.MatType.ThermalCapacity));
        particleB.Temperature += (float)(heatB / (m2 * particleB.MatType.ThermalCapacity));

        // Check for phase transitions
        int indexA = map.GetIndex(particleA);
        int indexB = map.GetIndex(particleB);
        particleA.StateOfPhase = GetPhaseState(particleA,map.PresMap[indexA], particleA.Temperature);
        particleB.StateOfPhase = GetPhaseState(particleB, map.PresMap[indexB], particleB.Temperature);

        // Adjust positions to prevent sticking
        Vector2 direction = particleB.Position - particleA.Position;
        var distance = direction.Length();
        if (distance > 0)
        {
            direction /= distance; // Normalize direction
            particleA.Position -= direction * 0.1f; // Move A back a bit
            particleB.Position += direction * 0.1f; // Move B forward a bit
        }
    }

    // Helper method to update particle in map after position change
    private static void UpdateParticleInMap(Particle particle, DamageMap map, int oldIndex)
    {
        int newX = (int)Math.Round(particle.Position.X);
        int newY = (int)Math.Round(particle.Position.Y);
        int newIndex = newY * map.Width + newX;

        if (newIndex != oldIndex && newIndex >= 0 && newIndex < map.PMap.Length)
        {
            map.PMap[oldIndex] = null;
            map.PMap[newIndex] = particle;
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="particle"></param>
    /// <param name="pressure"></param>
    /// <param name="temperature"></param>
    /// <returns></returns>
    public static PhaseState GetPhaseState(Particle particle, float pressure, float temperature)
    {
        var zeroPoint = particle.MatType.MeltingZeroPoint;
        var criticalPoint = particle.MatType.CriticalPoint;
        var tripplePoint = particle.MatType.TriplePoint;
        

        if (temperature > criticalPoint.Kelvin && pressure > criticalPoint.Bar)
            return PhaseState.Gas; // Since we've decided not to include supercritical fluid

        if (temperature < zeroPoint && pressure == 0)
            return PhaseState.Solid;

        if (temperature < tripplePoint.Kelvin && pressure < tripplePoint.Bar)
            return PhaseState.Solid;

        if (temperature > tripplePoint.Kelvin && pressure < tripplePoint.Bar)
            return PhaseState.Gas;

        // Between melting and boiling (or at the triple point)
        if ((temperature >= zeroPoint && temperature <= tripplePoint.Kelvin) ||
            (temperature >= tripplePoint.Bar && pressure >= tripplePoint.Bar && pressure < criticalPoint.Bar))
        {
            return pressure >= tripplePoint.Bar ? PhaseState.Liquid : PhaseState.Solid;
        }

        // Above boiling but below critical point
        if (temperature > tripplePoint.Kelvin && pressure >= tripplePoint.Bar && pressure < criticalPoint.Bar)
            return PhaseState.Liquid;

        // Gas if above boiling point at any pressure not covered by the above conditions
        return PhaseState.Gas;
        
    }
}