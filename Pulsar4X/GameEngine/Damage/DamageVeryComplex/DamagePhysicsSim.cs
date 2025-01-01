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
        damageMap.TotalEnergy = CalculateTotalEnergy(damageMap);
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
                damageMap.PMap[partTup.origIndex] = null;
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
        DamageMapHelpers.FindBadData(damageMap);    
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
    
    private static void ResolveCollision(Particle particleA, Particle particleB, DamageMap map)
    {
        // Calculate initial properties
        Vector2 vA = particleA.Velocity;
        Vector2 vB = particleB.Velocity;
        float m1 = particleA.Mass, m2 = particleB.Mass;
        Vector2 relativeVelocity = vA - vB;
        double relativeSpeed = relativeVelocity.Length();
        double force = relativeSpeed * particleA.Mass; // Simplistic force based on speed and mass
        double initialKineticEnergy = 0.5 * particleA.Mass * particleA.Velocity.LengthSquared() + 0.5 * particleB.Mass * particleB.Velocity.LengthSquared();
        // Check if particleB should be treated as connected only if it's in a solid phase
        bool isConnected = false;
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
                    break;
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

            // Calculate final kinetic energy and convert lost kinetic energy to heat
            double finalKineticEnergy = 0.5 * particleA.Mass * particleA.Velocity.LengthSquared() + 0.5 * particleB.Mass * particleB.Velocity.LengthSquared();
            double energyLost = Math.Max(0, initialKineticEnergy - finalKineticEnergy);

            float totalMass = particleA.Mass + particleB.Mass;
            float heatA = (float)(energyLost * (particleA.Mass / totalMass));
            float heatB = (float)(energyLost * (particleB.Mass / totalMass));

            float tempIncreaseA = heatA / (particleA.Mass * particleA.MatType.ThermalCapacity);
            float tempIncreaseB = heatB / (particleB.Mass * particleB.MatType.ThermalCapacity);

            particleA.Temperature += Math.Max(0, tempIncreaseA);
            particleB.Temperature += Math.Max(0, tempIncreaseB);
        }
        else
        {
            HandleUnConnectedCollision(particleA, particleB, map);
            
        }
    }

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

        double initialKineticEnergy = 0.5f * m1 * vA.LengthSquared() + 0.5f * m2 * vB.LengthSquared();
        double finalKineticEnergy = 0.5f * m1 * vA_new.LengthSquared() + 0.5f * m2 * vB_new.LengthSquared();
        double energyToHeat = initialKineticEnergy - finalKineticEnergy; // Ensure non-negative
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