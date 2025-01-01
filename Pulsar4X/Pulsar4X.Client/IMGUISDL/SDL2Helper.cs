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

namespace ImGuiSDL2CS;

public static class SDL2Helper
{
    private static bool _Initialized = false;
    public static bool Initialized => _Initialized;

    public static void Init()
    {
        if (_Initialized)
            return;
        _Initialized = true;

        SDL.SDL_Init(SDL.SDL_INIT_VIDEO);

        SetGLAttributes();
    }

    public static void SetGLAttributes(int doubleBuffer = 1,
                                       int depthSize = 24,
                                       int stencilSize = 8
        //int majorVersion = 2,
        //int minorVersion = 2
    )
    {
        SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DOUBLEBUFFER, doubleBuffer);
        SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DEPTH_SIZE, depthSize);
        SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_STENCIL_SIZE, stencilSize);
        //SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, majorVersion);
        //SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, minorVersion);
    }


    public static IntPtr CreateSDLTexture(IntPtr rendererPtr, RawBmp rawImg, bool clean = false)
    {


        IntPtr texture;
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


        SDL.SDL_DestroyTexture(rendererPtr);
        IntPtr sdlSurface = SDL.SDL_CreateRGBSurfaceFrom(pxls, w, h, d, s, rmask, gmask, bmask, amask);
        texture = SDL.SDL_CreateTextureFromSurface(rendererPtr, sdlSurface);
        SDL.SDL_FreeSurface(sdlSurface);


        // int a;
        // uint f;
        // int qw;
        // int qh;
        // int q = SDL.SDL_QueryTexture(texture, out f, out a, out qw, out qh);
        // if (q != 0)
        // {
        //     ImGui.Text("QueryResult: " + q);
        //     ImGui.Text(SDL.SDL_GetError());
        // }
        // ImGui.Text("a: " + a +" f: " + f +" w: "+ qw +" h: "+ qh);

        return texture;
    }

    public static IntPtr[] CreateSDLTextures(IntPtr renderPtr, DamageMap damageMap, byte alpha)
    {
        IntPtr[] textures = new IntPtr[5]; // One for each map (IDMap, PresMap, VMap, PMap)

        int width = damageMap.Width;
        int height = damageMap.Height;

        textures[0] = CreateTextureForIDMap(renderPtr, damageMap, width, height, alpha);
        textures[1] = CreateTextureForPresMap(renderPtr, damageMap, width, height, alpha);
        textures[2] = CreateTextureForVMap(renderPtr, damageMap, width, height, alpha);
        textures[3] = CreateTextureForPMap(renderPtr, damageMap, width, height, alpha);
        textures[4] = CreateTextureForTemp(renderPtr, damageMap, width, height, alpha);
        return textures;
    }

    internal static IntPtr CreateTextureForIDMap(IntPtr renderPtr, DamageMap damageMap, int width, int height, byte alpha)
    {
        var texture = SDL.SDL_CreateTexture(renderPtr, SDL.SDL_PIXELFORMAT_ARGB8888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, width, height);

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
                    string id = damageMap.compIDMap[index];
                    byte redValue = id != null ? (byte)(255 * uniqueInstances.IndexOf(id) / (float)uniqueInstances.Count) : (byte)0;
                    *pixelPtr = (uint)((alpha << 24) | (redValue << 16) | 0);
                    pixelPtr++;
                }

                pixelPtr += (pitch / 4) - width;
            }
        }

        SDL.SDL_UnlockTexture(texture);
        return texture;
    }

    internal static IntPtr CreateTextureForPresMap(IntPtr renderPtr, DamageMap damageMap, int width, int height, byte alpha)
    {
        var texture = SDL.SDL_CreateTexture(renderPtr, SDL.SDL_PIXELFORMAT_ARGB8888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, width, height);

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

    internal static IntPtr CreateTextureForVMap(IntPtr renderPtr, DamageMap damageMap, int width, int height, byte alpha)
    {
        var texture = SDL.SDL_CreateTexture(renderPtr, SDL.SDL_PIXELFORMAT_ARGB8888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, width, height);

        IntPtr pixels;
        int pitch;
        SDL.SDL_LockTexture(texture, IntPtr.Zero, out pixels, out pitch);

        unsafe
        {
            uint* pixelPtr = (uint*)pixels.ToPointer();
            double maxVelocity = damageMap.VMap.Max(v => v.Length());
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    byte greenValue = (byte)((damageMap.VMap[index].Length() * 255.0) / maxVelocity);
                    *pixelPtr = (uint)((alpha << 24) | (greenValue << 8));
                    pixelPtr++;
                }

                pixelPtr += (pitch / 4) - width;
            }
        }

        SDL.SDL_UnlockTexture(texture);
        return texture;
    }

    internal static IntPtr CreateTextureForPMap(IntPtr renderPtr, DamageMap damageMap, int width, int height, byte alpha)
    {
        var texture = SDL.SDL_CreateTexture(renderPtr, SDL.SDL_PIXELFORMAT_ARGB8888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, width, height);

        IntPtr pixels;
        int pitch;
        SDL.SDL_LockTexture(texture, IntPtr.Zero, out pixels, out pitch);

        unsafe
        {
            uint* pixelPtr = (uint*)pixels.ToPointer();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    Particle particle = damageMap.PMap[index];
                    uint color = 0;
                    if (particle != null)
                    {
                        // Red for Life (Health) 0 to 255
                        byte lifeRed = (byte)(particle.Life * 2.55f); // Life is 0-100, so *2.55 for 0-255

                        // Blue for StateOfPhase, using full range 0 to 255
                        int phaseStateCount = Enum.GetValues(typeof(PhaseState)).Length;
                        byte phaseBlue = (byte)((int)particle.StateOfPhase * 255 / (phaseStateCount - 1)); // Spread over 0-255

                        // Green for Temperature, assuming max temp is known or we normalize to 100
                        byte tempGreen = (byte)(Math.Min(particle.Temperature, 100) * 2.55f); // Normalize to 0-100 then to 0-255

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
        return texture;
    }
    
    internal static IntPtr CreateTextureForTemp(IntPtr renderPtr, DamageMap damageMap, int width, int height, byte alpha)
    {
        var texture = SDL.SDL_CreateTexture(renderPtr, SDL.SDL_PIXELFORMAT_ARGB8888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, width, height);

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
                    Particle particle = damageMap.PMap[index];

                    uint color = 0;
                    if (particle != null)
                    {
                         temperatureInKelvin = particle.Temperature;
                         thermalCapacity = particle.MatType.ThermalCapacity;
                         thermalConductivity = particle.MatType.ThermalConductivity;


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
        return texture;
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
}

[StructLayout(LayoutKind.Sequential)]
public struct Int4
{
    public readonly int X, Y, Z, W;
}

