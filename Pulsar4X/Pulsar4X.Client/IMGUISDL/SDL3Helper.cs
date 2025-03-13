using System;
using System.Runtime.InteropServices;
using Pulsar4X.Client;

namespace ImGuiSDL2CS;

public static class SDL3Helper
{
    internal static void CreateTestTexture(IntPtr renderer, ref IntPtr texture)
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
            Textures.UpdateOrCreate(renderer, ref texture, width, height, pixels);
        }
        finally
        {
            handle.Free();
        }
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

