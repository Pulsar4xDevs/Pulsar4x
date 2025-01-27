using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;
using Pulsar4X.Components;
using Pulsar4X.Damage;
using Pulsar4X.Datablobs;
using Pulsar4X.Factions;
using Pulsar4X.Helpers;

//using Pulsar4X.Orbital;
using Pulsar4X.Weapons;

namespace GameEngine.Damage;

public class DamageMap
{
    public TimeSpan RunTime = TimeSpan.Zero;
    private Dictionary<string, ushort> componentIDLookup = new();

    
    public Dictionary<string, ((int x, int y) Position, (int x, int y) Size, int totalParticles)> componentData = new();
    
    private ushort _nextComponentID = 0;

    public double TotalEnergy = 0;
    public const int PhysicsScale = 1000;//currently not used
    public int Scale = 100;//particles per meter
    int _pixBuf = 10; //this is just how much space we're leaving around the edges. 
    private int _armorHeadspace = 2; //space between skin and componenents.
    public PhysicalParticle[] PMap;
    public List<BeamPoint> BeamStarts = new();
    public List<BeamPoint> BeamPoints;
    public int[] compIDMap; //componentInstance Map.
    public float[] PresMap; //pressure in bar
    public int Width;
    public int Height;
    public int X = 0;
    public int Y = 0;
    public DamageMap(int width, int height)
    {
        Width = width;
        Height = height;
        PMap = new PhysicalParticle[Width * Height];
        PresMap = new float[Width * Height];
    }
    public DamageMap(int posX, int posY , Vector2 velocity, int width, int height, ParticleMaterial material)
    {
        X = posX;
        Y = posY;
        Width = width;
        Height = height;
        compIDMap = new int[Width * Height];
        PMap = new PhysicalParticle[Width * Height];
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
                    compIDMap[index] = _nextComponentID;
                    var newPart = new PhysicalParticle(_nextComponentID, material, new Vector2(x,y), velocity, Scale);
                    newPart.mapIndex = index;
                    PMap[index] = newPart;
                    PresMap[index] = 1.0f; // Assuming atmospheric pressure for simplicity
                }
                // If you want a more complex shape, you can use conditions here to define where particles exist
            }
        }
        componentIDLookup.Add("projectile"+_nextComponentID, _nextComponentID);
        _nextComponentID++;
    }

    public ushort GenerateNewCompID(string strID)
    {
        componentIDLookup.Add(strID+_nextComponentID, _nextComponentID);
        _nextComponentID++;
        return (ushort)(_nextComponentID - 1);
    }

    public void AddComponentID(string strID)//, float htkpp)
    {
        ushort intID = GenerateNewCompID(strID);
        componentIDLookup.Add(strID, intID);
        _nextComponentID++;
        //componentIDLookupByIntID.Add(intID, (strID, htkpp));
        
    }

    /// <summary>
    /// laser creation
    /// </summary>
    /// <param name="posX"></param>
    /// <param name="posY"></param>
    /// <param name="beamInfo"></param>
    /// <param name="lifetime"></param>
    public DamageMap(int posX, int posY, BeamInfoDB beamInfo, float lifetime)
    {
        X = posX;
        Y = posY;
        //var range = (beamInfo.LaunchPosition - beamInfo.Positions.Item1).Length();
        
        
        Width = 10; // Example width, set as needed
        Height = 10; // Example height, set as needed

        compIDMap = new int[Width * Height];
        PMap = new PhysicalParticle[Width * Height];
        PresMap = new float[Width * Height];
        int length = 5; //todo change this to dispersion from range.
        // Launch position is transformed into this smaller map's local coordinate space
        Vector2 localOrigin = new Vector2(Width / 2, Height / 2); // Start particles from the map center

        // Perpendicular vector for particle alignment relative to the laser direction
        Vector2 velocity = beamInfo.VelocityVector.ToNumericsVector2();
        Vector2 perpendicularVector = new Vector2(-velocity.Y, velocity.X);
        perpendicularVector = Vector2.Normalize(perpendicularVector);
        // Seed particles in the smaller map

        for (int i = -length / 2; i <= length / 2; i++)
        {
            Vector2 relativePosition = perpendicularVector * i;   // Step particles along the perpendicular
            Vector2 particlePosition = localOrigin + relativePosition; // Centered in local map space

            int mapX = (int)Math.Round(particlePosition.X);
            int mapY = (int)Math.Round(particlePosition.Y);

            if (mapX >= 0 && mapX < Width && mapY >= 0 && mapY < Height) // Bounds check for small map
            {
                BeamPoint newBP = new BeamPoint(beamInfo, particlePosition, lifetime);
                BeamStarts.Add(newBP);
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
        
        int centerY = Height / 2;
        int currentX = _pixBuf; // Start at the buffer size for the left side
        List<(int x, int y)> armorVertex = new();
        armorVertex.Add((currentX , centerY));
        currentX += _armorHeadspace;
        int partSizesIndex = 0;
        foreach (var partSize in partSizes)
        {
            
            string typeID = partSize.typeID;
            List<ComponentInstance> instanceIDs = componentInstances[typeID];

            ComponentDesign componentDesign = lib[typeID];
            var mats = ParticleHelpers.GetMaterialsList(modData, componentDesign);
            
            int partHeight = (int)Math.Round(partSize.height);
            int partLength = (int)Math.Round(partSize.len);
            
            armorVertex.Add((partLength, partHeight / 2));
            int stackCenterY = centerY - ((partSize.count * partHeight) / 2);
            for (int i = 0; i < partSize.count; i++)
            {
                string instanceID = instanceIDs[i].UniqueID; // Get the corresponding instanceID
                int actualY = stackCenterY + (partHeight * i);
                (int x, int y) position = (currentX, actualY);
                (int x, int y) size = (partLength, partHeight);
                int totalParticles = 0;
                for (int y = 0; y < partHeight; y++)
                {
                    if (actualY + y >= Height)
                        throw new Exception("Outside the height of the array.(more than height)");
                    if(actualY + y < 0)
                        throw new Exception("Outside the height of the array. (less than 0)");
                    for (int x = 0; x < partLength; x++)
                    {
                        int index = GetIndex(currentX + x, actualY + y);
                        var mat = ParticleHelpers.GetRandomMat(mats, rng);
                        Vector2 pos = new Vector2(currentX + x, actualY + y);
                        Vector2 vel = Vector2.Zero;
                        float pressure = 1f;
                        compIDMap[index] = _nextComponentID;
                        var newPart = new PhysicalParticle(_nextComponentID, mat, pos, vel, Scale);
                        newPart.mapIndex = index;
                        PMap[index] = newPart;
                        PresMap[index] = pressure;
                        totalParticles++;
                    }
                }

                componentData[instanceID] = (position, size, totalParticles);
                AddComponentID(instanceID);//, htkPerParticle);
            }

            // Increment currentX by the length of the part for the next placement
            currentX += partLength;
            // Check if we've gone beyond the width of the map, if so, throw
            if (currentX > Width) // Check against Width 
            {
                throw new Exception("trying to place items out of bounds of damage map");
            }

            partSizesIndex++;
        }
        
        
        //TODO: this is a placeholder!!! need to rework how we're storing armor in the ship construction. 
        ParticleMaterial amMat = new ParticleMaterial()
        {
            TensileStrength = 110,
            Elasticity = 0.5f,
            ThermalCapacity = 900,
            ThermalConductivity = 237,
            MeltingZeroPoint = 933.47f,
            TriplePoint = new PhasePoint(0.00001f, 933.47f),
            CriticalPoint = new PhasePoint(1150, 7500),
            Density = 7874
        };
        for (int i = 1; i < armorVertex.Count; i++)
        {
            DrawArmor(this, armorVertex[i - 1], armorVertex[i], amMat, 2);
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
        PMap = new PhysicalParticle[arraylen];
        PresMap = new float[arraylen];
        compIDMap = new int[arraylen];
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

    public int GetIndex(PhysicalParticle particle)
    {
        return (int)(Math.Round(particle.Position.Y) * Width + Math.Round(particle.Position.X));
    }

    public (int x, int y) GetPosition(int index)
    {
        return (index % Width, index / Width);
    }

    public PhysicalParticle[] GetImediateParticles(PhysicalParticle particle)
    {
        var array = new PhysicalParticle[9];
        var ctr = GetIndex(particle);
        array[0] = PMap[ctr - Width -1];
        array[1] = PMap[ctr - Width];
        array[2] = PMap[ctr - Width + 1];
        array[3] = PMap[ctr - 1];
        array[4] = PMap[ctr];
        array[5] = PMap[ctr + 1];
        array[6] = PMap[ctr + Width - 1];
        array[7] = PMap[ctr + Width];
        array[8] = PMap[ctr + Width + 1];
        return array;
        
    }
    
    private static void DrawArmor(DamageMap map, (int x, int y) coordStart, (int x, int y) coordEnd, ParticleMaterial mat, float thickness)
    {
        var x0 = coordStart.x;
        var y0 = coordStart.y;
        var x1 = coordEnd.x;
        var y1 = coordEnd.y;

        int delatx = Math.Abs(x1 - x0);
        int delaty = y1 - y0;
        double slope = Math.Abs((double)delaty / (double)delatx);
        double signedslope = (double)delaty / (double)delatx;
        double perpslope = 1 / signedslope;

        //double dwidth = (double)(width);

        int vmargin = (int)(thickness / 2);
        double dwidth = (double)(thickness) / Math.Sin(Math.Atan(1/slope));
        
        
        for (int yoffset = -(int)(dwidth / 2); yoffset < (int)(dwidth / 2); yoffset++)
        {

            int rx0 = x0;// - (int)((double)yoffset * signedslope);
            int ry0 = y0 + yoffset;
            int rx1 = x1;// - (int)((double)yoffset * signedslope);
            int ry1 = y1 + yoffset;

            for (int i = rx0; i < rx1; i++)
            {
                int currentx = Math.Abs(rx1 - i);
                double progress = (double)currentx / (double)delatx;
                int pixx = i;
                int pixy = ry1 - (int)(progress * delaty);
                if (pixy > Math.Max(y1, y0) + vmargin)
                    break;
                if (pixy < Math.Min(y1, y0) - vmargin)
                    break;

                Vector2 pos = new Vector2(pixx, pixy - map._armorHeadspace);
               
                var pmapIndex = map.GetIndex(pos);
                map.compIDMap[pmapIndex] = map._nextComponentID;
                var newPart = new PhysicalParticle(map._nextComponentID, mat, pos, Vector2.Zero, map.Scale);
                newPart.mapIndex = pmapIndex;
                map.PMap[pmapIndex] = newPart;

            }
        }
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
        int[] newIDMap = new int[newWidth * newHeight];
        PhysicalParticle[] newPMap = new PhysicalParticle[newWidth * newHeight];
        float[] newPresMap = new float[newWidth * newHeight];
        var newComponentData = new Dictionary<string, ((int,int) Position, (int,int) Size, int TotalParticles)>();
        // Offset for placing particles from this map
        int offsetX = expandX < 0 ? otherMap.Width : 0;
        int offsetY = expandY < 0 ? otherMap.Height : 0;

        foreach (var component in componentData)
        {
            string instanceID = component.Key;
            var (position, size, totalParticles) = component.Value;
            (int,int) newPosition = (position.x + offsetX, position.y + offsetY);
            newComponentData[instanceID] = (newPosition, size, totalParticles);
        }
        
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
                    var p = PMap[oldIndex];
                    p.mapIndex = newIndex;
                    newPMap[newIndex] = p;
                    var tempPosition = newPMap[newIndex].Position;
                    tempPosition.X += offsetX;
                    tempPosition.Y += offsetY;
                    newPMap[newIndex].Position = tempPosition;
                }
                
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
                    newPresMap[newIndex] = otherMap.PresMap[otherIndex];
                    if (otherMap.PMap[otherIndex] != null)
                    {
                        var p = otherMap.PMap[otherIndex];
                        p.mapIndex = newIndex;
                        newPMap[newIndex] = p;
                        newPMap[newIndex].Position = new(newX, newY);
                    }
                    
                }
            }
        }
        
        foreach (var bp in otherMap.BeamStarts)
        {
            var x = bp.Position.X + otherMap.X + offsetX;
            var y = bp.Position.Y + otherMap.Y + offsetY;
            bp.Position = new Vector2(x,y);
        }
       
        foreach (var otherComponent in otherMap.componentData)
        {
            string instanceID = otherComponent.Key;
            var (otherPosition, otherSize, otherTotalParticles) = otherComponent.Value;
            (int,int) newPosition = (otherPosition.x + otherMap.X + offsetX, otherPosition.y + otherMap.Y + offsetY);

            // If this component ID already exists, we'll merge damage or you might decide to handle conflicts differently
            if (newComponentData.ContainsKey(instanceID))
            {
                // Merge damage - this is a simple approach, might need refinement based on your needs
                var (currentPosition, currentSize, currentTotalParticles) = newComponentData[instanceID];
                newComponentData[instanceID] = (currentPosition, currentSize, currentTotalParticles);
            }
            else
            {
                newComponentData[instanceID] = (newPosition, otherSize, otherTotalParticles);
            }
        }

        // Update map properties
        compIDMap = newIDMap;
        PMap = newPMap;
        PresMap = newPresMap;
        Width = newWidth;
        Height = newHeight;
        componentData = newComponentData;
        BeamStarts = otherMap.BeamStarts;
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
            bool isOutOfBounds = DamagePhysicsSim.IsOutOfBounds(part, map);
            bool isDeleted = part.IsDeleted;
            if(index > map.PMap.Length - 1)
            {
                throw new IndexOutOfRangeException(pos.ToString());
            }
            if(index < 0)
            {
                throw new IndexOutOfRangeException(pos.ToString());
            }
            var isSame = map.PMap[index] == part; 
            //if (!isSame)
                //throw new Exception("out of position");
        }
    }
    
    public static List<PhysicalParticle> GetNeighboringParticles(DamageMap map, Vector2 position, float radius)
    {
        List<PhysicalParticle> neighbors = new List<PhysicalParticle>();
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