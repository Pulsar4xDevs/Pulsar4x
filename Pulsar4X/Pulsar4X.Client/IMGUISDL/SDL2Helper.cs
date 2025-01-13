using SDL2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
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

    public static void CreateSDLTextures(IRenderer renderer, IntPtr renderPtr, DamageMap damageMap, ref IntPtr[] textures)
    {
        int width = damageMap.Width;
        int height = damageMap.Height;
        CreateTextureForIDMap(renderer, damageMap, ref textures[0], width, height);
        CreateTextureForPresMap(renderPtr, damageMap, ref textures[1], width, height);
        CreateTextureForVMap(renderPtr, damageMap, ref textures[2], width, height);
        CreateTextureForPMap(renderPtr, damageMap, ref textures[3], width, height);
        CreateTextureForTemp(renderPtr, damageMap, ref textures[4], width, height);
        CreateTextureForPhaseState(renderPtr, damageMap, ref textures[5], width, height);
        CreateTextureForPhotonMap(renderPtr, damageMap, ref textures[6], width, height);

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

    internal static void CheckTexture(IRenderer renderer, ref IntPtr texture, int width, int height)
    {
        // If the texture doesn't exist, create it
        if(texture == IntPtr.Zero)
        {
            renderer.CreateTexture(ref texture, width, height, IntPtr.Zero, PixelFormat.ARGB8888);
        }

        // If the dimensions don't match, recreate the texture
        (int txWidth, int txHeight) = renderer.GetTextureDimensions(texture);
        if(width != txWidth || height != txHeight)
        {
            renderer.CreateTexture(ref texture, width, height, IntPtr.Zero, PixelFormat.ARGB8888);
        }
    }

    internal static void CreateTextureForIDMap(IRenderer renderer, DamageMap damageMap, ref IntPtr texture, int width, int height)
    {
        byte alpha = 255;

        // Check/create texture if needed
        CheckTexture(renderer, ref texture, width, height);

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
                pixelData[y * width + x] = (uint)((alpha << 24) | (redValue << 16) | 0);
            }
        }

        GCHandle handle = GCHandle.Alloc(pixelData, GCHandleType.Pinned);
        try
        {
            IntPtr pixels = handle.AddrOfPinnedObject();

            // Update the texture
            renderer.UpdateTexture(ref texture, width, height, pixels);
        }
        finally
        {
            handle.Free();
        }
    }

    internal static void CreateTextureForIDMap(IntPtr renderPtr, DamageMap damageMap, ref IntPtr texture, int width, int height)
    {
        byte alpha = 255;
        CheckTexture(renderPtr, ref texture, width, height);
        IntPtr pixels;
        int pitch;
        SDL.SDL_LockTexture(texture, IntPtr.Zero, out pixels, out pitch);

        unsafe
        {
            uint* pixelPtr = (uint*)pixels.ToPointer();
            var uniqueInstances = damageMap.compIDMap.Distinct().Where(id => id != null).ToList();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = damageMap.GetIndex(x, y);
                    int id = damageMap.compIDMap[index];
                    byte redValue = id != null ? (byte)(255 * uniqueInstances.IndexOf(id) / (float)uniqueInstances.Count) : (byte)0;
                    *pixelPtr = (uint)((alpha << 24) | (redValue << 16) | 0);
                    pixelPtr++;
                }

                pixelPtr += (pitch / 4) - width;
            }
        }
        SDL.SDL_UnlockTexture(texture);
    }

    internal static IntPtr CreateTextureForPresMap(IntPtr renderPtr, DamageMap damageMap, ref IntPtr texture, int width, int height)
    {
        byte alpha = 255;
        CheckTexture(renderPtr, ref texture, width, height);
        IntPtr pixels;
        int pitch;
        SDL.SDL_LockTexture(texture, IntPtr.Zero, out pixels, out pitch);

        unsafe
        {
            uint* pixelPtr = (uint*)pixels.ToPointer();
            float maxPressure = damageMap.PresMap.Max();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    byte blueValue = (byte)(damageMap.PresMap[index] * 255.0f / maxPressure);
                    *pixelPtr = (uint)((alpha << 24) | blueValue);
                    pixelPtr++;
                }
                pixelPtr += (pitch / 4) - width;
            }
        }

        SDL.SDL_UnlockTexture(texture);
        return texture;
    }

    internal static void CreateTextureForVMap(IntPtr renderPtr, DamageMap damageMap, ref IntPtr texture, int width, int height)
    {
        byte alpha = 255;
        CheckTexture(renderPtr, ref texture, width, height);
        IntPtr pixels;
        int pitch;
        SDL.SDL_LockTexture(texture, IntPtr.Zero, out pixels, out pitch);

        double maxVelocity = 0;
        foreach (var part in damageMap.PMap)
        {
            if(part != null && part.Velocity.Length() > maxVelocity)
                maxVelocity = part.Velocity.Length();
        }

        unsafe
        {
            uint* pixelPtr = (uint*)pixels.ToPointer();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = damageMap.GetIndex(x, y);
                    var part = damageMap.PMap[index];
                    byte greenValue = 0;
                    if(part != null)
                        greenValue = (byte)((damageMap.PMap[index].Velocity.Length() * 255.0) / maxVelocity);
                    *pixelPtr = (uint)((alpha << 24) | (greenValue << 8));
                    pixelPtr++;
                }

                pixelPtr += (pitch / 4) - width;
            }
        }

        SDL.SDL_UnlockTexture(texture);
    }

    internal static void CreateTextureForPMap(IntPtr renderPtr, DamageMap damageMap, ref IntPtr texture, int width, int height)
    {
        byte alpha = 255;
        CheckTexture(renderPtr, ref texture, width, height);
        IntPtr pixels;
        int pitch;
        SDL.SDL_LockTexture(texture, IntPtr.Zero, out pixels, out pitch);

        int phaseStateCount = Enum.GetValues(typeof(PhaseState)).Length;
        unsafe
        {
            uint* pixelPtr = (uint*)pixels.ToPointer();
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
                        byte lifeRed = (byte)(physicalParticle.Life * 2.55f); // Life is 0-100, so *2.55 for 0-255

                        // Blue for StateOfPhase, using full range 0 to 255

                        byte phaseBlue = (byte)((int)physicalParticle.StateOfPhase * 255 / (phaseStateCount - 1)); // Spread over 0-255

                        // Green for Temperature, assuming max temp is known or we normalize to 100
                        byte tempGreen = (byte)(Math.Min(physicalParticle.Temperature, 100) * 2.55f); // Normalize to 0-100 then to 0-255

                        // Combine all channels
                        color = (uint)((alpha << 24) | (lifeRed << 16) | (tempGreen << 8) | phaseBlue);
                        //color = 0xFFFFFFFF;

                    }
                    *pixelPtr = color;
                    pixelPtr++;
                }
                pixelPtr += (pitch / 4) - width; // Adjust for pitch
            }
        }
        SDL.SDL_UnlockTexture(texture);
    }

    internal static void CreateTextureForPhaseState(IntPtr renderPtr, DamageMap damageMap, ref IntPtr texture, int width, int height)
    {
        byte alpha = 255;
        CheckTexture(renderPtr, ref texture, width, height);
        IntPtr pixels;
        int pitch;
        SDL.SDL_LockTexture(texture, IntPtr.Zero, out pixels, out pitch);
        uint color = 0;
        int phaseStateCount = Enum.GetValues(typeof(PhaseState)).Length;
        unsafe
        {
            uint* pixelPtr = (uint*)pixels.ToPointer();

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
                    *pixelPtr = color;
                    pixelPtr++;
                }
                pixelPtr += (pitch / 4) - width; // Adjust for pitch
            }
        }

        SDL.SDL_UnlockTexture(texture);

    }

    internal static void CreateTextureForTemp(IntPtr renderPtr, DamageMap damageMap, ref IntPtr texture, int width, int height)
    {
        byte alpha = 255;
        CheckTexture(renderPtr, ref texture, width, height);
        IntPtr pixels;
        int pitch;
        SDL.SDL_LockTexture(texture, IntPtr.Zero, out pixels, out pitch);

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
        unsafe
        {
            uint* pixelPtr = (uint*)pixels.ToPointer();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    PhysicalParticle physicalParticle = damageMap.PMap[index];

                    uint color = 0;
                    if (physicalParticle != null)
                    {
                         temperatureInKelvin = physicalParticle.Temperature;
                         thermalCapacity = physicalParticle.MatType.ThermalCapacity;
                         thermalConductivity = physicalParticle.MatType.ThermalConductivity;


                        // Normalize temperature
                        float tempNormalized = (temperatureInKelvin - minTemp) / (maxTemp - minTemp);

                        // Mapping temperature to RGB
                        float r, g, b;
                        if (tempNormalized < 0.2f) // Very Cold - Dark Blue to Blue
                        {
                            r = 0;
                            g = tempNormalized * 5f;
                            b = 1;
                        }
                        else if (tempNormalized < 0.4f) // Cold - Blue to Cyan
                        {
                            r = 0;
                            g = 1;
                            b = 1 - (tempNormalized - 0.2f) * 5f;
                        }
                        else if (tempNormalized < 0.6f) // Cool - Cyan to Green
                        {
                            r = (tempNormalized - 0.4f) * 5f;
                            g = 1;
                            b = 0;
                        }
                        else if (tempNormalized < 0.8f) // Warm - Green to Yellow
                        {
                            r = 1;
                            g = 1 - (tempNormalized - 0.6f) * 5f;
                            b = 0;
                        }
                        else // Hot - Yellow to White
                        {
                            float t = (tempNormalized - 0.8f) * 5f;
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
                        color = (uint)((a << 24) | ((byte)(r * 255) << 16) | ((byte)(g * 255) << 8) | (byte)(b * 255));

                    }
                    *pixelPtr = color;
                    pixelPtr++;
                }
                pixelPtr += (pitch / 4) - width; // Adjust for pitch
            }
        }

        SDL.SDL_UnlockTexture(texture);

    }
    internal static void CreateTextureForPhotonMap(IntPtr renderPtr, DamageMap damageMap,ref IntPtr texture, int width, int height)
    {
        if(damageMap.PhMap == null)
        {
            if (texture != IntPtr.Zero)
            {
                SDL.SDL_DestroyTexture(texture);
                texture = IntPtr.Zero;
            }
            return;
        }

        byte alpha = 255;
        CheckTexture(renderPtr, ref texture, width, height);
        IntPtr pixels;
        int pitch;
        SDL.SDL_LockTexture(texture, IntPtr.Zero, out pixels, out pitch);


        var minFreq = (int)damageMap.PhMap
                                    .Where(p => p != null)
                                    .Select(p => p.WaveLength)
                                    .DefaultIfEmpty(0) // Fallback value
                                    .Min();
        var maxFreq = (int)damageMap.PhMap
                                    .Where(p => p != null)
                                    .Select(p => p.WaveLength)
                                    .DefaultIfEmpty(10000) // Fallback value
                                    .Max();
        var maxPow = (int)damageMap.PhMap
                                   .Where(p => p != null)
                                   .Select(p => p.WaveLength)
                                   .DefaultIfEmpty(10000) // Fallback value
                                   .Max();
        minFreq = (int)(minFreq * 0.5);
        maxFreq = (int)(maxFreq * 1.5);
        uint  color = 0;

        unsafe
        {
            uint* pixelPtr = (uint*)pixels.ToPointer();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = damageMap.GetIndex(x, y);
                    var photon = damageMap.PhMap[index];
                    if(photon != null)
                    {
                        var power = photon.Power;
                        var wavelen = (int)photon.WaveLength;
                        color = ColourFromValue2(wavelen, maxFreq, minFreq, power, 25, maxPow);
                    }
                    else color = 0;
                    *pixelPtr = color;
                    pixelPtr++;
                }
                pixelPtr += (pitch / 4) - width;
            }
        }

        SDL.SDL_UnlockTexture(texture);
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

    public static uint ColourFromValue(
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
        return (uint)((a << 24) | (r << 16) | (g << 8) | b);
    }
    public static uint ColourFromValue2(
        float value, int max, int min,
        float alphaValue = 255, int alphaMin = 0, int alphaMax = 255
    )
    {
        // Normalize the value range [min, max]
        float normalizedValue = (float)(value - min) / (max - min);
        normalizedValue = Math.Clamp(normalizedValue, 0.0f, 1.0f); // Ensure it's within [0, 1] for safety

        // Map normalizedValue to a hue-based RGB color
        float r = 0, g = 0, b = 0;
        if (normalizedValue < 0.25f) // Blue → Cyan
        {
            r = 0;
            g = normalizedValue * 4;      // Scale up
            b = 1;
        }
        else if (normalizedValue < 0.5f) // Cyan → Green
        {
            r = 0;
            g = 1;
            b = 1 - (normalizedValue - 0.25f) * 4;
        }
        else if (normalizedValue < 0.75f) // Green → Yellow
        {
            r = (normalizedValue - 0.5f) * 4;
            g = 1;
            b = 0;
        }
        else // Yellow → Red
        {
            r = 1;
            g = 1 - (normalizedValue - 0.75f) * 4;
            b = 0;
        }

        // Handle Alpha, normalized based on alphaMin and alphaMax
        float normalizedAlpha = (float)(alphaValue - alphaMin) / (alphaMax - alphaMin);
        byte a = (byte)(Math.Clamp(normalizedAlpha, 0.0f, 1.0f) * 255);

        // Convert to ARGB uint for SDL2
        return (uint)((a << 24) | ((byte)(r * 255) << 16) | ((byte)(g * 255) << 8) | (byte)(b * 255));
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct Int4
{
    public readonly int X, Y, Z, W;
}

