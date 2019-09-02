using System;

namespace Pulsar4X.ECSLib.ComponentFeatureSets.Damage
{
    public struct RawBmp
    {
        public byte[] ByteArray;
        public int Depth; //in bytes: in a picture bitmap this is the colour depth, ie a 8,8,8,8 bit rgba is 4 depth.
        public int Stride; //in a 32bit colour depth (ie 4 bits) this is 4 * width
        public int Width;
        public int Height;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="width">Canvas Width in pixels</param>
        /// <param name="height">Canvas Height in pixels</param>
        /// <param name="depth">Colour depth in bytes (default 4 for rgba)</param>
        public RawBmp(int width, int height, int depth = 4)
        {
            int size = depth * width * height;
            int stride = width * depth;

            ByteArray = new byte[size];
            Stride = stride;
            Depth = depth;
            Width = width;
            Height = height;
        }

        public void SetPixel( int x, int y, byte r, byte g, byte b, byte a)
        {
            int offset = (Stride * y) + (Depth * x); 
            ByteArray[offset] = r;
            ByteArray[offset+1] = g;
            ByteArray[offset+2] = b;
            ByteArray[offset+3] = a;
        }

        public static void SetPixel(ref Byte[] buffer, int stride, int depth, int x, int y, byte r, byte g, byte b, byte a)
        {
            int offset = (stride * y) + (depth * x);
            buffer[offset] = r;
            buffer[offset+1] = g;
            buffer[offset+2] = b;
            buffer[offset+3] = a;
        }

        public int GetOffset(int x, int y)
        {
            return Stride * y + Depth * x;
        }

        public (byte r, byte g, byte b, byte a) GetPixel(int x, int y)
        {
            int offset = (Stride * y) + (x * Depth);
            byte r = ByteArray[offset];
            byte g = ByteArray[offset + 1];
            byte b = ByteArray[offset + 2];
            byte a = ByteArray[offset + 3];
            return (r, g, b, a);
        }
        

    }
}