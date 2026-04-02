using Pulsar4X.Colonies;
using Pulsar4X.Engine;
using Pulsar4X.Galaxy;
using Pulsar4X.Names;
using Pulsar4X.Ships;
using SDL3;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;
using System.Drawing;

namespace Pulsar4X.Client;

public static class Utils
{
    public static int EnumEntries<T>() where T : struct, System.Enum =>
        Enum.GetNames(typeof(T)).Length;

    public static byte[] BytesFromString(string str, int sizeMax = 128)
    {
        byte[] dstArray = new byte[sizeMax];
        byte[] srsArray = System.Text.Encoding.UTF8.GetBytes(str);
        int srsSize = Math.Min(srsArray.Length, sizeMax);
        Buffer.BlockCopy(srsArray, 0, dstArray, 0, srsSize);
        return dstArray;
    }

    public static string StringFromBytes(byte[] byteArray)
    {
        // Get the string and trim off any trailing null characters
        string result = System.Text.Encoding.UTF8.GetString(byteArray);
        int nullIndex = result.IndexOf('\0');
        if(nullIndex >= 0)
        {
            result = result.Substring(0, nullIndex);
        }
        return result;
    }

    public static (IntPtr, uint) GuidToIntPtr(Guid guid)
    {
        byte[] bytes = guid.ToByteArray();
        IntPtr ptr = Marshal.AllocHGlobal(bytes.Length);
        Marshal.Copy(bytes, 0, ptr, bytes.Length);
        return (ptr, (uint)bytes.Length);
    }

    public static uint GetColor(byte r, byte g, byte b, byte a)
    {
        return (uint)((r << 0) | (g << 8) | (b << 16) | (a << 24));
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

    public static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
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

    internal static UserOrbitSettings.OrbitBodyType EntityBodyType(Entity entity)
    {
        if (entity.HasDataBlob<SystemBodyInfoDB>())
        {
            switch (entity.GetDataBlob<SystemBodyInfoDB>().BodyType)
            {
                case DataStructures.BodyType.Asteroid:
                    return UserOrbitSettings.OrbitBodyType.Asteroid;
                case DataStructures.BodyType.Comet:
                    return UserOrbitSettings.OrbitBodyType.Comet;
                case DataStructures.BodyType.DwarfPlanet:
                    return UserOrbitSettings.OrbitBodyType.DwarfPlanet;
                case DataStructures.BodyType.GasDwarf:
                case DataStructures.BodyType.GasGiant:
                case DataStructures.BodyType.IceGiant:
                case DataStructures.BodyType.Terrestrial:
                    return UserOrbitSettings.OrbitBodyType.Planet;
                case DataStructures.BodyType.Moon:
                    return UserOrbitSettings.OrbitBodyType.Moon;
                default:
                    break;
            }

        }
        if (entity.HasDataBlob<StarInfoDB>())
            return UserOrbitSettings.OrbitBodyType.Star;
        if (entity.HasDataBlob<ColonyInfoDB>())
            return UserOrbitSettings.OrbitBodyType.Colony;
        if (entity.HasDataBlob<ShipInfoDB>())
            return UserOrbitSettings.OrbitBodyType.Ship;

        return UserOrbitSettings.OrbitBodyType.Unknown;
    }

    internal static string EntityName(Entity entity, int? faction = null)
    {
        var f = faction ?? Game.NeutralFactionId;
        var s = "??";
        if (entity.TryGetDataBlob<NameDB>(out NameDB nDB))
            s = nDB.GetName(f);
        return s;
    }

    internal static IEnumerable<SDL.DisplayMode> GetDisplayModes()
    {
        foreach (var i in SDL.GetDisplays(out _))
            foreach (var j in SDL.GetFullscreenDisplayModes(i, out _))
                yield return j;
    }
}
