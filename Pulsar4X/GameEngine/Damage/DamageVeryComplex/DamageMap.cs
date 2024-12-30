using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Linq;
using Pulsar4X.Components;
using Pulsar4X.Damage;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Industry;
using Pulsar4X.Modding;
using Pulsar4X.Orbital;

namespace GameEngine.Damage;

public class DamageMap
{
    public double FastestSpeed = 0;
    public const int PhysicsScale = 1000;
    public int Scale = 100;//pixels per meter
    int _pixBuf = 3; //this is just how much space we're leaving around the edges. 
    public Particle[] PMap;
    public Vector2[] VMap;
    public string[] compIDMap; //componentInstance Map.
    public float[] PresMap;
    public int Width;
    public int Height;
    public int X = 0;
    public int Y = 0;
    public DamageMap(int width, int height)
    {
        Width = width;
        Height = height;
        PMap = new Particle[Width * Height];
        VMap = new Vector2[Width * Height];
        PresMap = new float[Width * Height];
    }
    public DamageMap(int posX, int posY , Vector2 velocity, int width, int height, ParticleMaterial material)
    {
        X = posX;
        Y = posY;
        Width = width;
        Height = height;
        compIDMap = new string[Width * Height];
        PMap = new Particle[Width * Height];
        VMap = new Vector2[Width * Height];
        PresMap = new float[Width * Height];
        // Let's create a simple projectile shape, like a bullet or missile
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Here, we'll define the shape of our projectile. For simplicity, let's make it a square:
                if (x == width / 2 && y == height / 2) // Center point for a single particle projectile
                {
                    int index = y * width + x;
                    compIDMap[index] = "projectile";
                    PMap[index] = new Particle(material, new Vector2(x,y), velocity);
                    VMap[index] = velocity; // Set the initial velocity in VMap
                    PresMap[index] = 1.0f; // Assuming atmospheric pressure for simplicity
                }
                // If you want a more complex shape, you can use conditions here to define where particles exist
            }
        }
    }
    
    public DamageMap(EntityDamageProfileDB shipProfile)
    {
        List<(string typeID, float len, float height, int count)> partSizes = SetSize(shipProfile, Scale);
        Dictionary<string, List<ComponentInstance>> componentInstances = shipProfile.OwningEntity.GetDataBlob<ComponentInstancesDB>().ComponentsByDesign;
        ReadOnlyDictionary<string, ComponentDesign> lib = shipProfile.OwningEntity.GetFactionOwner.GetDataBlob<FactionInfoDB>().ComponentDesigns;
        Random rng = shipProfile.OwningEntity.Manager.RNG;
        var modData = shipProfile.OwningEntity.Manager.Game.StartingGameData;
        int currentX = _pixBuf; // Start at half of the buffer for the left side
        int partSizesIndex = 0;
        foreach (var partSize in partSizes)
        {
            string typeID = partSize.typeID;
            List<ComponentInstance> instanceIDs = componentInstances[typeID];
            int centerY = Height / 2; // Center Y, Height already includes buffer
            centerY += (int)(Math.Round(partSize.height) * partSize.count * 0.5);
            ComponentDesign componentDesign = lib[typeID];
            var mats = ParticleHelpers.GetMaterialsList(modData, componentDesign);
            int numparticles = (int)(Math.Round(partSize.height) * Math.Round(partSize.len));
            
            
            for (int i = 0; i < partSize.count; i++)
            {
                string instanceID = instanceIDs[i].UniqueID; // Get the corresponding instanceID
                int offsetY = -(int)Math.Round(partSize.height) * i;
                int actualY = centerY + offsetY;

                for (int y = 0; y > -Math.Round(partSize.height); y--)
                {
                    if (actualY + y >= Height)
                        throw new Exception("Outside the height of the array.(more than height)");
                    if(actualY + y < 0)
                        throw new Exception("Outside the height of the array. (less than 0)");
                    for (int x = 0; x < partSize.len; x++)
                    {
                        int index = GetIndex(currentX + x, actualY + y);

                        var mat = ParticleHelpers.GetRandomMat(mats, rng);
                        Vector2 pos = new Vector2(currentX + x, actualY + y);
                        Vector2 vel = Vector2.Zero;
                        Particle p = new Particle(mat, pos, vel);
                        float pressure = 1f;
                        compIDMap[index] = instanceID;
                        PMap[index] = p;
                        PresMap[index] = pressure;
                        VMap[index] = vel;
                    }
                }
            }
            // Increment currentX by the length of the part for the next placement
            currentX += (int)Math.Ceiling(partSize.len);
            // Check if we've gone beyond the width of the map, if so, throw
            if (currentX > Width) // Check against Width 
            {
                throw new Exception("trying to place items out of bounds of damage map");
            }

            partSizesIndex++;
        }
        PhysicsSim.FindBadData(this);
    }

    private List<(string id, float len, float height, int count)> SetSize(EntityDamageProfileDB shipProfile, float scale )
    {
        List<(string id, float len, float height, int count)> partsize = new();
        int componentWidthNum = 0;

        int totalLen = 0;
        var totalHeight = 0;

        byte componentInstance = 0;
        ReadOnlyDictionary<string, ComponentDesign> lib = shipProfile.OwningEntity.GetFactionOwner.GetDataBlob<FactionInfoDB>().ComponentDesigns;
        var po = shipProfile.PlacementOrder;
        for (int i = 0; i < po.Count; i++)
        {
            var typeid = po[i].id;
            var count = po[i].count;
            var compSize= PhysicsSim.GetComponentSize(lib, typeid, scale);
            partsize.Add((typeid, compSize.length, compSize.height, count));
            if (count > componentWidthNum)
                componentWidthNum = count;
            totalLen += (int)Math.Ceiling(compSize.length);
            int height = (int)Math.Ceiling(compSize.height) * count;
            if (height > totalHeight)
            {
                totalHeight = height;
            }
        }
        Height = totalHeight + _pixBuf * 2; //create a bit larger canvas size for the armor.
        Width = totalLen + _pixBuf * 2;
        int arraylen = Width * Height;
        PMap = new Particle[arraylen];
        VMap = new Vector2[arraylen];
        PresMap = new float[arraylen];
        compIDMap = new string[arraylen];
        return partsize;
    }

    public int GetIndex(int x, int y)
    {
        return y * Width + x;
    }
    public int GetIndex(Vector2 point)
    {
        return (int)(Math.Round(point.Y) * Width + Math.Round(point.X));
    }

    public int GetIndex(Particle particle)
    {
        return (int)(Math.Round(particle.Position.Y) * Width + Math.Round(particle.Position.X));
    }
    
    public static T GetItem<T>(object[] ary, int aryWid, int x, int y)
    {
        int row = y * aryWid;
        int col = x;
        return (T)ary[row + col];
    }
    
    public void MergeAndResize(DamageMap otherMap)
    {
        // Determine expansion based on relative positions of maps
        int expandX = otherMap.X < X ? -1 : (otherMap.X > X + Width ? 1 : 0);
        int expandY = otherMap.Y < Y ? -1 : (otherMap.Y > Y + Height ? 1 : 0);

        // Calculate new dimensions
        int newWidth = Width + Math.Abs(expandX) * otherMap.Width;
        int newHeight = Height + Math.Abs(expandY) * otherMap.Height;

        // Create new arrays for storing merged data
        string[] newIDMap = new string[newWidth * newHeight];
        Particle[] newPMap = new Particle[newWidth * newHeight];
        Vector2[] newVMap = new Vector2[newWidth * newHeight];
        float[] newPresMap = new float[newWidth * newHeight];

        // Offset for placing particles from this map
        int offsetX = expandX < 0 ? otherMap.Width : 0;
        int offsetY = expandY < 0 ? otherMap.Height : 0;

        // Copy and offset old data to new arrays
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                int oldIndex = GetIndex(x, y);
                int newIndex = (y + offsetY) * newWidth + (x + offsetX);
                newIDMap[newIndex] = compIDMap[oldIndex];
                if (PMap[oldIndex] != null)
                {
                    newPMap[newIndex] = PMap[oldIndex];
                    newPMap[newIndex].Position.X += offsetX;
                    newPMap[newIndex].Position.Y += offsetY;
                }
                newVMap[newIndex] = VMap[oldIndex];
                newPresMap[newIndex] = PresMap[oldIndex];
            }
        }

        // Add particles from the other map
        for (int y = 0; y < otherMap.Height; y++)
        {
            for (int x = 0; x < otherMap.Width; x++)
            {
                int otherIndex = otherMap.GetIndex(x, y);
                int newX = x + otherMap.X + offsetX;
                int newY = y + otherMap.Y + offsetY;
                int newIndex = newY * newWidth + newX;

                if (newIndex >= 0 && newIndex < newPMap.Length)
                {
                    newIDMap[newIndex] = otherMap.compIDMap[otherIndex];
                    newVMap[newIndex] = otherMap.VMap[otherIndex];
                    newPresMap[newIndex] = otherMap.PresMap[otherIndex];
                    if (otherMap.PMap[otherIndex] != null)
                    {
                        newPMap[newIndex] = otherMap.PMap[otherIndex];
                        newPMap[newIndex].Position.X = newX;
                        newPMap[newIndex].Position.Y = newY; 
                    }
                }
            }
        }

        // Update map properties
        compIDMap = newIDMap;
        PMap = newPMap;
        VMap = newVMap;
        PresMap = newPresMap;
        Width = newWidth;
        Height = newHeight;
        PhysicsSim.FindBadData(this);
    }

    private Vector2 CalculateAverageVelocity(DamageMap map)
    {
        Vector2 totalVelocity = Vector2.Zero;
        int count = 0;

        foreach (var particle in map.PMap)
        {
            if (particle != null)
            {
                totalVelocity += particle.Velocity;
                count++;
            }
        }
        return count > 0 ? totalVelocity / count : Vector2.Zero;
    }
}

public static class PhysicsSim
{

    public static (float length, float height) GetComponentSize(ReadOnlyDictionary<string, ComponentDesign> lib, string typeid, float scale)
    {
        ComponentDesign componentDeign = lib[typeid];
        var volm3 = componentDeign.VolumePerUnit;
        var area = Math.Cbrt(volm3) * scale;
        float length = (float)Math.Sqrt(area * componentDeign.AspectRatio);
        float height = (float)(area / length);
        return (length, height);
    }

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

    /// <summary>
    /// this function should only be called for debugging purposes. 
    /// </summary>
    /// <param name="map"></param>
    /// <exception cref="IndexOutOfRangeException"></exception>
    public static void FindBadData(DamageMap map)
    {
        foreach (var part in map.PMap)
        {
            if(part == null)
                continue;
            var index = map.GetIndex(part);
            if(index > map.PMap.Length - 1)
                throw new IndexOutOfRangeException();
            if(index < 0)
                throw new IndexOutOfRangeException();
        }
    }
    



    public static void PhysicsLoop(DamageMap damageMap)
    {
        FindBadData(damageMap);
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

        // Update particle positions in PMap after resolving collision
        //UpdateParticleInMap(particleA, map, indexA);
        //UpdateParticleInMap(particleB, map, indexB);
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
        

        if (temperature > criticalPoint.kelvin && pressure > criticalPoint.bar)
            return PhaseState.Gas; // Since we've decided not to include supercritical fluid

        if (temperature < zeroPoint && pressure == 0)
            return PhaseState.Solid;

        if (temperature < tripplePoint.kelvin && pressure < tripplePoint.bar)
            return PhaseState.Solid;

        if (temperature > tripplePoint.kelvin && pressure < tripplePoint.bar)
            return PhaseState.Gas;

        // Between melting and boiling (or at the triple point)
        if ((temperature >= zeroPoint && temperature <= tripplePoint.kelvin) ||
            (temperature >= tripplePoint.bar && pressure >= tripplePoint.bar && pressure < criticalPoint.bar))
        {
            return pressure >= tripplePoint.bar ? PhaseState.Liquid : PhaseState.Solid;
        }

        // Above boiling but below critical point
        if (temperature > tripplePoint.kelvin && pressure >= tripplePoint.bar && pressure < criticalPoint.bar)
            return PhaseState.Liquid;

        // Gas if above boiling point at any pressure not covered by the above conditions
        return PhaseState.Gas;
        
    }
}