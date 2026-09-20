using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;
using Pulsar4X.Components;
using Pulsar4X.Damage;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Helpers;
using Pulsar4X.Ships;

//using Pulsar4X.Orbital;
using Pulsar4X.Weapons;

namespace GameEngine.Damage;

public partial class DamageMap
{
    public TimeSpan RunTime = TimeSpan.Zero;
    private Dictionary<string, ushort> componentIDLookup = new();

    
    public Dictionary<string, ((int x, int y) Position, (int x, int y) Size, int totalParticles)> componentData = new();
    
    internal ushort _nextComponentID = 0;

    public double TotalEnergy = 0;
    public int PhysicsScale = 1000;//will scale individual particles to this for physics interactions
    public int ParticlesPerMeter = 100; //default non physics scale. 
    int _pixBuf = 10; //this is just how much space we're leaving around the edges. 
    private int _armorHeadspace = 2; //space between skin and componenents.
    public PhysicalParticle[] PMap;
    public List<BeamPoint> BeamStarts = new();
    public List<BeamPoint> BeamPoints = new();
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
                    var newPart = new PhysicalParticle(_nextComponentID, material, new Vector2(x,y), velocity, ParticlesPerMeter);
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

    internal DamageMap(DamageMap map, PhysicalParticle part)
    {
        ParticlesPerMeter = PhysicsScale;
        var pos = map.GetPosition(part.mapIndex);
        X = pos.x;
        Y = pos.y;
        Height = ParticlesPerMeter;
        Width = ParticlesPerMeter;
        compIDMap = new int[Width * Height];
        PMap = new PhysicalParticle[Width * Height];
        PresMap = new float[Width * Height];
        
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                int index = y * Width + x;
                compIDMap[index] = part.compID;
                var newPart = new PhysicalParticle(part.compID, part.MatType, new Vector2(x,y), part.Velocity, ParticlesPerMeter);
                newPart.mapIndex = index;
                newPart.Temperature = part.Temperature;
                PMap[index] = newPart;
                PresMap[index] = map.PresMap[part.mapIndex];
                
            }
        }
        part.DMap = this;
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

    
    public DamageMap(Entity shipEntity, ShipDesign design)
    {
        Random rng = shipEntity.Manager.RNG;
        var modData = shipEntity.Manager.Game.StartingGameData;
 
        var armor = design.Armor;
        List<(ComponentDesign design, int count)> placementOrder = design.Components;
        List<(ComponentDesign design, float len, int height, int count)> partSizes = SetSize(placementOrder, ParticlesPerMeter);
        Dictionary<string, List<ComponentInstance>> componentInstances = shipEntity.GetDataBlob<ComponentInstancesDB>().ComponentsByDesign;
        
        int centerY = Height / 2;
        int currentX = _pixBuf + _armorHeadspace; // Start at the buffer size for the left side
        
        int partSizesIndex = 0;
        foreach (var partSize in partSizes)
        {
            List<ComponentInstance> instanceIDs = componentInstances[partSize.design.UniqueID];
            var mats = ParticleHelpers.GetMaterialsList(modData, partSize.design);
            
            int partHeight = partSize.height;
            int partLength = (int)Math.Round(partSize.len);
            int evenStackHeight = partHeight * partSize.count;
            if (int.IsOddInteger(evenStackHeight))
                evenStackHeight++;
            int stackCenterY = centerY - evenStackHeight / 2;
            
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
                        var newPart = new PhysicalParticle(_nextComponentID, mat, pos, vel, ParticlesPerMeter);
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
        
        List<int> lineHeight = new List<int>();

        int numparts = partSizes.Count;
        
        //Grabs the height of each transtion between parts

        
        int height0 = 0;
        int height1 =(partSizes[0].height * partSizes[0].count / 2);
        for (int partnum = 0; partnum < numparts - 1; partnum++)
        {
            if (height0 > height1)
                lineHeight.Add( height0);
            else
                lineHeight.Add( height1);
            height0 = partSizes[partnum].height * partSizes[partnum].count / 2;
            height1 = partSizes[partnum + 1].height * partSizes[partnum + 1].count / 2;
        }
        lineHeight.Add( height1);
        
        List<(int x, int y)> armorVertex = new();
        int currentx = _pixBuf - (int)(armor.thickness);
        armorVertex.Add((currentx, 0));
        armorVertex.Add((currentx, lineHeight[0] + _armorHeadspace));
        currentx += (int)Math.Round(partSizes[0].len) + _armorHeadspace;
        
        for (int partnum = 1; partnum < numparts; partnum++)
        {
            armorVertex.Add((currentx, lineHeight[partnum] + _armorHeadspace));
            currentx += (int)Math.Round(partSizes[partnum].len);
        }

        
        armorVertex.Add((currentx, lineHeight[numparts-1] + _armorHeadspace));
        
        ParticleMaterial amMat = new ParticleMaterial()
        {
            TensileStrength = armor.type.TensileStrength,
            Elasticity = armor.type.Elasticity,
            ThermalCapacity = armor.type.ThermalCapacity,
            ThermalConductivity = armor.type.ThermalConductivity,
            MeltingZeroPoint = armor.type.MeltingZeroPoint,
            TriplePoint = armor.type.TriplePoint,
            CriticalPoint = armor.type.CriticalPoint,
            Density = armor.type.Density,
            PhotonReflectivity = armor.type.PhotonReflectivity,
            PhotonReflectivityPeak = armor.type.PhotonReflectivityPeak,
            PhotonTransparency = armor.type.PhotonTransparency,
            PhotonTransperencyPeak = armor.type.PhotonTransparencyPeak
        };

        for (int index = 0; index < armorVertex.Count-1; index++)
        {
            (int x, int y) av0 = armorVertex[index];
            (int x, int y) av1 = armorVertex[index+1];
            DrawWuArmorSegment(this, av0, av1,armor.thickness / ParticlesPerMeter, amMat);
        }
    }
    
    private static void DrawWuArmorSegment(DamageMap map, (int x, int y) coordStart, (int x, int y) coordEnd, float thickness, ParticleMaterial mat)
    {
        thickness = 0.5f;
        var x0 = coordStart.x;
        var y0 = coordStart.y;
        var x1 = coordEnd.x;
        var y1 = coordEnd.y;
        int centerY = map.Height / 2;
        int deltax = Math.Abs(x1 - x0);
        int deltay = Math.Abs(y1 - y0);
        
        List<(float x, float y, float alpha)> points = new();

        if (deltax > deltay)
        {
            if (x0 > x1) { (x0, x1) = (x1, x0); (y0, y1) = (y1, y0); }
            float gradient = (float)(y1 - y0) / deltax;
            float y = y0 + gradient;

            for (int x = x0; x <= x1; x++)
            {
                //AddPoint(points, x, (int)y, 1 - (y - (int)y));
                points.Add((x, y, 1 - (y - (int)y)));
                //AddPoint(points, x, (int)y + 1, y - (int)y);
                points.Add((x, y + 1, y - (int)y));
                y += gradient;
            }
        }
        else
        {
            if (y0 > y1) { (x0, x1) = (x1, x0); (y0, y1) = (y1, y0); }
            float gradient = (float)(x1 - x0) / deltay;
            float x = x0 + gradient;

            for (int y = y0; y <= y1; y++)
            {
                //AddPoint(points, (int)x, y, 1 - (x - (int)x));
                points.Add((x, y, 1 - (x - (int)x)));
                //AddPoint(points, (int)x + 1, y, x - (int)x);
                points.Add((x + 1, y, x - (int)x));
                x += gradient;
            }
        }

        // Adjust alpha based on thickness
        float maxAlpha = thickness < 1 ? thickness : 1;

        foreach (var point in points)
        {
            for (int i = -(int)(thickness / 2); i <= (int)(thickness / 2); i++)
            {
                if(point.alpha == 0)
                    continue;
                DrawPoint(map, centerY, point.x, point.y + i, point.alpha * maxAlpha, mat);
                DrawPoint(map, centerY, point.x, -(point.y + i + 1), point.alpha * maxAlpha, mat); // Mirroring
            }
        }
    }
    

    private static void DrawPoint(DamageMap map, int centerY, float x, float y, float alpha, ParticleMaterial mat)
    {
        Vector2 pos = new Vector2(x, centerY + y);
        var pmapIndex = map.GetIndex(pos);
        map.compIDMap[pmapIndex] = map._nextComponentID;
        
        var newPart = new PhysicalParticle(map._nextComponentID, mat, pos, Vector2.Zero, map.ParticlesPerMeter)
        {
            mapIndex = pmapIndex
        };
        // Adjust mass based on alpha
        newPart.Mass *= alpha;
        map.PMap[pmapIndex] = newPart;
    }
    
    private List<(ComponentDesign design, float len, int height, int count)> SetSize(List<(ComponentDesign design, int count)> po, int scale )
    {
        List<(ComponentDesign design, float len, int height, int count)> partsize = new();
        int componentWidthNum = 0;

        int totalLen = 0;
        int totalHeight = 0;

        byte componentInstance = 0;

        for (int i = 0; i < po.Count; i++)
        {
            var count = po[i].count;
            var compSize= DamageMapHelpers.GetComponentSize(po[i].design, scale);
            
            var evenHeight = (int)Math.Ceiling(compSize.height);
            if (int.IsOddInteger(evenHeight)) //make heights even to simplify placement. 
                evenHeight++;
            partsize.Add((po[i].design, compSize.length, evenHeight, count));
            if (count > componentWidthNum)
                componentWidthNum = count;
            totalLen += (int)Math.Ceiling(compSize.length);
            int height = evenHeight * count;
            if (height > totalHeight)
            {
                totalHeight = height;
            }
        }
        
        Height = totalHeight + _pixBuf * 2; //create a bit larger canvas size for the armor.
        Width = totalLen + _pixBuf * 2;
        
        //lets make it always even for consistancy.
        if (int.IsOddInteger(Height))
            Height++;
        if (int.IsOddInteger(Width))
            Width++;
        
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
    

    
    public static T GetItem<T>(object[] ary, int aryWid, int x, int y)
    {
        int row = y * aryWid;
        int col = x;
        return (T)ary[row + col];
    }


    public void MergeAndResize(DamageMap otherMap)
    {

        if (ParticlesPerMeter != otherMap.ParticlesPerMeter)
        {
            // Assume 'this' is the target resolution (e.g., low-res ship)
            DamageMap downscaledMap = DamageMapHelpers.DownscaleHighResMap(otherMap, ParticlesPerMeter);
            MergeAndResize(downscaledMap); // Recursive call with matching resolutions
        }
        else
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
            var newComponentData = new Dictionary<string, ((int, int) Position, (int, int) Size, int TotalParticles)>();
            // Offset for placing particles from this map
            int offsetX = expandX < 0 ? otherMap.Width : 0;
            int offsetY = expandY < 0 ? otherMap.Height : 0;

            foreach (var component in componentData)
            {
                string instanceID = component.Key;
                var (position, size, totalParticles) = component.Value;
                (int, int) newPosition = (position.x + offsetX, position.y + offsetY);
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
                bp.Position = new Vector2(x, y);
            }

            foreach (var otherComponent in otherMap.componentData)
            {
                string instanceID = otherComponent.Key;
                var (otherPosition, otherSize, otherTotalParticles) = otherComponent.Value;
                (int, int) newPosition = (otherPosition.x + otherMap.X + offsetX, otherPosition.y + otherMap.Y + offsetY);

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
}

public static class DamageMapHelpers
{
    public static double AreaFromVolume(double volm3, int scale)
    {
        return Math.Cbrt(volm3) * scale;
    }
    public static (float length, float height) GetComponentSize(ComponentDesign componentDeign, int scale)
    {
        var volm3 = componentDeign.VolumePerUnit;
        var area = AreaFromVolume(volm3, scale);
        float length = (float)Math.Sqrt(area * componentDeign.AspectRatio);
        float height = (float)(area / length);
        return (length, height);
    }
    
    internal static DamageMap DownscaleHighResMap(DamageMap highResMap, int targetPPM)
    {
        float scaleFactor = (float)highResMap.ParticlesPerMeter / targetPPM; // e.g., 1000/10 = 100
        int newWidth = (int)(highResMap.Width / scaleFactor);
        int newHeight = (int)(highResMap.Height / scaleFactor);

        // Create a new map at the target (low) resolution
        DamageMap downscaledMap = new DamageMap(newWidth, newHeight);
        downscaledMap.X = highResMap.X / (int)scaleFactor;
        downscaledMap.Y = highResMap.Y / (int)scaleFactor;
        
        downscaledMap.ParticlesPerMeter = targetPPM;

        // Aggregate high-res particles into low-res grid
        for (int y = 0; y < newHeight; y++)
        {
            for (int x = 0; x < newWidth; x++)
            {
                int lowIndex = downscaledMap.GetIndex(x, y);
                int highXBase = (int)(x * scaleFactor);
                int highYBase = (int)(y * scaleFactor);
                int blockSize = (int)scaleFactor;

                Vector2 avgVelocity = Vector2.Zero;
                float totalMass = 0;
                float avgTemp = 0;
                float avgPressure = 0;
                int particleCount = 0;
                PhysicalParticle firstParticle = null;

                // Collect stats from the high-res block
                for (int dy = 0; dy < blockSize && (highYBase + dy) < highResMap.Height; dy++)
                {
                    for (int dx = 0; dx < blockSize && (highXBase + dx) < highResMap.Width; dx++)
                    {
                        int highIndex = highResMap.GetIndex(highXBase + dx, highYBase + dy);
                        if (highResMap.PMap[highIndex] != null)
                        {
                            var p = highResMap.PMap[highIndex];
                            if (firstParticle == null) firstParticle = p; // Use first for ID and material
                            avgVelocity += p.Velocity;
                            totalMass += p.Mass;
                            avgTemp += p.Temperature;
                            avgPressure += highResMap.PresMap[highIndex];
                            particleCount++;
                        }
                    }
                }

                // If there’s at least one particle, create a downscaled version
                if (particleCount > 0 && firstParticle != null)
                {
                    var newPart = new PhysicalParticle(
                        firstParticle.compID,
                        firstParticle.MatType,
                        new Vector2(x, y),
                        avgVelocity / particleCount,
                        targetPPM
                    );
                    newPart.mapIndex = lowIndex;
                    newPart.Mass = totalMass; // Sum mass, don’t average
                    newPart.Temperature = avgTemp / particleCount;
                    newPart.DMap = firstParticle.DMap; // Preserve the high-res DamageMap if it exists
                    downscaledMap.PMap[lowIndex] = newPart;
                    downscaledMap.compIDMap[lowIndex] = firstParticle.compID;
                    downscaledMap.PresMap[lowIndex] = avgPressure / particleCount;
                }
            }
        }

        // Copy component data (simplified, adjust as needed)
        downscaledMap.componentData = new Dictionary<string, ((int, int) Position, (int, int) Size, int TotalParticles)>(highResMap.componentData);
        downscaledMap.BeamStarts = new List<BeamPoint>(highResMap.BeamStarts); // Copy beams, could downscale positions if needed

        return downscaledMap;
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