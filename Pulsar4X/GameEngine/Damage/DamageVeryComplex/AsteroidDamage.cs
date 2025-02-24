using System;
using System.Collections.Generic;
using System.Numerics;
using Pulsar4X.Engine;
using Pulsar4X.Galaxy;
using Pulsar4X.Sensors;

namespace GameEngine.Damage;

public partial class DamageMap
{
    /// <summary>
    /// Creates a new asteroid map with a circular shape and optional jitter on the radius.
    /// </summary>
    /// <param name="posX">The x position of the asteroid in the map.</param>
    /// <param name="posY">The y position of the asteroid in the map.</param>
    /// <param name="radius">The average radius of the asteroid.</param>
    /// <param name="jitter">The amount of jitter to apply to the radius. Default: 0.</param>
    /// <param name="numPoints">Number of points to define the circumference of the asteroid. Default: 32.</param>
    /// <param name="material">Material type to be applied to the particles.</param>
    public DamageMap(Entity entity, SystemBodyInfoDB asteroidInfo)
    {
        var mdb = entity.GetDataBlob<MassVolumeDB>();
        var area = DamageMapHelpers.AreaFromVolume(mdb.Volume_m3, ParticlesPerMeter);
        float radius = (float)Math.Sqrt(area / Math.PI);
        float jitter = 0.5f;
        int numPoints = 32;
        var seed = entity.Manager.RNG.Next();
        Width = Height = (int)(radius * 2) + 1; // Ensure the map is large enough to contain the asteroid

        PMap = new PhysicalParticle[Width * Height];
        compIDMap = new int[Width * Height];
        PresMap = new float[Width * Height];
        
        // Generate the asteroid shape
        List<Vector2> vertices = AsteroidHelpers.GenerateAsteroidShape(radius, jitter, numPoints);
        AsteroidHelpers.AsteroidDamageProfile(this, radius, 30, entity.Manager.RNG);
        //AsteroidHelpers.FillAsteroidShape(this, vertices, radius, AsteroidHelpers.GetMats(seed));
    }
}

public static class AsteroidHelpers
{
    public static List<(ParticleMaterial partMat, float percent)> GetMats(int seed)
    {
        Random random = new Random(seed);

        // Example distribution percentages (you can tweak these)
        float totalPercentage = 100.0f;
        float icePercent = (float)random.NextDouble() * 20 + 10; // 10%-30% ice
        float stonePercent = (float)random.NextDouble() * 40 + 30; // 30%-70% stone
        float ironPercent = (float)random.NextDouble() * 20 + 10; // 10%-30% iron
        float nickelPercent = totalPercentage - (icePercent + stonePercent + ironPercent); // Remaining percentage to ensure total is 100%

        // Ensure percentages sum up to 100
        List<float> percentages = new() { icePercent, stonePercent, ironPercent, nickelPercent };

        // Materials
        List<(ParticleMaterial partMat, float percent)> mats = new();
        
        // Add water (ice)
        mats.Add((new ParticleMaterial
        {
            PartMatID = 100,
            ThermalCapacity = 4200,
            ThermalConductivity = 0.6089f,
            MeltingZeroPoint = -60,
            TriplePoint = new PhasePoint(0.0061f, 273.16f),
            CriticalPoint = new PhasePoint(220.6f, 647.096f),
            PhotonReflectivity = new EMWaveForm(400, 500, 600),
            PhotonReflectivityPeak = 0.07f,
            PhotonTransparency = new EMWaveForm(400, 650, 1100),
            PhotonTransperencyPeak = 0.92f,
            Density = 917, // Ice density in kg/m3
            Elasticity = 0.3f,
            TensileStrength = 2 // MPa
        }, percentages[0])); // Assign percentage for ice

        // Add stone material
        mats.Add((new ParticleMaterial
        {
            PartMatID = 101,
            ThermalCapacity = 800,
            ThermalConductivity = 2.5f,
            MeltingZeroPoint = 1400,
            TriplePoint = new PhasePoint(0.001f, 1400),
            CriticalPoint = new PhasePoint(3000, 2000),
            PhotonReflectivity = new EMWaveForm(400, 600, 800),
            PhotonReflectivityPeak = 0.5f,
            PhotonTransparency = new EMWaveForm(700, 1000, 1200),
            PhotonTransperencyPeak = 0.1f,
            Density = 2500, // Average stone density in kg/m3
            Elasticity = 0.7f,
            TensileStrength = 20 // MPa
        }, percentages[1])); // Assign percentage for stone

        // Add iron
        mats.Add((new ParticleMaterial
        {
            PartMatID = 102,
            ThermalCapacity = 440,
            ThermalConductivity = 80,
            MeltingZeroPoint = 1811,
            TriplePoint = new PhasePoint(0.000001f, 1811),
            CriticalPoint = new PhasePoint(100000, 5000),
            PhotonReflectivity = new EMWaveForm(300, 550, 700),
            PhotonReflectivityPeak = 0.8f,
            PhotonTransparency = new EMWaveForm(800, 1300, 1600),
            PhotonTransperencyPeak = 0.05f,
            Density = 7874, // Iron density in kg/m3
            Elasticity = 0.25f,
            TensileStrength = 300 // MPa
        }, percentages[2])); // Assign percentage for iron

        // Add other material - e.g., nickel
        mats.Add((new ParticleMaterial
        {
            PartMatID = 103,
            ThermalCapacity = 440,
            ThermalConductivity = 90,
            MeltingZeroPoint = 1728,
            TriplePoint = new PhasePoint(0.000001f, 1728),
            CriticalPoint = new PhasePoint(70000, 4000),
            PhotonReflectivity = new EMWaveForm(300, 500, 700),
            PhotonReflectivityPeak = 0.7f,
            PhotonTransparency = new EMWaveForm(900, 1200, 1600),
            PhotonTransperencyPeak = 0.04f,
            Density = 8908, // Nickel density in kg/m3
            Elasticity = 0.3f,
            TensileStrength = 160 // MPa
        }, percentages[3])); // Assign percentage for nickel

        return mats;
    }



    /// <summary>
    /// Generates a circular shape with optional jitter for the vertex points.
    /// </summary>
    /// <param name="radius">The radius of the circle.</param>
    /// <param name="jitter">The amount of deviation to apply to the radius of each point.</param>
    /// <param name="numPoints">Number of points around the circle.</param>
    /// <returns>List of vertices defining the perimeter of the shape.</returns>
    public static List<Vector2> GenerateAsteroidShape(float radius, float jitter, int numPoints)
    {
        List<Vector2> vertices = new();
        Random rnd = new();

        for (int i = 0; i < numPoints; i++)
        {
            double angle = (2 * Math.PI / numPoints) * i;
            float randomizedRadius = radius + ((float)rnd.NextDouble() * 2 - 1) * jitter; // Add jitter to radius
            float x = randomizedRadius * (float)Math.Cos(angle);
            float y = randomizedRadius * (float)Math.Sin(angle);
            vertices.Add(new Vector2(x, y));
        }

        return vertices;
    }
    

    public static void AsteroidDamageProfile(DamageMap map, double avgRadius, int irregularity, Random rng)
    {
        int segments = 8;
        double avgAngle = Math.PI  / segments;
        double angle = Math.PI;
        
        List<(int x, int y)> lineL = new List<(int x, int y)>();
        List<(int x, int y)> lineR = new List<(int x, int y)>();

        var startL = ((int)avgRadius, 0);
        var startR = ((int)avgRadius, 0);

        for (int i = 0; i < segments + 1; i++)
        {
            int jitterxL = rng.Next(0, irregularity);
            int jitteryL = rng.Next(-irregularity, irregularity);
            int jitterxR = rng.Next(0, irregularity);
            int jitteryR = rng.Next(-irregularity, irregularity);

            double x = avgRadius * Math.Sin(angle);
            double y = avgRadius * Math.Cos(angle);

            int xL = (int)(-x + avgRadius + jitteryL);
            int xR = (int)(x + avgRadius + jitteryR);
            int yL = (int)(y + avgRadius + jitterxL);
            int yR = (int)(y + avgRadius + jitterxR);

            BresenhamPoints(startL, (xL, yL), ref lineL);
            BresenhamPoints(startR, (xR, yR), ref lineR);

            startL = (xL, yL);
            startR = (xR, yR);
            angle -= avgAngle;

        }





        byte r = byte.MaxValue;
        byte g = byte.MaxValue;
        byte b = byte.MaxValue;
        byte a = byte.MaxValue;

        //fill an array with the same colour for buffer.blockcopy.
        byte[] px = new byte[4]{r,g,b,a};
        byte[] pxarray = new byte[map.Width * 4];
        for (int i = 0; i < map.Width; i++)
        {
            //wonder if I can use the destination as the source, and double the amount I'm copying each time.
            Buffer.BlockCopy(px, 0, pxarray, i, 4);
        }

        int height = map.Height;
        int indexl = 0;
        int indexr = 0;
        for (int i = 0; i < height; i++)
        {
            int ypos = i;

            while (indexl < lineL.Count -1  && lineL[indexl].y == ypos)
                indexl++;

            while (indexr < lineR.Count -1 && lineR[indexr].y == ypos)
                indexr++;


            int leftx = lineL[indexl].x;
            int rightx = lineR[indexr].x;

            int width = rightx - leftx;

            //Buffer.BlockCopy(pxarray, 0, dmgProfile.ByteArray, leftx, width); //this should be faster, but need to debug it.


            //below is a slower but easier to write way of filling the wanted line with colour.
            for (int j = 0; j <  width; j++)
            {
                int xpos = leftx + j;
                Vector2 pos = new Vector2(xpos, ypos);
                var ice = new ParticleMaterial
                {
                    PartMatID = 100,
                    ThermalCapacity = 4200,
                    ThermalConductivity = 0.6089f,
                    MeltingZeroPoint = -60,
                    TriplePoint = new PhasePoint(0.0061f, 273.16f),
                    CriticalPoint = new PhasePoint(220.6f, 647.096f),
                    PhotonReflectivity = new EMWaveForm(400, 500, 600),
                    PhotonReflectivityPeak = 0.07f,
                    PhotonTransparency = new EMWaveForm(400, 650, 1100),
                    PhotonTransperencyPeak = 0.92f,
                    Density = 917, // Ice density in kg/m3
                    Elasticity = 0.3f,
                    TensileStrength = 2 // MPa
                };
                var newPart = new PhysicalParticle(map._nextComponentID, ice, pos, Vector2.Zero, map.ParticlesPerMeter);
                map.PMap[map.GetIndex(pos)] = newPart;

            }
        }
        
    }

    private static void BresenhamPoints((int x, int y) start,(int x, int y) end, ref List<(int x, int y)> list)
    {
        int x = start.x;
        int y = start.y;
        int x2 = end.x;
        int y2 = end.y;

        int w = x2 - x ;
        int h = y2 - y ;
        int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0 ;
        if (w<0) dx1 = -1 ; else if (w>0) dx1 = 1 ;
        if (h<0) dy1 = -1 ; else if (h>0) dy1 = 1 ;
        if (w<0) dx2 = -1 ; else if (w>0) dx2 = 1 ;
        int longest = Math.Abs(w) ;
        int shortest = Math.Abs(h) ;
        if (!(longest>shortest))
        {
            longest = Math.Abs(h) ;
            shortest = Math.Abs(w) ;
            if (h<0)
                dy2 = -1 ;
            else if (h>0)
                dy2 = 1 ;
            dx2 = 0 ;
        }
        int numerator = longest >> 1 ;
        for (int i=0;i<=longest;i++) {
            list.Add((x,y));
            numerator += shortest ;
            if (!(numerator<longest)) {
                numerator -= longest ;
                x += dx1 ;
                y += dy1 ;
            } else {
                x += dx2 ;
                y += dy2 ;
            }
        }
    }

}
