using SDL2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GameEngine.Damage;
using ImGuiNET;
using Pulsar4X.DataStructures;
using Pulsar4X.Orbital;
using Pulsar4X.Client.Rendering;
using Microsoft.VisualBasic;

namespace ImGuiSDL2CS;

public static class SDL2Helper
{

    internal static void UpdateOrCreate(IRenderer renderer, ref IntPtr texture, int width, int height, IntPtr pixels)
    {
        // If the texture doesn't exist, create it
        if(texture == IntPtr.Zero)
        {
            renderer.CreateTexture(ref texture, width, height, pixels, PixelFormat.RGBA8888, TextureFilter.Nearest);
            return;
        }
        // If the dimensions don't match, recreate the texture
        (int txWidth, int txHeight) = renderer.GetTextureDimensions(texture);
        if(width != txWidth || height != txHeight)
        {
            renderer.CreateTexture(ref texture, width, height, pixels, PixelFormat.RGBA8888, TextureFilter.Nearest);
            return;
        }
        else
            renderer.UpdateTexture(ref texture, width, height, pixels);
    }
    internal static void CreateTestTexture(IRenderer renderer, ref IntPtr texture)
    {
        const int squareSize = 100;
        const int width = squareSize * 2;  // 200 pixels wide
        const int height = squareSize * 2; // 200 pixels high

        // Create a buffer for the pixel data
        uint[] pixelData = new uint[width * height];

        // Fill the pixel data
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                uint color = 0; // Default to black or transparent

                // Determine which square we're in
                int squareX = x / squareSize;
                int squareY = y / squareSize;
                byte valueMax = 255;
                byte valueMin = 0;


                if (squareX == 0 && squareY == 0) // Top left - Red
                    color = (uint)((valueMax << 0) | (valueMin << 8) | (valueMin << 16) | (valueMax << 24));
                else if (squareX == 1 && squareY == 0) // Top right - Green
                    color = (uint)((valueMin << 0) | (valueMax << 8) | (valueMin << 16) | (valueMax << 24));
                else if (squareX == 0 && squareY == 1) // Bottom left - Blue
                    color = (uint)((valueMin << 0) | (valueMin << 8) | (valueMax << 16) | (valueMax << 24));
                else if (squareX == 1 && squareY == 1) // Bottom right - Alpha only (black but full alpha)
                    color = (uint)((valueMin << 0) | (valueMin << 8) | (valueMin << 16) | (valueMax << 24));

                pixelData[y * width + x] = color;
            }
        }

        // Pin the pixel data in memory
        GCHandle handle = GCHandle.Alloc(pixelData, GCHandleType.Pinned);
        try
        {
            IntPtr pixels = handle.AddrOfPinnedObject();
            // Update or create the texture with this pixel data
            UpdateOrCreate(renderer, ref texture, width, height, pixels);
        }
        finally
        {
            handle.Free();
        }
    }

    static uint GetColor(byte r, byte g, byte b, byte a)
    {
        return (uint)((r << 0) | (g << 8) | (b << 16) | (a << 24));
    }

    public static void CreateSDLTextures(IRenderer renderer, IntPtr renderPtr, DamageMap damageMap, ref IntPtr[] textures)
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

    internal static void CheckTexture(IntPtr renderPtr, ref IntPtr texture, int width, int height)
    {
        if(texture == IntPtr.Zero)
            texture = SDL.SDL_CreateTexture(renderPtr, SDL.SDL_PIXELFORMAT_ARGB8888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, width, height);
        SDL.SDL_QueryTexture(texture, out _, out _, out var txWidth, out var txHeight);
        if (width != txWidth || height != txHeight)
        {
            SDL.SDL_DestroyTexture(texture);
            texture = SDL.SDL_CreateTexture(renderPtr, SDL.SDL_PIXELFORMAT_ARGB8888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, width, height);
        }
    }



    internal static void CreateTextureForIDMap(IRenderer renderer, DamageMap damageMap, ref IntPtr texture, int width, int height)
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
                pixelData[y * width + x] = GetColor(redValue, 0, 0, alpha);
            }
        }
        
        GCHandle handle = GCHandle.Alloc(pixelData, GCHandleType.Pinned);
        try
        {
            IntPtr pixels = handle.AddrOfPinnedObject();
            // Update the texture
            UpdateOrCreate(renderer, ref texture, width, height, pixels);
        }
        finally
        {
            handle.Free();
        }
    }


    internal static void CreateTextureForPresMap(IRenderer renderer, DamageMap damageMap, ref IntPtr texture, int width, int height)
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
                pixelData[y * width + x] = GetColor(0, 0, blueValue, alpha);
            }
        }

        GCHandle handle = GCHandle.Alloc(pixelData, GCHandleType.Pinned);
        try
        {
            IntPtr pixels = handle.AddrOfPinnedObject();

            // Update the texture
            UpdateOrCreate(renderer, ref texture, width, height, pixels);
        }
        finally
        {
            handle.Free();
        }
    }


    internal static void CreateTextureForVMap(IRenderer renderer, DamageMap damageMap, ref IntPtr texture, int width, int height)
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
                pixelData[y * width + x] = GetColor(0, greenValue, 0, alpha);
            }
        }

        GCHandle handle = GCHandle.Alloc(pixelData, GCHandleType.Pinned);
        try
        {
            IntPtr pixels = handle.AddrOfPinnedObject();

            // Update the texture
            UpdateOrCreate(renderer, ref texture, width, height, pixels);
        }
        finally
        {
            handle.Free();
        }
    }


    internal static void CreateTextureForPMap(IRenderer renderer, DamageMap damageMap, ref IntPtr texture, int width, int height)
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
                    color = GetColor(lifeRed, tempGreen, phaseBlue, alpha);
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
            UpdateOrCreate(renderer, ref texture, width, height, pixels);
        }
        finally
        {
            handle.Free();
        }
    }


    internal static void CreateTextureForPhaseState(IRenderer renderer, DamageMap damageMap, ref IntPtr texture, int width, int height)
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
                    color = ColourFromValue(byteState, phaseStateCount, 0);
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
            UpdateOrCreate(renderer, ref texture, width, height, pixels);
        }
        finally
        {
            handle.Free();
        }
    }


    internal static void CreateTextureForTemp(IRenderer renderer, DamageMap damageMap, ref IntPtr texture, int width, int height)
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
                    color = GetColor(r, g, b, a);
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
            UpdateOrCreate(renderer, ref texture, width, height, pixels);
        }
        finally
        {
            handle.Free();
        }
    }
    internal static void CreateTextureForBeamPoints(IRenderer renderer, DamageMap damageMap, ref IntPtr texture, int width, int height)
    {
        List<BeamPoint> beamPoints = damageMap.BeamPoints;
        if (beamPoints == null || beamPoints.Count == 0)
        {
            if (texture != IntPtr.Zero)
            {
                renderer.DeleteTexture((uint)texture);
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
                color = ColourFromValue((int)point.Wavelength, maxFreq, minFreq, point.Power, 25, maxPow);
                pixelData[index] = color;
            }
        }

        GCHandle handle = GCHandle.Alloc(pixelData, GCHandleType.Pinned);
        try
        {
            IntPtr pixels = handle.AddrOfPinnedObject();
            // Update the texture
            UpdateOrCreate(renderer, ref texture, width, height, pixels);
        }
        finally
        {
            handle.Free();
        }
    }


    
    public static void CreateSDLTexture(IntPtr rendererPtr, RawBmp rawImg, ref IntPtr texturePtr)
    {


        //IntPtr texture;
        int h = rawImg.Height;
        int w = rawImg.Width;
        int d = rawImg.Depth * 8;
        int s = rawImg.Stride;
        IntPtr pxls;
        unsafe
        {
            fixed (byte* ptr = rawImg.ByteArray)
            {
                pxls = new IntPtr(ptr);
            }
        }

        uint rmask = 0xff000000;
        uint gmask = 0x00ff0000;
        uint bmask = 0x0000ff00;
        uint amask = 0x000000ff;


        SDL.SDL_DestroyTexture(texturePtr);
        IntPtr sdlSurface = SDL.SDL_CreateRGBSurfaceFrom(pxls, w, h, d, s, rmask, gmask, bmask, amask);
        texturePtr = SDL.SDL_CreateTextureFromSurface(rendererPtr, sdlSurface);
        SDL.SDL_FreeSurface(sdlSurface);

    }

    public static (float, float, float) AdjustSaturation(float r, float g, float b, float saturation)
    {
        float max = Math.Max(r, Math.Max(g, b));
        float min = Math.Min(r, Math.Min(g, b));
        float l = (max + min) / 2f;

        if (max == min)
            return (r, g, b); // No saturation for grayscale colors

        float s = l < 0.5f ? (max - min) / (max + min) : (max - min) / (2f - max - min);
        s *= saturation;

        float v1 = l + s * (l < 0.5f ? l : (1f - l)); // v1 = v + s * v
        float v2 = 2f * l - v1; // v2 = v - s * v

        r = HueToRGB(v2, v1, r + 1f / 3f);
        g = HueToRGB(v2, v1, g);
        b = HueToRGB(v2, v1, b - 1f / 3f);

        return (r, g, b);
    }


    public static (float, float, float) AdjustLightness(float r, float g, float b, float lightness)
    {
        return (r * lightness, g * lightness, b * lightness);
    }

    // Helper method for saturation adjustment
    public static float HueToRGB(float v1, float v2, float vH)
    {
        if (vH < 0) vH += 1;
        if (vH > 1) vH -= 1;
        if (6 * vH < 1) return v1 + (v2 - v1) * 6 * vH;
        if (2 * vH < 1) return v2;
        if (3 * vH < 2) return v1 + (v2 - v1) * ((2f / 3f) - vH) * 6;
        return v1;
    }

    public static uint BlackWhiteFromValue(
        float value, int max, int min,
        float alphaValue = 255, int alphaMin = 0, int alphaMax = 255
    )
    {
        // Normalize RGB based on the value range [min, max]
        float normalizedValue = (float)(value - min) / (max - min);

        // Scale RGB
        byte r = (byte)(normalizedValue * 255);
        byte g = (byte)(normalizedValue * 255);
        byte b = (byte)(normalizedValue * 255);

        // Handle Alpha, either fixed or normalized based on separate alpha range
        float normalizedAlpha = (float)(alphaValue - alphaMin) / (alphaMax - alphaMin);
        byte a = (byte)(normalizedAlpha * 255);
        return GetColor(r, g, b, a);
    }
    public static uint ColourFromValue(
        float value, int max, int min,
        float alphaValue = 255, int alphaMin = 0, int alphaMax = 255
    )
    {
        // Normalize the value range [min, max]
        float normalizedValue = (float)(value - min) / (max - min);
        normalizedValue = Math.Clamp(normalizedValue, 0.0f, 1.0f); // Ensure it's within [0, 1] for safety

        // Map normalizedValue to a hue-based RGB color
        byte r = 0, g = 0, b = 0;
        if (normalizedValue < 0.5)
        {
            // Interpolate between red (255, 0, 0) and green (0, 255, 0)
            r = (byte)(255 * (1 - 2 * normalizedValue));
            g = (byte)(255 * (2 * normalizedValue));
            b = 0;
        }
        else
        {
            // Interpolate between green (0, 255, 0) and blue (0, 0, 255)
            r = 0;
            g = (byte)(255 * (2 * (1 - normalizedValue)));
            b = (byte)(255 * (2 * (normalizedValue - 0.5)));
        }

        // Handle Alpha, normalized based on alphaMin and alphaMax
        float normalizedAlpha = (float)(alphaValue - alphaMin) / (alphaMax - alphaMin);
        byte a = (byte)(Math.Clamp(normalizedAlpha, 0.0f, 1.0f) * 255);

        // Convert to RGBA uint
        return GetColor(r, g, b, a);
    }

    enum ColourOrder
    {
        ARGB,
        RGBA
    }
    static uint ColourFromRGBA(byte r, byte g, byte b, byte a, ColourOrder order = ColourOrder.RGBA)
    {
        if(order == ColourOrder.RGBA)
            return (uint)((r << 24) | (g << 16) | (b << 8) | a);
        else if(order == ColourOrder.ARGB)
            return (uint)((a << 24) | (r << 16) | (g << 8) | b);
        else throw new Exception("Invalid ColourOrder");
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct Int4
{
    public readonly int X, Y, Z, W;
}

