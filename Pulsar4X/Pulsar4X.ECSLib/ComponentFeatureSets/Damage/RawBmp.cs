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
        
        public void SetPixel( int x, int y, byte r, byte g, byte b, byte a)
        {
            int offset = (Stride*y) + (x*4); //TODO: 4 is going to change with depth I think... maybe depth should be number of bytes.
            ByteArray[offset] = r;
            ByteArray[offset+1] = g;
            ByteArray[offset+2] = b;
            ByteArray[offset+3] = a;
        }

        public static void SetPixel(ref Byte[] buffer, int stride, int depth, int x, int y, byte r, byte g, byte b, byte a)
        {
            int offset = (stride*y) + (depth * x);
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