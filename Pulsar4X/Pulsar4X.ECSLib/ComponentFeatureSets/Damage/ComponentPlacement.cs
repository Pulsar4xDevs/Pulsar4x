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
            List<(int width, int height)> partsize = new List<(int width, int height)>();
            int componentWidthNum = 0;

            int totalLen = 0;
            var totalWidth = 0;

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

                partsize.Add(((int)typeBmp.Width, (int)typeBmp.Height * count));

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
          


            float addedLineThickness = 5;

            //adding margins to the bitmap(white space around its edges to make it look cleaner once displayed)
            Vector2 shipbmpMargins = new Vector2(shipBmp.Width*0.1+addedLineThickness,shipBmp.Height*0.1+addedLineThickness);
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
          
            List<(int x, int y)> linePoints = new List<(int x, int y)>();
            List<int> lineHeight = new List<int>();
            

            int spacer = 5;

            int numparts = partsize.Count();


            //Grabs the height of each transtion between parts
            for (int partnum = 0; partnum < numparts - 1; partnum++)
            {
                if (partsize[partnum].height > partsize[partnum + 1].height)
                    lineHeight.Add(partsize[partnum].height / 2);
                else
                    lineHeight.Add(partsize[partnum + 1].height / 2);
            }
            lineHeight.Add(partsize[numparts - 1].height / 2);

            linePoints.Add((0, 0));

            //Adds a point at each transtions
            int currentx = 0;
            for (int partnum = 0; partnum < numparts; partnum++)
            {
                currentx += partsize[partnum].width;
                linePoints.Add((currentx, lineHeight[partnum] + spacer));        
            }


            var halfwidth = canvasWidth / 2;

            var bottomcoordStart = linePoints[0];
            var topcoordStart = linePoints[0];
            bottomcoordStart = (bottomcoordStart.x, halfwidth + bottomcoordStart.y);
            topcoordStart = (topcoordStart.x, halfwidth - topcoordStart.y);
            for (int i = 1; i < linePoints.Count; i++)
            {
                //Draws the bottom line
                var bottomcoordEnd = linePoints[i];
                bottomcoordEnd = (bottomcoordEnd.x, halfwidth + bottomcoordEnd.y);

                ThickLine(shipBmp, bottomcoordStart, bottomcoordEnd,  shipProfile.Armor.thickness / 10 + addedLineThickness, 255, 255, 255, 255, shipbmpMargins);
                bottomcoordStart = bottomcoordEnd;

                //Draws the top line
                var topcoordEnd = linePoints[i];
                topcoordEnd = (topcoordEnd.x, halfwidth - topcoordEnd.y);

                ThickLine(shipBmp, topcoordStart, topcoordEnd,  shipProfile.Armor.thickness / 10 + addedLineThickness, 255, 255, 255, 255, shipbmpMargins);
                topcoordStart = topcoordEnd;
            }
            

             

            shipProfile.DamageProfile = shipBmp;
            return shipBmp;
        }
       


        static void ThickLine(RawBmp bmp, (int x, int y) coordStart, (int x, int y) coordEnd, float wd, byte r, byte g, byte b, byte a, Vector2 margins)
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
                bmp.SetPixel(x0 + (int)margins.X, y0 + (int)margins.Y, r, g, b, alph);
                e2 = err; x2 = x0;
                if (2*e2 >= -dx) 
                {                                           /* x step */
                    for (e2 += dy, y2 = y0; e2 < ed * wd && (y1 != y2 || dx > dy); e2 += dx)
                    {
                        alph = (byte)Math.Max(0, a * (Math.Abs(e2) / ed - wd + 1));
                        bmp.SetPixel(x0+(int)margins.X, (y2 += sy)+(int)margins.Y, r, g, b, alph);
                    }

                    if (x0 == x1) break;
                    e2 = err; err -= dy; x0 += sx; 
                } 
                if (2*e2 <= dy) 
                {                                            /* y step */
                    for (e2 = dx - e2; e2 < ed * wd && (x1 != x2 || dx < dy); e2 += dy)
                    {
                        alph = (byte)Math.Max(0, a * (Math.Abs(e2) / ed - wd + 1));
                        bmp.SetPixel((x2 += sx)+(int)margins.X, y0+(int)margins.Y, r, g, b, alph);
                    }

                    if (y0 == y1) break;
                    err += dx; y0 += sy; 
                }
            }
        }
        static void ThickLine(RawBmp bmp, (int x, int y) coordStart, (int x, int y) coordEnd, float wd, byte r, byte g, byte b, byte a)
        {
            ThickLine(bmp, coordStart, coordEnd, wd, r, g, b, a, new Vector2(0,0));
            
        }
    }
}