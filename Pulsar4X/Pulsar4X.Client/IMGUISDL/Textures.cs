using System;
using System.Runtime.InteropServices;
using Pulsar4X.Client.Rendering;
using Pulsar4X.DataStructures;
using SDL3;

namespace Pulsar4X.Client;

public static class Textures
{
    public static void CreateTexture(IntPtr renderer, ref IntPtr texture, int width, int height, int depth, int stride, IntPtr pixels, PixelFormat pixelFormat = PixelFormat.RGBA8888, TextureFilter textureFilter = TextureFilter.Linear)
    {
        // Create the masks based on the requested pixel format
        uint rmask, gmask, bmask, amask;

        switch(pixelFormat)
        {
            case PixelFormat.ARGB8888:
                amask = 0xff000000;
                rmask = 0x00ff0000;
                gmask = 0x0000ff00;
                bmask = 0x000000ff;
                break;
            case PixelFormat.RGBA8888:
            default:
                rmask = 0xff000000;
                gmask = 0x00ff0000;
                bmask = 0x0000ff00;
                amask = 0x000000ff;
                break;
        }

        // Create the surface
        IntPtr surface = CreateRGBSurfaceFrom(
                                pixels,
                                width,
                                height,
                                depth,
                                stride,
                                rmask,
                                gmask,
                                bmask,
                                amask);

        texture = SDL.CreateTextureFromSurface(renderer, surface);
        SDL.DestroySurface(surface);
    }
    public static void CreateTexture(IntPtr renderer, RawBmp rawBmp, ref IntPtr texturePtr, PixelFormat pixelFormat = PixelFormat.RGBA8888, TextureFilter textureFilter = TextureFilter.Linear)
    {
        IntPtr pixels;
        unsafe
        {
            fixed (byte* ptr = rawBmp.ByteArray)
            {
                pixels = new IntPtr(ptr);
            }
        }

        CreateTexture(renderer, ref texturePtr, rawBmp.Width, rawBmp.Height, rawBmp.Depth * 8, rawBmp.Stride, pixels, pixelFormat, textureFilter);
    }

    public static IntPtr CreateTextureFromSurface(IntPtr renderer, IntPtr surface)
    {
        return SDL.CreateTextureFromSurface(renderer, surface);
    }

    public static void UpdateOrCreate(IntPtr renderer, ref IntPtr texture, int width, int height, IntPtr pixels)
    {
        // If the texture doesn't exist, create it
        if(texture == IntPtr.Zero)
        {
            CreateTexture(renderer, ref texture, width, height, 32, width * 4, pixels, PixelFormat.RGBA8888, TextureFilter.Nearest);
            return;
        }
        // If the dimensions don't match, recreate the texture
        (int txWidth, int txHeight) = GetTextureSize(texture);
        if(width != txWidth || height != txHeight)
        {
            CreateTexture(renderer, ref texture, width, height, 32, width * 4, pixels, PixelFormat.RGBA8888, TextureFilter.Nearest);
            return;
        }
        else
            UpdateTexture(texture, pixels, width * 4);
    }

    public static void UpdateTexture(IntPtr texture, IntPtr pixels, int pitch)
    {
        SDL.UpdateTexture(texture, IntPtr.Zero, pixels, pitch);
    }

    public static void DeleteTexture(IntPtr texture)
    {
        SDL.DestroyTexture(texture);
    }

    public static nint CreateRGBSurfaceFrom(nint pixels, int width, int height, int depth, int pitch, uint rmask, uint gmask, uint bmask, uint amask)
    {
        var pixelFormat = SDL.GetPixelFormatForMasks(depth, rmask, gmask, bmask, amask);

        return SDL.CreateSurfaceFrom(
            width, height,
            pixelFormat,
            pixels,
            pitch
        );
    }

    public static (int, int) GetTextureSize(IntPtr texture)
    {
        SDL.GetTextureSize(texture, out float w, out float h);
        return ((int)w, (int)h);
    }

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
}