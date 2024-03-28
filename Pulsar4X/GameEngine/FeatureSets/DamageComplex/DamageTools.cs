using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using Pulsar4X.Blueprints;
using Pulsar4X.Orbital;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;

namespace Pulsar4X.Engine.Damage
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

    /// <summary>
    /// Merge this into materials?
    /// </summary>
    public class DamageResistBlueprint: Blueprint
    {
        /*
         this could potentialy get more complex,
         ie transperency, damage passes through but does no damage, probibly need that.
         absorption, and threashold - can absorb heat to a melting point.
         relfection/resistance - will compleatly ignore/reduce damage under a value
        */
        public byte IDCode;
        public int HitPoints;

        public int MeltingPoint;
        //public float Heat;
        //public float Kinetic;
        public float Density;

        
        public DamageResistBlueprint(byte iDCode, int hitPoints, float density) 
        {
            IDCode = iDCode;
            HitPoints = hitPoints;
            //Heat = heat;
            //Kinetic = kinetic;
            Density = density; //kg/m^3
            if (DamageTools.DamageResistsLookupTable.ContainsKey(iDCode))
                DamageTools.DamageResistsLookupTable[iDCode] = this;
            else 
                DamageTools.DamageResistsLookupTable.Add(IDCode, this);
        }
    }

    public struct DamageFragment
    {
        public Vector2 Velocity;
        public (int x,int y) Position;
        public double Heat;
        public float Mass;
        public float Momentum;
        public float Density;//kg/m^3
        public float Length;
    }

    public static class DamageTools
    {
        public static Dictionary<byte, DamageResistBlueprint> DamageResistsLookupTable = new Dictionary<byte, DamageResistBlueprint>()
        {
           
        };
        

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

        public static DamageResistBlueprint FromColor(Color color)
        {
            byte id = color.R;
            return DamageResistsLookupTable[id];
        }
        
        public static (List<(byte id, int damageAmount)> damageToComponents, List<RawBmp> damageFrames) DealDamageSim(EntityDamageProfileDB damageProfile, DamageFragment damage)
        {
            RawBmp shipDamageProfile = damageProfile.DamageProfile;

            List<RawBmp> damageFrames = new List<RawBmp>();
            List<(byte id, int damageAmount)> damageToComponents = new List<(byte, int)>();
            
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
            var pwIndex = pwidth - 1;//zero based arrays
            var hw = pwidth * 0.5;
            var phight = damageProfile.DamageProfile.Height;
            var phIndex = phight - 1;//zero based arrays
            var hh = phight * 0.5;
            var len = Math.Sqrt((pwidth * pwidth) + (phight * phight));

            //damage.Position ralitive to our targets center, but we need to translate for calculating 0,0 at top left
            Vector2 start = new Vector2(damage.Position.x - hw, damage.Position.y - hh);
            var end = new Vector2((pwidth * 0.5)-1, (phight * 0.5)-1); //center of our target
            var tl = new Vector2(0, 0);
            var tr = new Vector2(pwIndex, 0);
            var bl = new Vector2(0, phIndex);
            var br = new Vector2(pwIndex, phIndex);

            //pretty sure these can be else ifs.
            Vector2 intersection;

            //left
            if (GeneralMath.LineIntersectsLine(start, end, tl, bl, out intersection))
            {
            }
            //right
            else if (GeneralMath.LineIntersectsLine(start, end, tr, br, out intersection))
            {
            }
            //top
            else if (GeneralMath.LineIntersectsLine(start,end,tl, tr, out intersection))
            {
            }
            //bottom
            else if (GeneralMath.LineIntersectsLine(start,end,bl, br, out intersection))
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
                    DamageResistBlueprint damageresist = DamageResistsLookupTable[px.r];

                    double density = damageresist.Density / (px.a / 255f); //density / health
                    double maxImpactDepth = dlen * dden / density;
                    double depthPercent = pixelscale / maxImpactDepth;
                    dlen -= (float)(damage.Length * depthPercent);
                    var momentumLoss = startMomentum * depthPercent;
                    momentum -= momentumLoss;
                    if (momentum > 0)
                    {
                        px = ( px.r, px.g, px.b, 0);
                        damageToComponents.Add((px.g, 1));
                    }
                }
                
                //this is the damage fragment
                thisFrame.SetPixel(dpos.x, dpos.y, byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)momentum);
                
                //this is the entity being damaged.
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
            //damageProfile.DamageSlides.Add(damageFrames);
            
            damageProfile.DamageEvents.Add(damage);
            damageProfile.DamageProfile = finalFrame;
            return (damageToComponents, damageFrames);
        }
    }
}