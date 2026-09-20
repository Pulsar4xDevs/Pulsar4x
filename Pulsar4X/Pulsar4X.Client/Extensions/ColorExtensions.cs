using System.Drawing;
using SDL3;
using System;

namespace Pulsar4X.Client
{
    public static class ColorExtensions
    {
        public static byte FloatToByte(float f) =>
            (byte)MathF.Floor(f >= 1 ? 255 : f * 256);

        public static float ByteToFloat(byte b) =>
            (1.0f / 255) * b;

        // ImVector4 convert
        public static Color ToColor(this System.Numerics.Vector4 vec)
        {
            return Color.FromArgb(
                    ColorExtensions.FloatToByte(vec.W),
                    ColorExtensions.FloatToByte(vec.X),
                    ColorExtensions.FloatToByte(vec.Y),
                    ColorExtensions.FloatToByte(vec.Z));
        }

        public static SDL.Color ToSDLColor(this System.Numerics.Vector4 vec)
        {
            return new () {
                R = ColorExtensions.FloatToByte(vec.X),
                G = ColorExtensions.FloatToByte(vec.Y),
                B = ColorExtensions.FloatToByte(vec.Z),
                A = ColorExtensions.FloatToByte(vec.W)
            };
        }

        // Color convert
        public static SDL.Color ToSDLColor(this Color color)
        {
            return new () {
                R = color.R,
                G = color.G,
                B = color.B,
                A = color.A
            };
        }

        public static System.Numerics.Vector4 ToImVector4(this Color color)
        {
            return new (
                    ColorExtensions.ByteToFloat(color.R),
                    ColorExtensions.ByteToFloat(color.G),
                    ColorExtensions.ByteToFloat(color.B),
                    ColorExtensions.ByteToFloat(color.A));
        }

        // SDL.Color convert
        public static Color ToColor(this SDL.Color color)
        {
            return Color.FromArgb(
                    color.A,
                    color.R,
                    color.G,
                    color.B);
        }

        public static System.Numerics.Vector4 ToImVector4(this SDL.Color color)
        {
            return new (
                    ColorExtensions.ByteToFloat(color.R),
                    ColorExtensions.ByteToFloat(color.G),
                    ColorExtensions.ByteToFloat(color.B),
                    ColorExtensions.ByteToFloat(color.A));
        }
    }
}
