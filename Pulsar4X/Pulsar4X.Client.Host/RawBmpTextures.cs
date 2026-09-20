using System;
using Pulsar4X.Client;
using Pulsar4X.DataStructures;
using SDL3;

namespace Pulsar4X.Client.Host;

/// <summary>SDL texture upload for the engine's <see cref="RawBmp"/> (damage maps, ship profiles) —
/// host-side because RawBmp is an engine type.</summary>
public static class RawBmpTextures
{
    public static void CreateTexture(IntPtr renderer, RawBmp rawBmp, ref IntPtr texturePtr,
            SDL.PixelFormat pixelFormat = SDL.PixelFormat.RGBA8888)
    {
        IntPtr pixels;
        unsafe
        {
            fixed (byte* ptr = rawBmp.ByteArray)
            {
                pixels = new IntPtr(ptr);
            }
        }

        Textures.CreateTexture(renderer, ref texturePtr, rawBmp.Width, rawBmp.Height, rawBmp.Depth * 8, rawBmp.Stride, pixels, pixelFormat);
    }
}
