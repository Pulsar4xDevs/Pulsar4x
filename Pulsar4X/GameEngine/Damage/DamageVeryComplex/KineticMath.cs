using System;
using System.Collections.Generic;
using Pulsar4X.Orbital;
using Vector2 = System.Numerics.Vector2;


namespace GameEngine.Damage;

public static class KineticMath
{
    static int collisionCount = 0;
    public static void DetectCollision(PhysicalParticle movingParticle, DamageMap map, List<(PhysicalParticle,PhysicalParticle)> collidedParticles)
    {
        int index = map.GetIndex(movingParticle);
        var otherP = map.PMap[index];
        if 
        (
            otherP != null && //check there's actualy something at that position
            otherP != movingParticle && //check not the same particle
            map.GetIndex(otherP) == index //check that the other particle hasn't moved as well
        )
        {
            collidedParticles.Add((movingParticle, otherP)); // Add the particle we've collided with
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
        var keGroupA = CalcKineticEnergy(physicalParticleA, neighborsA);
        var keGroupB = CalcKineticEnergy(physicalParticleB, neighborsB);

        for (int index = 0; index < neighborsA.Count; index++)
        {
            PhysicalParticle? neighbor = neighborsA[index];
            if (neighborsB.Contains(neighbor))
            {
                if (physicalParticleA.compID == neighbor.compID)
                    neighborsB.Remove(neighbor);
                else
                {
                    neighborsA.Remove(neighbor);
                    index--;
                }
            }
        }

        var kePartA = CalcKineticEnergy(physicalParticleA);
        var kePartB = CalcKineticEnergy(physicalParticleB);
        
        Vector2 collisonNormal = Vector2.Normalize(physicalParticleB.Position - physicalParticleA.Position);
        float baseElasticity = (float)(physicalParticleA.MatType.Elasticity + physicalParticleB.MatType.Elasticity * 0.5);// Assuming 0-1 range
        //double relativeVelocityMagnitude = Vector2.Dot(relativeVelocity, collisonNormal);
        Vector2 relativeVelocity = vA - vB;
        
        double kePostCollision = (kePartA + kePartB) * baseElasticity;
        double keTotalDelta = kePartA + kePartB + kePostCollision;
        double keADelta = (massA / totalMass) * keTotalDelta;
        double keBDelta = (massB / totalMass) * keTotalDelta;
        
        var ketd = keADelta + keBDelta;
        
        //DistributeKE(physicalParticleA, keADelta, collisonNormal, neighborsA);
        //DistributeKE(physicalParticleB, keBDelta, -collisonNormal, neighborsB);
        float impulseMagnitude = -((1 + baseElasticity) * Vector2.Dot(relativeVelocity, collisonNormal)) /
                                 ((1 / physicalParticleA.Mass) + (1 / physicalParticleB.Mass));

        DistributeImpulse(physicalParticleA, neighborsA, collisonNormal, impulseMagnitude);
        DistributeImpulse(physicalParticleB, neighborsB, -collisonNormal, impulseMagnitude);
        
        
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
            //throw new Exception("no ke loss");
        }

        if (cstart > cend)
        {
            string added = "Added: " + (totalEnd - totalStart);
            //throw new Exception(added);
        }
        if (totalEnd > totalStart)
        {
            string start = "Start: " + totalStart;
            string end = "End: " + totalEnd;
            string added = "Added: " + (totalEnd - totalStart);
            //throw new Exception(start + "\n" + end + "\n" + added);
        }
        
        TempratureMath.PostCollisionTempratureChange(physicalParticleA, physicalParticleB, keTotalDelta, map);
    }
    
    
    public static void DistributeImpulse(
        PhysicalParticle ctrParticle,
        List<PhysicalParticle> neighbors,
        Vector2 collisionNormal, double collisionMagnitude
    )
    {
        float basePrimaryRatio = 0.6f; // More impulse to the central particle
        float baseNeighborRatio = 0.4f; // Remaining impulse to neighbors

        var ctrKEStart = CalcKineticEnergy(ctrParticle);
        
        Vector2 collisionImpulse;
        // Precompute tensile connection data for the center particle
        var totalTensile = TensileConnectionData(ctrParticle, neighbors);
        float tensileWeighting = 1.0f / totalTensile;
        
        if (neighbors.Count == 0 || totalTensile<= 0)
        {
            collisionImpulse = collisionNormal * (float)collisionMagnitude;
        }
        else
        {
            float primaryRatio = basePrimaryRatio * tensileWeighting;
            collisionImpulse = collisionNormal * (float)collisionMagnitude * primaryRatio;
        }
        
        Vector2 centerVelocityChange = collisionImpulse / ctrParticle.Mass;
        //var averageNeighborVelocity = AverageVelocity(neighbors);

        // Apply the standard velocity change to the center particle
        ctrParticle.Velocity += centerVelocityChange;
        if (!float.IsFinite(ctrParticle.Velocity.Length()))
            throw new Exception("not a finite number");
        
        var keCtrEnd = CalcKineticEnergy(ctrParticle);
        if (keCtrEnd > ctrKEStart)
        {
            var dif = keCtrEnd - ctrKEStart;
            //throw new Exception("this math doesn't work");
        }
        
        
        if(float.IsNaN(ctrParticle.Velocity.Length()))
            throw new Exception("NaN");
        // Apply impulses to neighbors
        float neighborRatio = baseNeighborRatio * tensileWeighting;
        float neighborImpulseMag = (float)collisionMagnitude * neighborRatio / neighbors.Count;
        foreach (var neighbor in neighbors)
        {
            var keNbrStart = CalcKineticEnergy(neighbor);
            // Calculate impulse direction and scaled impulse
            Vector2 directionToNeighbor = Vector2.Normalize(neighbor.Position - ctrParticle.Position);
            var direction = Vector2.Normalize(collisionNormal * 0.8f + directionToNeighbor * 0.2f);
            if(float.IsNaN(direction.Length()))
                direction = collisionNormal;

            Vector2 neighborVelocityChange = (direction * neighborImpulseMag) / neighbor.Mass;
            
            // Apply the adjusted impulse to the neighbor
            neighbor.Velocity += neighborVelocityChange;
            if(!float.IsFinite(neighbor.Velocity.Length()))
                throw new Exception("NaN or Infinity");
            var keNbrEnd = CalcKineticEnergy(ctrParticle);
            if (keNbrEnd > keNbrStart)
            {
                var dif = keNbrEnd - keNbrStart;
                //throw new Exception("this math doesn't work");
            }
        }
    }

    /*
    public static void DistributeKE(PhysicalParticle ctrParticle, double keDelta, Vector2 collisionNormal, List<PhysicalParticle> neighbors)
    {
        float dampingFactor = 0.01f;
        var totalKEStart = CalcKineticEnergy(ctrParticle, neighbors);
        
        // Get tensile connection data for the central particle
        float totalTensile  = TensileConnectionData(ctrParticle, neighbors);
        var tensileWeighting = 1.0f / totalTensile;
        // Calculate the central particle's share of KE
        double ctrKeDelta = keDelta * tensileWeighting;
        if (neighbors.Count == 0 || totalTensile <= 0)
        {
            ctrKeDelta = keDelta;
        }
        var keCtrstart = CalcKineticEnergy(ctrParticle);
        var newKeTotal = keCtrstart + ctrKeDelta;
        if(newKeTotal > dampingFactor)
        {
            var velAngle = Angle.RadiansFromVector2(ctrParticle.Velocity);
            var colAngle = Angle.RadiansFromVector2(collisionNormal);
            double angleBetween = Angle.DifferenceBetweenRadians(velAngle, colAngle);
            float newSpeed = (float)Math.Sqrt(2 * newKeTotal / ctrParticle.Mass);
            float speedAlongNormal = (float)(newSpeed * Math.Cos(angleBetween));
            Vector2 perpendicularToNormal = Vector2.Normalize( new Vector2(-collisionNormal.Y, collisionNormal.X));
            float speedPerpendicular = (float)(newSpeed * Math.Sin(angleBetween));
            Vector2 newVelocity = collisionNormal * speedAlongNormal + perpendicularToNormal * speedPerpendicular;
            if (Vector2.Dot(ctrParticle.Velocity, collisionNormal) < 0) // Moving away from the collision
            {
                newVelocity = -newVelocity; // Flip the direction 
            }
            ctrParticle.Velocity = newVelocity;
            var keCtrEnd = CalcKineticEnergy(ctrParticle);
            if (keCtrEnd > newKeTotal)
            {
                var dif = keCtrEnd - newKeTotal;
                throw new Exception("this math doesn't work");
            }
        }
        else
        {
            ctrParticle.Velocity = Vector2.Zero;
        }
        
        // Calculate how much KE to distribute to each neighbor
        double neighborKeDelta = (keDelta - ctrKeDelta) / neighbors.Count;

        foreach (var neighbor in neighbors)
        {
            // Calculate new KE for each neighbor
            var newNeighborKE = CalcKineticEnergy(neighbor) + neighborKeDelta;
            if (newNeighborKE > dampingFactor) // Same padding for neighbors
            {
                
                float newSpeed = (float)Math.Sqrt(2 * newNeighborKE / neighbor.Mass);
                float currentSpeed = neighbor.Velocity.Length();
                float speedRatio = newSpeed / currentSpeed;

                Vector2 adjustedVelocity = neighbor.Velocity * speedRatio;
                Vector2 directionToNeighbor = Vector2.Normalize(neighbor.Position - ctrParticle.Position);
                // Then, adjust for direction:
                Vector2 direction = Vector2.Normalize(collisionNormal * 0.8f + directionToNeighbor * 0.2f);
                
                var velAngle = Angle.RadiansFromVector2(adjustedVelocity);
                var dirAngle = Angle.RadiansFromVector2(direction);
                double angleBetween = Angle.DifferenceBetweenRadians(velAngle, dirAngle);
                
                Vector2 newNormalComponent = direction * (float)(adjustedVelocity.Length() * Math.Cos(angleBetween));
                Vector2 perpendicularToDir = Vector2.Normalize( new Vector2(-direction.Y, direction.X));
                Vector2 newTangentComponent = perpendicularToDir * (float)(adjustedVelocity.Length() * Math.Sin(angleBetween));
                neighbor.Velocity = newNormalComponent + newTangentComponent;
                
                var keNbrEnd = CalcKineticEnergy(neighbor);
                if (keNbrEnd > newNeighborKE)
                    throw new Exception("this math doesn't work");

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

    } */
    
    
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
        if(centerParticle.StateOfPhase == PhaseState.Solid)
        {
            for (int i = -map.Width; i <= map.Width; i += map.Width)
            {
                for (int j = -1; j <= 1; j++)
                {
                    int index = ctrIndex + i + j;
                    if (index < 0 || index >= map.PMap.Length)
                        continue; //check out of bounds
                    if (index == ctrIndex)
                        continue; //check not the same particle
                    var neighbor = map.PMap[index];
                    if (neighbor == null)
                        continue; //check not null
                    if (neighbor.StateOfPhase != PhaseState.Solid)
                        continue; //check solid particle
                    //if material is the same, or the particles are from the same component
                    if ((neighbor.MatType.PartMatID == centerParticle.MatType.PartMatID || neighbor.compID == centerParticle.compID))
                    {
                        totalVelocityDifference = Vector2.Distance(centerParticle.Velocity, neighbor.Velocity);
                        neighborCount++;
                        //connectedNeighbors.Add(neighbor);
                    }
                }
            }
            if (totalVelocityDifference / neighborCount > velocityThreshold)
                isDetached = true;
        }
        else 
            isDetached = true;
        
        if(isDetached)
        {
             centerParticle.IsComponentPartDestroyed = true;
            centerParticle.compID = map.GenerateNewCompID("fragment");
        }
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
    

    public static PhysicalParticle GetFastestPart(DamageMap map)
    {
        double mag = 0;
        PhysicalParticle fastPart = null;
        foreach (var part in map.PMap)
        {
            if(part == null)
                continue;
            if( part.Velocity.Length() > mag)
            {
                fastPart = part;

            }
        }
        return fastPart; 
    }
    
    public static float TensileConnectionData(PhysicalParticle physicalParticle, List<PhysicalParticle> neighbors)
    {
        float tensileStrength = 1; 
        tensileStrength = physicalParticle.MatType.TensileStrength;
        // Check if particle has neighbors with similar velocity
        foreach (var neighbor in neighbors)
        {
            tensileStrength += neighbor.MatType.TensileStrength;
        }
        return tensileStrength;
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