using System;
using System.Collections.Generic;
using Pulsar4X.Orbital;


namespace GameEngine.Damage;

public static class KineticMath
{
        public static void DetectCollision(Particle particle, DamageMap map, List<(Particle,Particle)> collidedParticles)
    {
        int index = map.GetIndex(particle);
        var otherP = map.PMap[index];
        if 
        (
            otherP != null && //check there's actualy something at that position
            otherP != particle && //check not the same particle
            map.GetIndex(otherP) == index //check that the other particle hasn't moved as well
        )
        {
            collidedParticles.Add((particle, otherP)); // Add the particle we've collided with
        }
    }
    
    public static void ResolveCollision(Particle particleA, Particle particleB, DamageMap map)
    {
        // Calculate initial properties
        Vector2 vA = particleA.Velocity;
        Vector2 vB = particleB.Velocity;
        Vector2 relativeVelocity = vA - vB;
        double relativeSpeed = relativeVelocity.Length();
        double force = relativeSpeed * particleA.Mass; // Simplistic force based on speed and mass
        double initialKineticEnergy = 0.5 * particleA.Mass * particleA.Velocity.LengthSquared() + 0.5 * particleB.Mass * particleB.Velocity.LengthSquared();
        // Check if particleB should be treated as connected only if it's in a solid phase
        bool isConnected = false;
        double finalKineticEnergy = initialKineticEnergy; 
        if (particleB.StateOfPhase == PhaseState.Solid)
        {
            bool particleBIsConnected = false;
            float connectionRadius = 1.0f; // Define what radius to check for neighbors
            List<Particle> neighbors = DamageMapHelpers.GetNeighboringParticles(map, particleB.Position, connectionRadius);
            float effectiveTensileStrength = particleB.MatType.TensileStrength;
            int connections = 0;
            // Check if particleB has neighbors with similar velocity
            foreach (var neighbor in neighbors)
            {
                if (neighbor != particleA && neighbor.StateOfPhase == PhaseState.Solid && 
                    Vector2.Distance(particleB.Velocity, neighbor.Velocity) < 0.1f) // Example velocity similarity check
                {
                    particleBIsConnected = true;
                    effectiveTensileStrength += neighbor.MatType.TensileStrength;
                    connections++;
                }
            }
            
            effectiveTensileStrength /= (connections + 1);
            // Decide if particleB should stay connected
            double stress = force / (connections + 1); // Spread force over connections
            bool shouldStayConnected = stress <= effectiveTensileStrength;
            Vector2 normal = Vector2.Normalise(particleB.Position - particleA.Position);
            if (shouldStayConnected)
            {
                // Shift position but keep velocity
                float moveDistance = (float)Math.Min(1.0f, force / effectiveTensileStrength); // Limit movement to less than 1 unit
                particleB.Position += normal * moveDistance;
                particleA.Velocity = particleB.Velocity;

            }
            else
            {
                // Full collision response: adjust both position and velocity
                float baseElasticity = particleB.MatType.Elasticity; // Assuming 0-1 range
                double impulse = -(1 + baseElasticity) * Vector2.Dot(relativeVelocity, normal) / (1/particleA.Mass + 1/particleB.Mass);
                
                Vector2 vA_new = particleA.Velocity + impulse * (1/particleA.Mass) * normal;
                Vector2 vB_new = particleB.Velocity - impulse * (1/particleB.Mass) * normal;

                particleA.Velocity = vA_new;
                particleB.Velocity = vB_new;

                double moveDistance = Math.Min(1.0f, force / effectiveTensileStrength); // Limit movement to less than 1 unit
                particleB.Position += normal * moveDistance;

            }
        }
        else
        {
            HandleUnConnectedCollision(particleA, particleB, map);
            
        }
        
        TempratureMath.PostCollisionTempratureChange(particleA, particleB, initialKineticEnergy, map);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="particleA"></param>
    /// <param name="particleB"></param>
    /// <param name="map"></param>
    /// <returns>final Kinetic Energy</returns>
    public static void HandleUnConnectedCollision(Particle particleA, Particle particleB, DamageMap map)
    {
        Vector2 vA = particleA.Velocity;
        Vector2 vB = particleB.Velocity;
        float m1 = particleA.Mass, m2 = particleB.Mass;
        Vector2 relativeVelocity = vA - vB;
        double relativeSpeed = relativeVelocity.Length();
        
        
        // Define elasticity based on speed
        float maxVelocity = 10000f; // Example threshold for high-speed
        float baseElasticity = particleB.MatType.Elasticity; 
        Vector2 normal = Vector2.Normalise(particleB.Position - particleA.Position);
        double impulse = -(1 + baseElasticity) * Vector2.Dot(relativeVelocity, normal) / (1/particleA.Mass + 1/particleB.Mass);

        // Calculate new velocities
        Vector2 vA_new = vA + impulse * (1/m1) * normal;
        Vector2 vB_new = vB - impulse * (1/m2) * normal;
        // Update particle velocities
        particleA.Velocity = vA_new;
        particleB.Velocity = vB_new;
        
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
}