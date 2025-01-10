using System;
using System.Collections.Generic;
using System.Numerics;


namespace GameEngine.Damage;

public static class KineticMath
{
    public static void DetectCollision(IDamageParticle physicalParticle, DamageMap map, List<(IDamageParticle,PhysicalParticle)> collidedParticles)
    {
        int index = map.GetIndex(physicalParticle);
        var otherP = map.PMap[index];
        if 
        (
            otherP != null && //check there's actualy something at that position
            otherP != physicalParticle && //check not the same particle
            map.GetIndex(otherP) == index //check that the other particle hasn't moved as well
        )
        {
            collidedParticles.Add((physicalParticle, otherP)); // Add the particle we've collided with
        }
    }
    
    public static void ResolveCollision(PhysicalParticle physicalParticleA, PhysicalParticle physicalParticleB, DamageMap map)
    {
        
        // Calculate initial properties
        Vector2 vA = physicalParticleA.Velocity;
        Vector2 vB = physicalParticleB.Velocity;
        Vector2 relativeVelocity = vA - vB;
        double relativeSpeed = relativeVelocity.Length();
        float force = (float)(relativeSpeed * physicalParticleA.Mass); // Simplistic force based on speed and mass
        var keA = CalcKineticEnergy(physicalParticleA);
        var keB = CalcKineticEnergy(physicalParticleB);
        
        (int connections, float effectiveTensile) tda = TensileConnectionData(physicalParticleA, map);
        (int connections, float effectiveTensile) tdb = TensileConnectionData(physicalParticleB, map);
        Vector2 normal = Vector2.Normalize(physicalParticleB.Position - physicalParticleA.Position);
        float baseElasticity = (float)(physicalParticleA.MatType.Elasticity + physicalParticleB.MatType.Elasticity * 0.5);// Assuming 0-1 range
        float impulse = (float)(-(1 + baseElasticity) * Vector2.Dot(relativeVelocity, normal) / (1/physicalParticleA.Mass + 1/physicalParticleB.Mass));
        HandleCollision(physicalParticleA, tda.connections, tda.effectiveTensile, normal, force, impulse);
        HandleCollision(physicalParticleB, tdb.connections, tdb.effectiveTensile, normal, force, -impulse);
        var keA2 = CalcKineticEnergy(physicalParticleA);
        var keB2 = CalcKineticEnergy(physicalParticleB);
        if (keA2 + keB2 > keA + keB)
            throw new Exception("ke energy added");
        TempratureMath.PostCollisionTempratureChange(physicalParticleA, physicalParticleB, keA+keB, map);
    }

    public static float CalcKineticEnergy(PhysicalParticle physicalParticle)
    {
        var m = physicalParticle.Mass;
        var v = physicalParticle.Velocity;
        return (float)(0.5f * m * v.LengthSquared());
    }

    public static void HandleCollision(PhysicalParticle physicalParticle, int connections, float effectiveTensileStrength, Vector2 normal, float force, float impulse)
    {
        // Decide if particleB should stay connected
        double stress = force / (connections + 1); // Spread force over connections
        bool shouldStayConnected = stress <= effectiveTensileStrength;
        if (!shouldStayConnected)
        {
            // Full collision response: adjust both position and velocity
            //particle.Velocity += - impulse * (1/particle.Mass) * normal;
        }
        physicalParticle.Velocity += impulse * (1/physicalParticle.Mass) * normal;
        double moveDistance = Math.Min(1.0f, force / effectiveTensileStrength); // nudge a bit
        //particle.Position += normal * moveDistance;
    }
    
    
    public static (int connections, float effectiveTensile ) TensileConnectionData(PhysicalParticle physicalParticle, DamageMap map)
    {
        float connectionRadius = 1.0f; // Define what radius to check for neighbors
        List<PhysicalParticle> neighbors = DamageMapHelpers.GetNeighboringParticles(map, physicalParticle.Position, connectionRadius);
        float effectiveTensileStrength = 0; 
        int connections = 0;
        if(physicalParticle.StateOfPhase == PhaseState.Solid)
        {
            effectiveTensileStrength = physicalParticle.MatType.TensileStrength;
            // Check if particle has neighbors with similar velocity
            foreach (var neighbor in neighbors)
            {
                if (neighbor.StateOfPhase == PhaseState.Solid && Vector2.Distance(physicalParticle.Velocity, neighbor.Velocity) < 0.1f) // Example velocity similarity check
                {
                    effectiveTensileStrength += neighbor.MatType.TensileStrength;
                    connections++;
                }
            }
        }
        effectiveTensileStrength /= (connections + 1);
        return (connections, effectiveTensileStrength);
    }
}