using System;
using System.Collections.Generic;
using System.Numerics;


namespace GameEngine.Damage;

public static class KineticMath
{
    static int collisionCount = 0;
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
        collisionCount++;
        // Calculate initial properties
        Vector2 vA = physicalParticleA.Velocity;
        Vector2 vB = physicalParticleB.Velocity;
        double massA = physicalParticleA.Mass;
        double massB = physicalParticleB.Mass;

        // Total mass of both particles
        double totalMass = massA + massB;
        
        var neighborsA = GetConnectedNeighbors(map, physicalParticleA);
        var neighborsB = GetConnectedNeighbors(map, physicalParticleB);
        
        foreach (var neighbor in neighborsA)
        {
            if (neighborsB.Contains(neighbor))
            {
                throw new Exception("Collision detected with shared neighbors. Particle A and B are indirectly connected.");
            }
        }
        var kePartA = CalcKineticEnergy(physicalParticleA);
        var kePartB = CalcKineticEnergy(physicalParticleB);
        var keGroupA = CalcKineticEnergy(physicalParticleA, neighborsA);
        var keGroupB = CalcKineticEnergy(physicalParticleB, neighborsB);
        
        Vector2 collisonNormal = Vector2.Normalize(physicalParticleB.Position - physicalParticleA.Position);
        float baseElasticity = (float)(physicalParticleA.MatType.Elasticity + physicalParticleB.MatType.Elasticity * 0.5);// Assuming 0-1 range
        //double relativeVelocityMagnitude = Vector2.Dot(relativeVelocity, collisonNormal);
        
        double kePostCollision = (kePartA + kePartB) * baseElasticity;
        double keAF = (massA / totalMass) * kePostCollision;
        double keBF = (massB / totalMass) * kePostCollision;
        
        double keADelta = kePartA - keAF;
        double keBDelta = kePartB - keBF;
        double keTotalDelta = keADelta + keBDelta;
        DistributeKE(physicalParticleA, keADelta, collisonNormal, neighborsA);
        DistributeKE(physicalParticleB, keBDelta, -collisonNormal, neighborsB);

        // this section just for detecting energy gain in the system and throwing an error if so.
        var kfa = CalcKineticEnergy(physicalParticleA);
        var kfb = CalcKineticEnergy(physicalParticleB);
        var keFinalA = CalcKineticEnergy(physicalParticleA, neighborsA);
        var keFinalB = CalcKineticEnergy(physicalParticleB, neighborsB);
        
        
        var totalStart = keGroupA + keGroupB;
        var totalEnd = keFinalA + keFinalB;
        var cstart = kePartA + kePartB;
        var cend = kfa + kfb;
        
        var ctot = cend - cstart;
        if (ctot == 0)
        {
            throw new Exception("no ke loss");
        }

        if (cstart > cend)
        {
            string added = "Added: " + (totalEnd - totalStart);
            throw new Exception(added);
        }
        if (totalEnd > totalStart)
        {
            string start = "Start: " + totalStart;
            string end = "End: " + totalEnd;
            string added = "Added: " + (totalEnd - totalStart);
            throw new Exception(start + "\n" + end + "\n" + added);
        }
        
        TempratureMath.PostCollisionTempratureChange(physicalParticleA, physicalParticleB, keAF+keBF, map);
    }

    public static void DistributeKE(PhysicalParticle ctrParticle, double keDelta, Vector2 collisionNormal, List<PhysicalParticle> neighbors)
    {
        float dampingFactor = 0.01f;
        var totalKEStart = CalcKineticEnergy(ctrParticle, neighbors);
        
        // Get tensile connection data for the central particle
        float totalTensile  = TensileConnectionData(ctrParticle, neighbors);
        var tensileWeighting = 1.0f / totalTensile;
        // Calculate the central particle's share of KE
        double ctrParticleKE = keDelta * tensileWeighting;
        if (neighbors.Count == 0 || totalTensile <= 0)
        {
            ctrParticleKE = keDelta;
        }
        var newKeTotal = CalcKineticEnergy(ctrParticle) + keDelta;
        if(newKeTotal > dampingFactor)
        {
            var speedChange = (float)Math.Sqrt(2 * newKeTotal / ctrParticle.Mass) - ctrParticle.Velocity.Length();
            var normalComponent = Vector2.Dot(ctrParticle.Velocity, collisionNormal) * collisionNormal;
            var tangentComponent = ctrParticle.Velocity - normalComponent;
            // Adjust speed along collision normal
            var newNormalComponent = normalComponent + collisionNormal * speedChange;
            // Combine normal and tangent components
            ctrParticle.Velocity += newNormalComponent + tangentComponent;
        }
        else
        {
            ctrParticle.Velocity = Vector2.Zero;
        }
        
        // Calculate how much KE to distribute to each neighbor
        double neighborKe = (keDelta - ctrParticleKE) / neighbors.Count;

        foreach (var neighbor in neighbors)
        {
            // Calculate new KE for each neighbor
            var neighborKE = CalcKineticEnergy(neighbor) + neighborKe;
            if (neighborKE > dampingFactor) // Same padding for neighbors
            {
                var neighborSpeedChange = (float)Math.Sqrt(2 * neighborKE / neighbor.Mass) - neighbor.Velocity.Length();
        
                // Direction towards collision normal with slight bias away from ctr
                Vector2 directionToNeighbor = Vector2.Normalize(neighbor.Position - ctrParticle.Position);
                var direction = Vector2.Normalize(collisionNormal * 0.8f + directionToNeighbor * 0.2f);

                var neighborNormalComponent = Vector2.Dot(neighbor.Velocity, direction) * direction;
                var neighborTangentComponent = neighbor.Velocity - neighborNormalComponent;
                var newNeighborNormalComponent = neighborNormalComponent + direction * neighborSpeedChange;
        
                neighbor.Velocity += newNeighborNormalComponent + neighborTangentComponent;
            }
            else
            {
                neighbor.Velocity = Vector2.Zero;
            }
    
            if(float.IsNaN( neighbor.Velocity.Length() ))
                throw new Exception("is nan");
        }
        
        var totalKEEnd = CalcKineticEnergy(ctrParticle, neighbors);
        if (totalKEEnd >= totalKEStart + keDelta)
            throw new Exception("energyAdded");

    }
    
    
    public static double CalcKineticEnergy(PhysicalParticle physicalParticle)
    {
        var m = physicalParticle.Mass;
        var v = physicalParticle.Velocity;
        var foo = (0.5 * m * v.LengthSquared());
        if(double.IsNaN(foo))
            throw new Exception("is nan");
        return foo;
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

    
    
    public static float TensileConnectionData(PhysicalParticle physicalParticle, List<PhysicalParticle> neighbors)
    {
        float effectiveTensileStrength = 0; 
        if(physicalParticle.StateOfPhase == PhaseState.Solid)
        {
            effectiveTensileStrength = physicalParticle.MatType.TensileStrength;
            // Check if particle has neighbors with similar velocity
            foreach (var neighbor in neighbors)
            {
                effectiveTensileStrength += neighbor.MatType.TensileStrength;
            }
        }
        return effectiveTensileStrength;
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
}