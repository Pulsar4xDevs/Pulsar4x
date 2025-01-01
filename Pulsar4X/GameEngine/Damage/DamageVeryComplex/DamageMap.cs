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
    public double TotalEnergy = 0;
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
                    PMap[index] = new Particle(material, new Vector2(x,y), velocity, Scale);
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
                        Particle p = new Particle(mat, pos, vel, Scale);
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
    }

    private List<(string id, float len, float height, int count)> SetSize(EntityDamageProfileDB shipProfile, int scale )
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
            var compSize= DamageMapHelpers.GetComponentSize(lib, typeid, scale);
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

    public (int x, int y) GetPosition(int index)
    {
        return (index % Width, index / Width);
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
    }
}

public static class DamageMapHelpers
{
    public static double AreaFromVolume(double volm3, int scale)
    {
        return Math.Cbrt(volm3) * scale;
    }
    public static (float length, float height) GetComponentSize(ReadOnlyDictionary<string, ComponentDesign> lib, string typeid, int scale)
    {
        ComponentDesign componentDeign = lib[typeid];
        var volm3 = componentDeign.VolumePerUnit;
        var area = AreaFromVolume(volm3, scale);
        float length = (float)Math.Sqrt(area * componentDeign.AspectRatio);
        float height = (float)(area / length);
        return (length, height);
    }
    public static Vector2 CalculateAverageVelocity(DamageMap map)
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
            var pos = map.GetPosition(index);
            var partPos = (Math.Round( part.Position.X), Math.Round(part.Position.Y));
            
            if(index > map.PMap.Length - 1)
            {
                throw new IndexOutOfRangeException(pos.ToString());
            }
            if(index < 0)
            {
                throw new IndexOutOfRangeException(pos.ToString());
            }
            var isSame = map.PMap[index] == part; 
            if (!isSame)
                throw new Exception("out of position");
        }
    }
    
    public static List<Particle> GetNeighboringParticles(DamageMap map, Vector2 position, float radius)
    {
        List<Particle> neighbors = new List<Particle>();
        int minX = Math.Max(0, (int)(position.X - radius));
        int maxX = Math.Min(map.Width - 1, (int)(position.X + radius));
        int minY = Math.Max(0, (int)(position.Y - radius));
        int maxY = Math.Min(map.Height - 1, (int)(position.Y + radius));

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                int index = y * map.Width + x; // Assuming row-major order
                if (index >= 0 && index < map.PMap.Length && map.PMap[index] != null) 
                {
                    if (Vector2.Distance(position, map.PMap[index].Position) <= radius)
                    {
                        neighbors.Add(map.PMap[index]);
                    }
                }
            }
        }
        return neighbors;
    }
}