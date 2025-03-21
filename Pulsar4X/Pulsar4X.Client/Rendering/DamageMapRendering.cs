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
        if (fastestParticle == null)
        {
            texture = IntPtr.Zero;
            return;
        }

        // Verify fastestParticle is in baseMap
        bool foundInBase = Array.Exists(baseMap.PMap, p => p == fastestParticle);
        if (!foundInBase)
            throw new Exception($"Fastest particle (ID {fastestParticle.ID}) not found in baseMap.PMap");

        // Resolution setup
        int highResPPM = baseMap.PhysicsScale; // e.g., 1000
        int lowResPPM = baseMap.ParticlesPerMeter; // e.g., 10
        float scaleFactor = (float)highResPPM / lowResPPM; // e.g., 100
        int blockSize = (int)scaleFactor; // Block size in high-res pixels (e.g., 100)

        // Texture is at high-res scale
        int highResTextureSize = textureSize; // e.g., 64 pixels
        uint[] pixelData = new uint[highResTextureSize * highResTextureSize];
        byte alpha = 255;

        // Center on fastest particle in low-res coordinates
        float centerX = fastestParticle.Position.X; // Keep as float for precision
        float centerY = fastestParticle.Position.Y;

        // Iterate over high-res texture pixels
        for (int y = 0; y < highResTextureSize; y++)
        {
            for (int x = 0; x < highResTextureSize; x++)
            {
                // Map high-res texture pixel to low-res baseMap coordinates
                float offsetX = (x - highResTextureSize / 2f) / scaleFactor; // Center texture on particle
                float offsetY = (y - highResTextureSize / 2f) / scaleFactor;
                float baseX = centerX + offsetX;
                float baseY = centerY + offsetY;

                int baseXInt = (int)Math.Floor(baseX);
                int baseYInt = (int)Math.Floor(baseY);

                // Check bounds
                if (baseXInt >= 0 && baseXInt < baseMap.Width && baseYInt >= 0 && baseYInt < baseMap.Height)
                {
                    int baseIndex = baseMap.GetIndex(baseXInt, baseYInt);
                    var particle = baseMap.PMap[baseIndex];

                    if (particle != null && particle.DMap != null && particle.DMap.ParticlesPerMeter == highResPPM)
                    {
                        // High-res: Map to DMap pixel
                        DamageMap dmap = particle.DMap;
                        // DMap coords relative to particle’s baseMap position
                        int dmapX = (int)((baseX - particle.Position.X) * scaleFactor + dmap.Width / 2f);
                        int dmapY = (int)((baseY - particle.Position.Y) * scaleFactor + dmap.Height / 2f);

                        if (dmapX >= 0 && dmapX < dmap.Width && dmapY >= 0 && dmapY < dmap.Height)
                        {
                            int dmapIndex = dmap.GetIndex(dmapX, dmapY);
                            var uniqueInstances = dmap.compIDMap.Distinct().Where(id => id != 0).ToList();
                            int id = dmap.compIDMap[dmapIndex];
                            byte red = id != 0 ? (byte)(255 * uniqueInstances.IndexOf(id) / uniqueInstances.Count) : (byte)0;
                            pixelData[y * highResTextureSize + x] = Utils.GetColor(red, 0, 0, alpha);
                        }
                    }
                    else
                    {
                        // Low-res: Use baseMap ID for the whole block area
                        int id = particle != null ? baseMap.compIDMap[baseIndex] : 0;
                        var uniqueInstances = baseMap.compIDMap.Distinct().Where(id => id != 0).ToList();
                        byte red = id != 0 ? (byte)(255 * uniqueInstances.IndexOf(id) / uniqueInstances.Count) : (byte)0;
                        pixelData[y * highResTextureSize + x] = Utils.GetColor(red, 0, 0, alpha);
                    }
                }
                // Leave out-of-bounds pixels black (default 0 from array init)
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