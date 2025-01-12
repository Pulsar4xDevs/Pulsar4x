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
        var neighborsA = GetConnectedNeighbors(map, physicalParticleA);
        var neighborsB = GetConnectedNeighbors(map, physicalParticleB);
        
        foreach (var neighbor in neighborsA)
        {
            if (neighborsB.Contains(neighbor))
            {
                throw new Exception("Collision detected with shared neighbors. Particle A and B are indirectly connected.");
            }
        }
        
        var keA = CalcKineticEnergy(physicalParticleA, neighborsA);
        var keB = CalcKineticEnergy(physicalParticleB, neighborsB);
        
        
        Vector2 normal = Vector2.Normalize(physicalParticleB.Position - physicalParticleA.Position);
        float baseElasticity = (float)(physicalParticleA.MatType.Elasticity + physicalParticleB.MatType.Elasticity * 0.5);// Assuming 0-1 range
        float impulseMagnitude = -((1 + baseElasticity) * Vector2.Dot(relativeVelocity, normal)) /
                                 ((1 / physicalParticleA.Mass) + (1 / physicalParticleB.Mass));
        // Impulse for A
        Vector2 impulseA = normal * impulseMagnitude;
        // Impulse for B (equal and opposite)
        Vector2 impulseB = -normal * impulseMagnitude;
        
        DistributeImpulse(physicalParticleA, neighborsA, impulseA, map);
        DistributeImpulse(physicalParticleB, neighborsB, impulseB, map);

        // this section just for detecting energy gain in the system and throwing an error if so.
        var keFinalA = CalcKineticEnergy(physicalParticleA, neighborsA);
        var keFinalB = CalcKineticEnergy(physicalParticleB, neighborsB);
        var totalStart = keA + keB;
        var totalEnd = keFinalA + keFinalB;
        if (totalEnd > totalStart)
        {
            string start = "Start: " + totalStart;
            string end = "End: " + totalEnd;
            string added = "Added: " + (totalEnd - totalStart);
            throw new Exception(start + "\n" + end + "\n" + added);
        }
        
        TempratureMath.PostCollisionTempratureChange(physicalParticleA, physicalParticleB, keA+keB, map);
    }

    public static double CalcKineticEnergy(PhysicalParticle physicalParticle)
    {
        var m = physicalParticle.Mass;
        var v = physicalParticle.Velocity;
        return (0.5 * m * v.LengthSquared());
    }
    
    public static double CalcKineticEnergy(PhysicalParticle physicalParticle, List<PhysicalParticle> neighbors)
    {
        var m = physicalParticle.Mass;
        var v = physicalParticle.Velocity;
        var ke = (0.5 * m * v.LengthSquared());
        foreach (var nb in neighbors)
        {
            ke += CalcKineticEnergy(nb);
        }
        return ke;
    }

    /// <summary>
    /// this is called from the main PhysicsLoop
    /// </summary>
    /// <param name="map"></param>
    /// <param name="centerParticle"></param>
    /// <param name="velocityThreshold"></param>
    public static void ReAssessDetachmentWithNeighbors(
        DamageMap map, PhysicalParticle centerParticle, float velocityThreshold)
    {
        bool isDetached = false;
        float totalVelocityDifference = 0;
        int neighborCount = 0;
        //var connectedNeighbors = new List<PhysicalParticle>();
        int ctrIndex = map.GetIndex(centerParticle);
        for (int i = -map.Width; i <= map.Width; i+=map.Width)
        {
            for (int j = -1; j <= 1; j++)
            {
                int index = ctrIndex + i + j;
                if (index < 0 || index >= map.PMap.Length)
                    continue; //check out of bounds
                if(index == ctrIndex)
                    continue; //check not the same particle
                var neighbor = map.PMap[index];
                if(neighbor == null)
                    continue; //check not null
                if(neighbor.StateOfPhase != PhaseState.Solid)
                    continue; //check solid particle
                //if material is the same, or the particles are from the same component
                if ((neighbor.MatType.PartMatID == centerParticle.MatType.PartMatID 
                     || neighbor.compID == centerParticle.compID))
                {
                    totalVelocityDifference = Vector2.Distance(centerParticle.Velocity, neighbor.Velocity);
                    neighborCount++;
                    //connectedNeighbors.Add(neighbor);
                }
            }
        }

        if (totalVelocityDifference / neighborCount > velocityThreshold)
            isDetached = true;
        if(isDetached)
            centerParticle.compID = map.GenerateNewCompID("fragment");
    }
    
    public static List<PhysicalParticle> GetConnectedNeighbors(DamageMap map, PhysicalParticle centerParticle)
    {
        var connectedNeighbors = new List<PhysicalParticle>();
        int ctrIndex = map.GetIndex(centerParticle);
        for (int i = -map.Width; i <= map.Width; i+=map.Width)
        {
            for (int j = -1; j <= 1; j++)
            {
                int index = ctrIndex + i + j;
                if (index < 0 || index >= map.PMap.Length)
                    continue; //check out of bounds
                if(index == ctrIndex)
                    continue; //check not the same particle
                var neighbor = map.PMap[index];
                if(neighbor == null)
                    continue; //check not null
                if(neighbor.StateOfPhase != PhaseState.Solid)
                    continue; //check solid particle
                //if material is the same, or the particles are from the same component
                if ((neighbor.MatType.PartMatID == centerParticle.MatType.PartMatID 
                    || neighbor.compID == centerParticle.compID) 
                    && (Vector2.Distance(centerParticle.Velocity, neighbor.Velocity) < 0.1f))
                {
                    connectedNeighbors.Add(neighbor);
                }
            }
        }
        return connectedNeighbors;
    }

    
    
    public static (int connections, float effectiveTensile ) TensileConnectionData(PhysicalParticle physicalParticle, DamageMap map)
    {
        List<PhysicalParticle> neighbors = GetConnectedNeighbors(map, physicalParticle);
        float effectiveTensileStrength = 0; 
        int connections = 0;
        if(physicalParticle.StateOfPhase == PhaseState.Solid)
        {
            effectiveTensileStrength = physicalParticle.MatType.TensileStrength;
            // Check if particle has neighbors with similar velocity
            foreach (var neighbor in neighbors)
            {
                effectiveTensileStrength += neighbor.MatType.TensileStrength;
                connections++;
            }
        }
        effectiveTensileStrength /= (connections + 1);
        return (connections, effectiveTensileStrength);
    }

    public static Vector2 AverageVelocity(List<PhysicalParticle> particles)
    {
        Vector2 averageVelocity = Vector2.Zero;
        foreach (var particle in particles)
        {
            averageVelocity += particle.Velocity;
        }
        averageVelocity /= particles.Count;
        return averageVelocity;
    }
    
    public static void DistributeImpulse(
        PhysicalParticle ctrParticle,
        List<PhysicalParticle> neighbors,
        Vector2 collisionImpulse, // The primary impulse from the collision
        DamageMap map
    )
    {
        float basePrimaryRatio = 0.6f; // More impulse to the central particle
        float baseNeighborRatio = 0.4f; // Remaining impulse to neighbors
        float velocityThreshold = 0.05f; // Threshold for negligible velocity changes

        // Precompute tensile connection data for the center particle
        (int connections, float effectiveTensile) tca = TensileConnectionData(ctrParticle, map);
        if (neighbors.Count == 0 || tca.effectiveTensile <= 0)
        {
            ctrParticle.Velocity += collisionImpulse / ctrParticle.Mass; // Full impact to the center
            return; // Exit early, no neighbors to process
        }
        // Scaling factor inversely proportional to tensile strength
        float changeScaling = 1.0f / tca.effectiveTensile;

        // Calculate primary impulse for the center particle
        float primaryRatio = basePrimaryRatio * changeScaling;
        Vector2 centerVelocityChange = collisionImpulse * primaryRatio / ctrParticle.Mass;

        // Apply velocity change to the center particle only if it exceeds the threshold
        if (centerVelocityChange.Length() < velocityThreshold)
        {
            // Set the center particle's velocity to the average of its neighbors
            var averageNeighborVelocity = AverageVelocity(neighbors);
            ctrParticle.Velocity = averageNeighborVelocity;
        }
        else
        {
            // Apply the standard velocity change to the center particle
            ctrParticle.Velocity += centerVelocityChange;
        }
        var normCtrVel = Vector2.Normalize(centerVelocityChange);
        if(float.IsNaN(ctrParticle.Velocity.Length()))
            throw new Exception("NaN");
        // Apply impulses to neighbors
        foreach (var neighbor in neighbors)
        {
            // Neighbor velocity change scaled using the same effective tensile strength logic
            float neighborRatio = baseNeighborRatio * changeScaling;

            // Calculate impulse direction and scaled impulse
            Vector2 directionToNeighbor = Vector2.Normalize(neighbor.Position - ctrParticle.Position);
            var direction = Vector2.Normalize(normCtrVel * 0.8f + directionToNeighbor * 0.2f);
            if(float.IsNaN(direction.Length()))
                direction = normCtrVel;
            Vector2 adjustedImpulse = collisionImpulse * neighborRatio * direction;
            Vector2 neighborVelocityChange = adjustedImpulse / neighbor.Mass;

            // Skip updating the neighbor's velocity if the change is below the threshold
            if (neighborVelocityChange.Length() < velocityThreshold)
            {
                continue;
            }

            // Apply the adjusted impulse to the neighbor
            neighbor.Velocity += neighborVelocityChange;
            if(float.IsNaN(neighbor.Velocity.Length()))
                throw new Exception("NaN");
        }
    }
    
    
    
}