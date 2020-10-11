using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using Pulsar4X.Orbital;

namespace Pulsar4X.ECSLib.ComponentFeatureSets.Damage
{

    [Flags]
    public enum Connections
    {
        Skin = 1,
        Front = 2,
        Back = 4,
        Sides = 8,
        Structural = 15
    }

    public struct DamageResist
    {
        /*
         this could potentialy get more complex,
         ie transperency, damage passes through but does no damage, probibly need that.
         absorption, and threashold - can absorb heat to a melting point.
         relfection/resistance - will compleatly ignore/reduce damage under a value
        */
        public byte IDCode;
        public int HitPoints;
        //public float Heat;
        //public float Kinetic;
        public float Density;
        
        public DamageResist(byte iDCode, int hitPoints, float density)
        {
            IDCode = iDCode;
            HitPoints = hitPoints;
            //Heat = heat;
            //Kinetic = kinetic;
            Density = density; //kg/m^3
            DamageTools.DamageResistsLookupTable.Add(IDCode, this);
        }
    }
    
    public struct DamageFragment
    {
        public Vector2 Velocity;
        public (int x,int y) Position;
        public double Angle;
        public float Mass;
        public float Momentum;
        public float Density;//kg/m^3
        public float Length;
    }

    public static class DamageTools
    {
        public static Dictionary<byte, DamageResist> DamageResistsLookupTable = new Dictionary<byte, DamageResist>()
        {
            {0, new DamageResist() {IDCode = 0, HitPoints = 0}} //emptyspace
        };

        struct Bitmap
        {
            public int Height;
            public int Width;
            public int Stride;
            public byte[] PxArray;
        }
        
        public static RawBmp LoadFromBitMap(string file)
        {
            
            
            if(!File.Exists(file))
                throw new FileNotFoundException();

            byte[] bmpBytes = File.ReadAllBytes(file);
            (int offset,int size)[] headerDef = new (int,int)[12];
            headerDef[0] = (0,2); //first two bytes should be BM in ascii
            headerDef[1] = (2, 4); //size of bmp in bytes (whole file size)
            headerDef[3] = (6, 2); //reserved creation application dependant
            headerDef[4] = (6, 8); //reserved creaton applicaton dependant
            headerDef[5] = (10, 4); //startAddressOf imageByteArray
            //bitmapInfo:
            headerDef[6] = (14, 4); //headerSize in bytes: should be 12?
            headerDef[7] = (18, 4); //bmp width in px (ushort)
            headerDef[8] = (22, 4); //bmp height in px (ushort)
            headerDef[9] = (26, 2); //num of colour planes = 1
            headerDef[10] = (28, 2); //bits per px, shoudl be our colour depth;
            headerDef[11] = (34, 4); //image Size (raw bmp data)
            byte[][] headerDat = new byte[12][];

            for (int i = 0; i < headerDef.Length; i++)
            {
                int size = headerDef[i].size;
                int offset = headerDef[i].offset;
                headerDat[i] = new byte[headerDef[i].size];
                for (int j = 0; j < size; j++)
                {
                    headerDat[i][j] = bmpBytes[offset + j];
                }
                
            }

            int filesize = BitConverter.ToInt32(headerDat[1]);
            int start = BitConverter.ToInt16(headerDat[5]);
            int width = BitConverter.ToInt16(headerDat[7]);
            int height = BitConverter.ToInt16(headerDat[8]);
            int depth = BitConverter.ToInt16(headerDat[10]) / 8;//we want depth in bytes. 
            int sizeAry = BitConverter.ToUInt16(headerDat[11]); 
            
            //Note: the bmp is stored in memory above starting at the bottom left. 
            
            // Create an array to store image data
            byte[] imageData = new byte[4 * width * height];
            // Use the Marshal class to copy image data
            Buffer.BlockCopy(bmpBytes, start, imageData, 0, sizeAry);
            
            //byte[] imageData2 = new byte[4 * width * height];
            RawBmp newBmp = new RawBmp()
            {
                ByteArray = imageData,
                Depth = depth,
                Height = height,
                Width = width,
                Stride = depth * width
            };
            return newBmp;
            
            
            
        }

        public static Color FromByte(byte byteColor)
        {
            byte r = byteColor;
            byte g = byteColor;
            byte b = byteColor;
            Color color = Color.FromArgb(255,r, g, b);
            return color;
        }

        public static DamageResist FromColor(Color color)
        {
            byte id = color.R;
            return DamageResistsLookupTable[id];
        }
        
        public static RawBmp CreateComponentByteArray(ComponentDesign componentDesign, byte typeID)
        {
            //1 pixel = 1meter resolution
            var vol = componentDesign.VolumePerUnit * 1000;

            double floatdepth = Math.Pow(componentDesign.AspectRatio, (float)1 / 3);
            double CSA = componentDesign.VolumePerUnit / floatdepth;
            double floatwidth = Math.Sqrt(CSA) * (double)componentDesign.AspectRatio;
            int depth = (int)floatdepth;
            int width = (int)floatwidth;
            int height = (int)(CSA / width);
            int v2d = height * width;
            int volume = (int)vol;

            if (componentDesign.AspectRatio > 1)
            {
                width = (int)(width / componentDesign.AspectRatio);
                height = (int)(height / componentDesign.AspectRatio);
            }

            int imagedepth = 4;
            int size = imagedepth * width * height;
            int stride = width * imagedepth;
            
            byte[] buffer = new byte[size];

            for (int ix = 0; ix < width; ix++)
            {
                for (int iy = 0; iy < height; iy++)
                {
                    byte c = typeID;
                    RawBmp.SetPixel(ref buffer, stride, imagedepth, ix, iy, 255, 255,c, 255);
                }
            }

            RawBmp bmp = new RawBmp()
            {
                ByteArray =  buffer,
                Stride = stride,
                Depth = imagedepth,
                Width = width,
                Height = height,
                
            };
            return bmp;
        }


        public static List<RawBmp> DealDamage(EntityDamageProfileDB damageProfile, DamageFragment damage)
        {
            RawBmp shipDamageProfile = damageProfile.DamageProfile;

            List<RawBmp> damageFrames = new List<RawBmp>();

            var fragmentMass = damage.Mass;
            (int x, int y) dpos = (0, 0);
            var dvel = damage.Velocity;
            var dden = damage.Density;
            var dlen = damage.Length;
            var pos = new Vector2(dpos.x, dpos.y);
            var pixelscale = 0.01;
            double startMomentum = damage.Momentum;
            double momentum = startMomentum;


            
            //We need to figure out where the incoming damage intersects with the ship's damage profile "image"
            var pwidth = damageProfile.DamageProfile.Width;
            var hw = pwidth * 0.5;
            var phight = damageProfile.DamageProfile.Height;
            var hh = phight * 0.5;
            var len = Math.Sqrt((pwidth * pwidth) + (phight * phight));

            //damage.Position ralitive to our targets center, but we need to translate for calculating 0,0 at top left
            Vector2 start = new Vector2(damage.Position.x - hw, damage.Position.y - hh); 
            var end = new Vector2(pwidth * 0.5, phight * 0.5); //center of our target
            var tl = new Vector2(0, 0);
            var tr = new Vector2(pwidth, 0);
            var bl = new Vector2(0, phight);
            var br = new Vector2(pwidth, phight);
            
            //pretty sure these can be else ifs. 
            Vector2 intersection;

            //left
            if (LineIntersectsLine(start, end, tl, bl, out intersection))
            {
            }
            //right
            else if (LineIntersectsLine(start, end, tr, br, out intersection))
            {
            }
            //top
            else if (LineIntersectsLine(start,end,tl, tr, out intersection))
            {
            }
            //bottom
            else if (LineIntersectsLine(start,end,bl, br, out intersection))
            {
            }

            dpos.x = Convert.ToInt32(intersection.X);
            dpos.y = Convert.ToInt32(intersection.Y);

            byte[] byteArray = new byte[shipDamageProfile.ByteArray.Length];
            Buffer.BlockCopy(shipDamageProfile.ByteArray, 0, byteArray, 0, shipDamageProfile.ByteArray.Length);
            RawBmp firstFrame = new RawBmp()
            {
                ByteArray = byteArray,
                Height = shipDamageProfile.Height,
                Width = shipDamageProfile.Width,
                Depth = shipDamageProfile.Depth,
                Stride = shipDamageProfile.Stride
            };
            damageFrames.Add(firstFrame);
            (byte r, byte g, byte b, byte a) savedpx = shipDamageProfile.GetPixel(dpos.x, dpos.y);
            (int x, int y) savedpxloc = dpos;
            
            while (
                momentum > 0 && 
                dpos.x >= 0 && 
                dpos.x <= shipDamageProfile.Width && 
                dpos.y >= 0 && dpos.y <= shipDamageProfile.Height)
            {
                byteArray = new byte[shipDamageProfile.ByteArray.Length];
                RawBmp lastFrame = damageFrames.Last();
                Buffer.BlockCopy(lastFrame.ByteArray, 0, byteArray, 0, shipDamageProfile.ByteArray.Length);
                var thisFrame = new RawBmp()
                {
                    ByteArray = byteArray,
                    Height = shipDamageProfile.Height,
                    Width = shipDamageProfile.Width,
                    Depth = shipDamageProfile.Depth,
                    Stride = shipDamageProfile.Stride
                };

                (byte r, byte g, byte b, byte a) px = thisFrame.GetPixel(dpos.x, dpos.y);
                if (px.a > 0)
                {
                    DamageResist damageresist = DamageResistsLookupTable[px.r];

                    double density = damageresist.Density / (px.a / 255f); //density / health
                    double maxImpactDepth = dlen * dden / density;
                    double depthPercent = pixelscale / maxImpactDepth;
                    dlen -= (float)(damage.Length * depthPercent);
                    var momentumLoss = startMomentum * depthPercent;
                    momentum -= momentumLoss;
                    if (momentum > 0)
                    {
                        px = ( px.r, px.g, px.b, 0);
                        damageProfile.ComponentLookupTable[px.g].HTKRemaining -= 1;
                    }

                }

                thisFrame.SetPixel(dpos.x, dpos.y, byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)fragmentMass);
                thisFrame.SetPixel(savedpxloc.x, savedpxloc.y, savedpx.r, savedpx.g, savedpx.b, savedpx.a);
                damageFrames.Add(thisFrame);
                savedpxloc = dpos;
                savedpx = px;


                double dt = 1 / dvel.Length(); 
                pos.X += dvel.X * dt;
                pos.Y += dvel.Y * dt;
                dpos.x = Convert.ToInt32(pos.X);
                dpos.y = Convert.ToInt32(pos.Y);
            }

            
            Buffer.BlockCopy(damageFrames.Last().ByteArray, 0, byteArray, 0, shipDamageProfile.ByteArray.Length);
            var finalFrame = new RawBmp()
            {
                ByteArray = byteArray,
                Height = shipDamageProfile.Height,
                Width = shipDamageProfile.Width,
                Depth = shipDamageProfile.Depth,
                Stride = shipDamageProfile.Stride
            };
            finalFrame.SetPixel(savedpxloc.x, savedpxloc.y, savedpx.r, savedpx.g, savedpx.b, savedpx.a);
            damageProfile.DamageSlides.Add(damageFrames);
            damageProfile.DamageProfile = finalFrame;
            return damageFrames;
        }
        
        static bool LineIntersectsLine(Vector2 l1start, Vector2 l1End, Vector2 l2Start, Vector2 l2End, out Vector2 intersectsAt)
        {
            // calculate the direction of the lines
            var uA = 
                ((l2End.X-l2Start.X)*(l1start.Y-l2Start.Y) - (l2End.Y-l2Start.Y)*(l1start.X-l2Start.X)) / 
                ((l2End.Y-l2Start.Y)*(l1End.X-l1start.X) - (l2End.X-l2Start.X)*(l1End.Y-l1start.Y));
            var uB = 
                ((l1End.X-l1start.X)*(l1start.Y-l2Start.Y) - (l1End.Y-l1start.Y)*(l1start.X-l2Start.X)) / 
                ((l2End.Y-l2Start.Y)*(l1End.X-l1start.X) - (l2End.X-l2Start.X)*(l1End.Y-l1start.Y));

            // if uA and uB are between 0-1, lines are colliding
            if (uA >= 0 && uA <= 1 && uB >= 0 && uB <= 1) {
                double intersectionX = l1start.X + (uA * (l1End.X-l1start.X));
                double intersectionY = l1start.Y + (uA * (l1End.Y-l1start.Y));
                intersectsAt = new Vector2(intersectionX, intersectionY);
                return true;
            }

            intersectsAt = new Vector2();
            return false;
        }
    }
}