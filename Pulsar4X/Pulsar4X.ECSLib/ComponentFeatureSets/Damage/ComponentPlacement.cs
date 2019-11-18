using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework.Constraints;
using Pulsar4X.Vectors;

namespace Pulsar4X.ECSLib.ComponentFeatureSets.Damage
{
    public static class ComponentPlacement
    {
        public static RawBmp CreateShipBmp(EntityDamageProfileDB shipProfile)
        {
            byte armorID = 255;//shipProfile.Armor.IDCode;
            var po = shipProfile.PlacementOrder;
            
            List<(Guid typeID, RawBmp bmp)> typeBitmaps = shipProfile.TypeBitmaps;
            int componentWidthNum = 0;

            int totalLen = 0;
            var totalWidth = 0;
            int widestPoint = 0;
            int widestLen = 0;

            byte componentInstance = 0;
            
            for (int i = 0; i < po.Count; i++)
            {
                
                var typeid = po[i].id;
                var count = po[i].count;
                var typeBmp = typeBitmaps[i].bmp;

  
                if (count > componentWidthNum)
                    componentWidthNum = count;

                totalLen += typeBmp.Width;

                int width = typeBmp.Height * count;
                if (width > totalWidth)
                {
                    totalWidth = width;
                    widestPoint = totalLen;
                    widestLen = typeBmp.Width;
                }
            }

            int canvasWidth = totalWidth + 6; //create a bit larger canvas size for the armor.
            int canvasLen = totalLen + 6;


            int size = 4 * canvasLen * canvasWidth;
            int stride = canvasLen * 4;
            RawBmp shipBmp = new RawBmp()
            {
                ByteArray = new byte[size],
                Stride = stride,
                Depth =  4,
                Width = canvasLen,
                Height = canvasWidth,
            };

            if (po.Count <= 0)
            {
                return shipBmp;
            }

            byte[] shipByteArray = new byte[size];

            int offsetx = 4;
            for (int i = 0; i < po.Count; i++)
            {
                if (po[i].count == 0)
                {
                    continue;
                }
                var typeBmp = typeBitmaps[i].bmp;
                componentInstance++;
                var typeid = po[i].id;
                var count = po[i].count;
                int maxPixHeight = typeBmp.Height * count;
                
                for (int o = 0; o < count; o++)
                {

                    //pixHeight -= - (o * typeBmp.Height);
                    int offsety = (canvasWidth - maxPixHeight) / 2;
                    offsety += typeBmp.Height * o;

                    //not doing anything?
                    int bytesPerLine = 4 * typeBmp.Width;

                    for (int x = 0; x < typeBmp.Width; x++)
                    {
                        for (int y = 0; y < typeBmp.Height; y++)
                        {
                            var srsClr = typeBmp.GetPixel(x, y);
                            int destx = offsetx + x;
                            int desty = offsety + y;
                            shipBmp.SetPixel(destx, desty, srsClr.r, componentInstance, srsClr.b, srsClr.a);
                        }
                    }
                }

                /*
                for (int j = 0; j < count; j++)
                {
                    for (int pxstrip = 0; pxstrip < typeBmp.Height; pxstrip++)
                    {
                        int srcpos = typeBmp.Stride * pxstrip;
                        int destPos = shipBmp.GetOffset(offsetx, offsety + pxstrip + (typeBmp.Height * j));

                        Buffer.BlockCopy(typeBmp.ByteArray, srcpos, shipByteArray, destPos, bytesPerLine);
                    }
                }
                */
                offsetx += typeBmp.Width;

            }

            //shipBmp.ByteArray = shipByteArray;

            //TODO: somehow make lines thicker without crashing
           

            //below is a failed attempt at trying to make lines thicker
            //int bezzierMargin = 50;
            //widestPoint -= bezzierMargin;
            //widestLen -= bezzierMargin
            //bool canvasWidthIsBigEnough = canvasWidth > bezzierMargin;
            //bool canvasLenIsBigEnough = canvasLen > bezzierMargin;
            //canvasWidth = (canvasWidthIsBigEnough ? canvasWidth - bezzierMargin : canvasWidth);
            //canvasLen = (canvasLenIsBigEnough?canvasLen-bezzierMargin:canvasLen);
            //((canvasWidthIsBigEnough&&canvasLenIsBigEnough)?4:0);


            float addedLineThickness = 0;


           


            //create bezzier control points for front and rear armor/skin

            (int x, int y)[] controlPointsFore = new (int x, int y)[4];

            controlPointsFore[0] = (0, canvasWidth / 2);
            controlPointsFore[1] = (0, 2);
            controlPointsFore[2] = (0, 2);
            controlPointsFore[3] = (widestPoint - widestLen, 2);
            
            (int x, int y)[] controlPointsAft = new (int x, int y)[4];
            int lastbmpHeight = typeBitmaps[po.Count -1].bmp.Height * po[po.Count -1].count / 2;
            int lastbmpLen = typeBitmaps[po.Count -1].bmp.Width ;
            
            controlPointsAft[0] = (widestPoint + 3 , 2); //add three for the extra canvas size
            controlPointsAft[1] = (totalLen - lastbmpLen, 2 + lastbmpHeight);
            controlPointsAft[2] = (totalLen - lastbmpLen, 2 + lastbmpHeight);
            controlPointsAft[3] = (canvasLen, canvasWidth / 2 -  lastbmpHeight);
            
            List<(int x, int y)> linePoints = new List<(int x, int y)>();
            float dt = 0.01f;
            for (float t = 0.0f; t < 1.0; t += dt)
            {
                var x = BezCalc(t, controlPointsFore[0].x, controlPointsFore[1].x, controlPointsFore[2].x, controlPointsFore[3].x);
                var y = BezCalc(t, controlPointsFore[0].y, controlPointsFore[1].y, controlPointsFore[2].y, controlPointsFore[3].y);
                
                linePoints.Add(((int)x, (int)y));
                
            }
            
            for (float t = 0.0f; t < 1.0; t += dt)
            {
                var x = BezCalc(t, controlPointsAft[0].x, controlPointsAft[1].x, controlPointsAft[2].x, controlPointsAft[3].x);
                var y = BezCalc(t, controlPointsAft[0].y, controlPointsAft[1].y, controlPointsAft[2].y, controlPointsAft[3].y);
                
                linePoints.Add(((int)x, (int)y));
                
            }
            
            //set the pixels in teh bitmap for the bezier curves.
            var coordStart = linePoints[0];
            for (int i = 1; i < linePoints.Count; i++)
            {
                var coordEnd = linePoints[i];
                ThickLine(shipBmp, coordStart, coordEnd,  shipProfile.Armor.thickness / 10 + addedLineThickness, 255, 255, 255, 255);
                //Mirror:
                coordStart = (coordStart.x, canvasWidth - coordStart.y);
               
                coordEnd = (coordEnd.x, canvasWidth - coordEnd.y);
                
                ThickLine(shipBmp, coordStart, coordEnd,  shipProfile.Armor.thickness / 10 + addedLineThickness, 255, 255, 255, 255);
                coordStart = linePoints[i];
            }
            
            //connect the front and rear armor/skin.
            //TODO: handle angles, currently expects start and end y to be the same.
            (int x, int y) straightStart = controlPointsFore[3];
            (int x, int y) straightEnd = controlPointsAft[0];
            
            ThickLine(shipBmp, straightStart, straightEnd,  shipProfile.Armor.thickness / 10 + addedLineThickness, 255, 255, 255, 255);
            //Mirror:
            straightStart = (straightStart.x, canvasWidth - straightStart.y);
            straightEnd = (straightEnd.x, canvasWidth - straightEnd.y);
            ThickLine(shipBmp, straightStart, straightEnd,  shipProfile.Armor.thickness / 10 + addedLineThickness, 255, 255, 255, 255);


            
             //adding margins to the bitmap(white space around its edges to make it look cleaner once displayed)
            Vector2 shipbmpMargins = new Vector2(shipBmp.Width*0.1,shipBmp.Height*0.1);
            RawBmp finalShipBmp = new RawBmp(shipBmp.Width + (int)shipbmpMargins.X*2, shipBmp.Height+ (int)shipbmpMargins.Y*2, shipBmp.Depth);
            //shifting 
            for (int x = 0; x < shipBmp.Width; x++)
            {
                for (int y = 0; y < shipBmp.Height; y++)
                {
                    var srsClr = shipBmp.GetPixel(x,y);
                    finalShipBmp.SetPixel(x+(int)shipbmpMargins.X, y+(int)shipbmpMargins.Y, srsClr.r, srsClr.g, srsClr.b, srsClr.a);
                }
            }

            shipBmp = finalShipBmp;

            shipProfile.DamageProfile = shipBmp;
            return shipBmp;
        }
        
        private static double BezCalc(double t, double a0, double a1, double a2, double a3)
        {
            double foo = a0 * Math.Pow((1 - t), 3); 
            foo += a1 * 3 * t * Math.Pow((1 - t), 2); 
            foo += a2 * 3 * Math.Pow(t, 2) * (1 - t); 
            foo += a3 * Math.Pow(t, 3);
            return foo;
        }



        static void ThickLine(RawBmp bmp, (int x, int y) coordStart, (int x, int y) coordEnd, float wd, byte r, byte g, byte b, byte a)
        {
            var x0 = coordStart.x;
            var y0 = coordStart.y;
            var x1 = coordEnd.x;
            var y1 = coordEnd.y;
            
            int dx = Math.Abs(x1 - x0);
            int sx = x0 < x1 ? 1 : -1; 
            int dy = Math.Abs(y1-y0), sy = y0 < y1 ? 1 : -1; 
            int err = dx-dy, e2, x2, y2;                          /* error value e_xy */
            float ed = (float)( dx+dy == 0 ? 1 : Math.Sqrt((float)dx*dx+(float)dy*dy));
            byte alph = a;
            for (wd = (wd+1)/2; ; ) 
            {                                   /* pixel loop */
                alph = (byte)Math.Max(0, r * Math.Abs(err-dx+dy)/ed-wd+1);
                bmp.SetPixel(x0, y0, r, g, b, alph);
                e2 = err; x2 = x0;
                if (2*e2 >= -dx) 
                {                                           /* x step */
                    for (e2 += dy, y2 = y0; e2 < ed * wd && (y1 != y2 || dx > dy); e2 += dx)
                    {
                        alph = (byte)Math.Max(0, a * (Math.Abs(e2) / ed - wd + 1));
                        bmp.SetPixel(x0, y2 += sy, r, g, b, alph);
                    }

                    if (x0 == x1) break;
                    e2 = err; err -= dy; x0 += sx; 
                } 
                if (2*e2 <= dy) 
                {                                            /* y step */
                    for (e2 = dx - e2; e2 < ed * wd && (x1 != x2 || dx < dy); e2 += dy)
                    {
                        alph = (byte)Math.Max(0, a * (Math.Abs(e2) / ed - wd + 1));
                        bmp.SetPixel(x2 += sx, y0, r, g, b, alph);
                    }

                    if (y0 == y1) break;
                    err += dx; y0 += sy; 
                }
            }
        }
    }
}