using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using GameEngine.Damage;

namespace Pulsar4X.Client;

public static class DamageMapRendering
{



    public static void CreateSDLTextures(IntPtr renderer, DamageMap damageMap, ref IntPtr[] textures)
    {
        int width = damageMap.Width;
        int height = damageMap.Height;
        CreateTextureForIDMap(renderer, damageMap, ref textures[0], width, height);
        CreateTextureForPresMap(renderer, damageMap, ref textures[1], width, height);
        CreateTextureForVMap(renderer, damageMap, ref textures[2], width, height);
        CreateTextureForPMap(renderer, damageMap, ref textures[3], width, height);
        CreateTextureForTemp(renderer, damageMap, ref textures[4], width, height);
        CreateTextureForPhaseState(renderer, damageMap, ref textures[5], width, height);
        CreateTextureForBeamPoints(renderer, damageMap, ref textures[6], width, height);
        CreateTextureForCompisiteMap(renderer, damageMap, ref textures[7], width, height);
    }

    internal static void CreateTextureForIDMap(IntPtr renderer, DamageMap damageMap, ref IntPtr texture, int width, int height)
    {
        byte alpha = 255;
        // Create a buffer for the pixel data
        uint[] pixelData = new uint[width * height];
        // Get unique instances for color mapping
        var uniqueInstances = damageMap.compIDMap.Distinct().Where(id => id != null).ToList();
        // Fill the pixel data
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = damageMap.GetIndex(x, y);
                int id = damageMap.compIDMap[index];

                // Calculate red value based on instance index
                byte redValue = id != null
                    ? (byte)(255 * uniqueInstances.IndexOf(id) / (float)uniqueInstances.Count)
                    : (byte)0;

                // Pack ARGB values into a single uint
                // Note: OpenGL expects RGBA format, so we need to swap the byte order
                pixelData[y * width + x] = Utils.GetColor(redValue, 0, 0, alpha);
            }
        }

        GCHandle handle = GCHandle.Alloc(pixelData, GCHandleType.Pinned);
        try
        {
            IntPtr pixels = handle.AddrOfPinnedObject();
            // Update the texture
            Textures.UpdateOrCreate(renderer, ref texture, width, height, pixels);
        }
        finally
        {
            handle.Free();
        }
    }

    internal static void CreateTextureForFastestParticleRegion(
        IntPtr renderer,
        DamageMap baseMap, // Top-level low-res map
        PhysicalParticle fastestParticle,
        ref IntPtr texture,
        int textureSize) // Size in pixels (e.g., 64 for a 64x64 texture)
    {
        // Verify fastestParticle is in baseMap (assumption check)
        bool foundInBase = Array.Exists(baseMap.PMap, p => p == fastestParticle);
        if (!foundInBase)
            throw new Exception($"Fastest particle (ID {fastestParticle.ID}) not found in baseMap.PMap");

        // Resolution setup
        int highResPPM = baseMap.PhysicsScale; // e.g., 1000
        int lowResPPM = baseMap.ParticlesPerMeter; // e.g., 10
        float scaleFactor = (float)highResPPM / lowResPPM; // e.g., 100
        int blockSize = (int)scaleFactor; // High-res pixels per low-res unit (e.g., 100)

        // Texture at high-res scale
        int highResTextureSize = textureSize;
        uint[] pixelData = new uint[highResTextureSize * highResTextureSize];
        byte alpha = 255;

        // Center on fastest particle in low-res baseMap coordinates
        int centerX = (int)Math.Round(fastestParticle.Position.X);
        int centerY = (int)Math.Round(fastestParticle.Position.Y);

        // Define low-res region to iterate over (portion of baseMap)
        int lowResHalfSize = (int)(textureSize / 2 / scaleFactor); // Half-size in low-res units
        int startX = Math.Max(0, centerX - lowResHalfSize);
        int startY = Math.Max(0, centerY - lowResHalfSize);
        int endX = Math.Min(baseMap.Width, centerX + lowResHalfSize);
        int endY = Math.Min(baseMap.Height, centerY + lowResHalfSize);

        // Iterate over low-res baseMap positions
        for (int y = startY; y < endY; y++)
        {
            for (int x = startX; x < endX; x++)
            {
                int baseIndex = baseMap.GetIndex(x, y);
                var particle = baseMap.PMap[baseIndex];

                // Map low-res position to high-res texture coordinates
                int highXBase = (int)((x - startX) * scaleFactor);
                int highYBase = (int)((y - startY) * scaleFactor);

                if (particle != null && particle.DMap != null && particle.DMap.ParticlesPerMeter == highResPPM)
                {
                    // High-res: Iterate over DMap pixels
                    DamageMap dmap = particle.DMap;
                    var uniqueInstances = dmap.compIDMap.Distinct().Where(id => id != 0).ToList();

                    // DMap offset to align with particle position (center of DMap)
                    int dmapCenterX = dmap.Width / 2;
                    int dmapCenterY = dmap.Height / 2;

                    for (int dy = 0; dy < dmap.Height && highYBase + dy < highResTextureSize; dy++)
                    {
                        for (int dx = 0; dx < dmap.Width && highXBase + dx < highResTextureSize; dx++)
                        {
                            int dmapX = dx - dmapCenterX + (int)((x - fastestParticle.Position.X) * scaleFactor);
                            int dmapY = dy - dmapCenterY + (int)((y - fastestParticle.Position.Y) * scaleFactor);

                            if (dmapX >= 0 && dmapX < dmap.Width && dmapY >= 0 && dmapY < dmap.Height)
                            {
                                int dmapIndex = dmap.GetIndex(dmapX, dmapY);
                                int id = dmap.compIDMap[dmapIndex];
                                byte red = id != 0 ? (byte)(255 * uniqueInstances.IndexOf(id) / uniqueInstances.Count) : (byte)0;
                                int pixelIndex = (highYBase + dy) * highResTextureSize + (highXBase + dx);
                                if (pixelIndex >= 0 && pixelIndex < pixelData.Length)
                                    pixelData[pixelIndex] = GetCompisiteDamageColor(particle);
                            }
                        }
                    }
                }
                else
                {
                    // Low-res: Fill a block
                    int id = particle != null ? baseMap.compIDMap[baseIndex] : 0;
                    var uniqueInstances = baseMap.compIDMap.Distinct().Where(id => id != 0).ToList();
                    byte red = id != 0 ? (byte)(255 * uniqueInstances.IndexOf(id) / uniqueInstances.Count) : (byte)0;

                    for (int dy = 0; dy < blockSize && highYBase + dy < highResTextureSize; dy++)
                    {
                        for (int dx = 0; dx < blockSize && highXBase + dx < highResTextureSize; dx++)
                        {
                            int pixelIndex = (highYBase + dy) * highResTextureSize + (highXBase + dx);
                            if (pixelIndex >= 0 && pixelIndex < pixelData.Length)
                                pixelData[pixelIndex] = GetCompisiteDamageColor(particle);
                        }
                    }
                }
            }
        }

        // Create texture
        GCHandle handle = GCHandle.Alloc(pixelData, GCHandleType.Pinned);
        try
        {
            IntPtr pixels = handle.AddrOfPinnedObject();
            texture = IntPtr.Zero;
            Textures.UpdateOrCreate(renderer, ref texture, highResTextureSize, highResTextureSize, pixels);
        }
        finally
        {
            handle.Free();
        }
    }
    private static uint GetCompisiteDamageColor(PhysicalParticle particle)
    {
        uint color = 0;
        if (particle != null)
        {
            float healthPercent = particle.IsComponentPartDestroyed ? 0f : 1f; // Needs real health
            float tempRatio = particle.Temperature / particle.MatType.MeltingZeroPoint;
            byte r = (byte)(255 * (1 - healthPercent)); // Gray to red
            byte g = healthPercent > 0 ? (byte)200 : (byte)0;
            byte b = g;
            byte a = 255;

            if (tempRatio > 0.9f) // Heat tint
            {
                g = (byte)(g + (255 - g) * (tempRatio - 0.9f));
                if (tempRatio > 1f) b = (byte)(b + (255 - b) * (tempRatio - 1));
            }

            if (particle.StateOfPhase == PhaseState.Liquid) a = 200; // Drip effect
            else if (particle.StateOfPhase == PhaseState.Gas) a = 150; // Haze
            color = Utils.GetColor(r, g, b, a);
        }
        return color;
    }
    internal static void CreateTextureForCompisiteMap(IntPtr renderer, DamageMap damageMap, ref IntPtr texture, int width, int height)
    {
        byte alpha = 255;
        // Create a buffer for the pixel data
        uint[] pixelData = new uint[width * height];

        // Fill the pixel data
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                pixelData[y * width + x] = GetCompisiteDamageColor(damageMap.PMap[index]);
            }
        }

        GCHandle handle = GCHandle.Alloc(pixelData, GCHandleType.Pinned);
        try
        {
            IntPtr pixels = handle.AddrOfPinnedObject();

            // Update the texture
            Textures.UpdateOrCreate(renderer, ref texture, width, height, pixels);
        }
        finally
        {
            handle.Free();
        }
    }
    internal static void CreateTextureForPresMap(IntPtr renderer, DamageMap damageMap, ref IntPtr texture, int width, int height)
    {
        byte alpha = 255;
        float maxPressure = damageMap.PresMap.Max();

        // Create a buffer for the pixel data
        uint[] pixelData = new uint[width * height];

        // Fill the pixel data
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;

                // Calculate blue value
                byte blueValue = (byte)(damageMap.PresMap[index] * 255.0f / maxPressure);

                // Pack ARGB values into a single uint
                pixelData[y * width + x] = Utils.GetColor(0, 0, blueValue, alpha);
            }
        }

        GCHandle handle = GCHandle.Alloc(pixelData, GCHandleType.Pinned);
        try
        {
            IntPtr pixels = handle.AddrOfPinnedObject();

            // Update the texture
            Textures.UpdateOrCreate(renderer, ref texture, width, height, pixels);
        }
        finally
        {
            handle.Free();
        }
    }


    internal static void CreateTextureForVMap(IntPtr renderer, DamageMap damageMap, ref IntPtr texture, int width, int height)
    {
        byte alpha = 255;
        double maxVelocity = 0;
        foreach (var part in damageMap.PMap)
        {
            if(part != null && part.Velocity.Length() > maxVelocity)
                maxVelocity = part.Velocity.Length();
        }


        // Create a buffer for the pixel data
        uint[] pixelData = new uint[width * height];

        // Fill the pixel data
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = damageMap.GetIndex(x, y);
                var part = damageMap.PMap[index];
                byte greenValue = 0;
                if(part != null)
                    greenValue = (byte)((damageMap.PMap[index].Velocity.Length() * 255.0) / maxVelocity);

                // Pack ARGB values into a single uint
                pixelData[y * width + x] = Utils.GetColor(0, greenValue, 0, alpha);
            }
        }

        GCHandle handle = GCHandle.Alloc(pixelData, GCHandleType.Pinned);
        try
        {
            IntPtr pixels = handle.AddrOfPinnedObject();

            // Update the texture
            Textures.UpdateOrCreate(renderer, ref texture, width, height, pixels);
        }
        finally
        {
            handle.Free();
        }
    }


    internal static void CreateTextureForPMap(IntPtr renderer, DamageMap damageMap, ref IntPtr texture, int width, int height)
    {
        byte alpha = 255;
        int phaseStateCount = Enum.GetValues(typeof(PhaseState)).Length;

        // Create a buffer for the pixel data
        uint[] pixelData = new uint[width * height];

        // Fill the pixel data
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                PhysicalParticle physicalParticle = damageMap.PMap[index];
                uint color = 0;
                if (physicalParticle != null)
                {
                    // Red for Life (Health) 0 to 255
                    byte lifeRed = (byte)(physicalParticle.IsComponentPartDestroyed ? 50 : 255); //

                    // Blue for StateOfPhase, using full range 0 to 255

                    byte phaseBlue = (byte)((int)physicalParticle.StateOfPhase * 255 / (phaseStateCount - 1)); // Spread over 0-255

                    // Green for Temperature, assuming max temp is known or we normalize to 100
                    byte tempGreen = (byte)(Math.Min(physicalParticle.Temperature, 100) * 2.55f); // Normalize to 0-100 then to 0-255

                    // Combine all channels
                    color = Utils.GetColor(lifeRed, tempGreen, phaseBlue, alpha);
                }

                // Pack ARGB values into a single uint
                pixelData[index] = color;
            }
        }

        GCHandle handle = GCHandle.Alloc(pixelData, GCHandleType.Pinned);
        try
        {
            IntPtr pixels = handle.AddrOfPinnedObject();

            // Update the texture
            Textures.UpdateOrCreate(renderer, ref texture, width, height, pixels);
        }
        finally
        {
            handle.Free();
        }
    }


    internal static void CreateTextureForPhaseState(IntPtr renderer, DamageMap damageMap, ref IntPtr texture, int width, int height)
    {
        int phaseStateCount = Enum.GetValues(typeof(PhaseState)).Length;
        uint color = 0;

        // Create a buffer for the pixel data
        uint[] pixelData = new uint[width * height];

        // Fill the pixel data
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = damageMap.GetIndex(x,y);
                PhysicalParticle physicalParticle = damageMap.PMap[index];
                if (physicalParticle != null)
                {
                    var phaseState = physicalParticle.StateOfPhase;
                    byte byteState = (byte)phaseState;
                    color = Utils.ColourFromValue(byteState, phaseStateCount, 0);
                }
                else color = 0;

                // Pack ARGB values into a single uint
                pixelData[index] = color;
            }
        }

        GCHandle handle = GCHandle.Alloc(pixelData, GCHandleType.Pinned);
        try
        {
            IntPtr pixels = handle.AddrOfPinnedObject();
            // Update the texture
            Textures.UpdateOrCreate(renderer, ref texture, width, height, pixels);
        }
        finally
        {
            handle.Free();
        }
    }


    internal static void CreateTextureForTemp(IntPtr renderer, DamageMap damageMap, ref IntPtr texture, int width, int height)
    {
        uint color = 0;
        float temperatureInKelvin = 0;
        float thermalCapacity = 0;
        float thermalConductivity = 0;
        // Define our color spectrum based on Kelvin scale
        float minTemp = 6000;   // Absolute zero
        float maxTemp = 6000; // Arbitrary max, adjust based on your data range or visual needs
        foreach (var particle in damageMap.PMap)
        {
            if (particle != null)
            {
                if(particle.Temperature < minTemp)
                    minTemp = particle.Temperature;
                if(particle.Temperature > maxTemp)
                    maxTemp = particle.Temperature;
            }
        }

        // Create a buffer for the pixel data
        uint[] pixelData = new uint[width * height];

        // Fill the pixel data
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                PhysicalParticle physicalParticle = damageMap.PMap[index];

                if (physicalParticle != null)
                {
                    temperatureInKelvin = physicalParticle.Temperature;
                    thermalCapacity = physicalParticle.MatType.ThermalCapacity;
                    thermalConductivity = physicalParticle.MatType.ThermalConductivity;

                    // Normalize temperature
                    float tempNormalized = (temperatureInKelvin - minTemp) / (maxTemp - minTemp);

                    // Mapping temperature to RGB
                    byte r, g, b;
                    if (tempNormalized < 0.2f) // Very Cold - Dark Blue to Blue
                    {
                        r = 0;
                        g = (byte)((tempNormalized * 5f) * 255);
                        b = 1;
                    }
                    else if (tempNormalized < 0.4f) // Cold - Blue to Cyan
                    {
                        r = 0;
                        g = 1;
                        b = (byte)(1 - (tempNormalized - 0.2f) * 5f * 255) ;
                    }
                    else if (tempNormalized < 0.6f) // Cool - Cyan to Green
                    {
                        r = (byte)((tempNormalized - 0.4f) * 5f * 255);
                        g = 1;
                        b = 0;
                    }
                    else if (tempNormalized < 0.8f) // Warm - Green to Yellow
                    {
                        r = 1;
                        g = (byte)(1 - (tempNormalized - 0.6f) * 5f * 255);
                        b = 0;
                    }
                    else // Hot - Yellow to White
                    {
                        byte t = (byte)((tempNormalized - 0.8f) * 5f * 255);
                        r = 1;
                        g = t;
                        b = t;
                    }

                    /*
                    // Adjust color based on thermal properties
                    float saturation = 1.0f - (thermalConductivity / 100f);
                    (r, g, b) = AdjustSaturation(r, g, b, saturation);

                    // Use thermal capacity to adjust lightness
                    float lightness = 0.5f + (thermalCapacity / 200f);
                    (r, g, b) = AdjustLightness(r, g, b, lightness);
                    */
                    // Convert to uint for SDL2 texture (ARGB format)
                    byte a = 255; // Full opacity
                    color = Utils.GetColor(r, g, b, a);
                }
                else
                {
                    color = 0;
                }

                // Pack ARGB values into a single uint
                pixelData[index] = color;
            }
        }

        GCHandle handle = GCHandle.Alloc(pixelData, GCHandleType.Pinned);
        try
        {
            IntPtr pixels = handle.AddrOfPinnedObject();
            // Update the texture
            Textures.UpdateOrCreate(renderer, ref texture, width, height, pixels);
        }
        finally
        {
            handle.Free();
        }
    }
    internal static void CreateTextureForBeamPoints(IntPtr renderer, DamageMap damageMap, ref IntPtr texture, int width, int height)
    {
        List<BeamPoint> beamPoints = damageMap.BeamPoints;
        if (beamPoints == null || beamPoints.Count == 0)
        {
            if (texture != IntPtr.Zero)
            {
                Textures.DeleteTexture(texture);
                texture = IntPtr.Zero;
            }
            return;
        }

        // Find min and max for wavelength and power
        var minFreq = (int)beamPoints.Min(p => p.Wavelength);
        var maxFreq = (int)beamPoints.Max(p => p.Wavelength);
        var maxPow = (int)beamPoints.Max(p => p.Power);

        // Adjust the range for visualization
        minFreq = (int)(minFreq * 0.5);
        maxFreq = (int)(maxFreq * 1.5);

        uint color = 0;

        // Create a buffer for the pixel data
        uint[] pixelData = new uint[width * height];

        // Initialize pixel data to black (or transparent if your format supports alpha)
        Array.Clear(pixelData, 0, pixelData.Length);

        // Fill the pixel data based on BeamPoints
        foreach (var point in beamPoints)
        {
            int x = (int)point.Position.X;
            int y = (int)point.Position.Y;

            // Ensure the point is within the texture bounds
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                int index = y * width + x;
                color = Utils.ColourFromValue((int)point.Wavelength, maxFreq, minFreq, point.Power, 25, maxPow);
                pixelData[index] = color;
            }
        }

        GCHandle handle = GCHandle.Alloc(pixelData, GCHandleType.Pinned);
        try
        {
            IntPtr pixels = handle.AddrOfPinnedObject();
            // Update the texture
            Textures.UpdateOrCreate(renderer, ref texture, width, height, pixels);
        }
        finally
        {
            handle.Free();
        }
    }

}