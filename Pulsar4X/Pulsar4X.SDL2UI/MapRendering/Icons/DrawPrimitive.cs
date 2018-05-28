using System;
using System.Collections.Generic;
using SDL2;
using Point = SDL2.SDL.SDL_Point;

namespace Pulsar4X.SDL2UI
{
    public static class DrawPrimitive
    {
        public static void DrawEllipse(IntPtr renderer, int posX, int posY, double xWidth, double yWidth)
        {
            byte _numberOfArcSegments = 255;

            double angle = (Math.PI * 2.0) / (_numberOfArcSegments);

            int lastX = posX + (int)Math.Round(xWidth * Math.Sin(angle));
            int lastY = posY + (int)Math.Round(yWidth * Math.Cos(angle));
            int drawX;
            int drawY;
            for (int i = 0; i < _numberOfArcSegments + 1; i++)
            {
                drawX = posX + (int)Math.Round(xWidth * Math.Sin(angle * i));
                drawY = posY + (int)Math.Round(yWidth * Math.Cos(angle * i));
                //SDL.SDL_RenderDrawPoint(renderer, drawX, drawY);
                SDL.SDL_RenderDrawLine(renderer, lastX, lastY, drawX, drawY);
                lastX = drawX;
                lastY = drawY;
            }
        }

        public static void DrawArc(IntPtr renderer, int posX, int posY, double xWidth, double yWidth, double startAngleRadians, double arcAngleRadians)
        {
            byte _numberOfArcSegments = 255;

            double incrementAngle = (Math.PI * 2.0) / (_numberOfArcSegments);

            int drawX;
            int drawY;
            int totalSegments = (int)(arcAngleRadians / incrementAngle);


            SDL.SDL_Point[] points = new SDL.SDL_Point[totalSegments];

            for (int i = 0; i < totalSegments; i++)
            {
                double nextAngle = startAngleRadians + incrementAngle * i;
                drawX = posX + (int)Math.Round(xWidth * Math.Sin(nextAngle));
                drawY = posY + (int)Math.Round(yWidth * Math.Cos(nextAngle));
                points[i] = new SDL.SDL_Point() { x = drawX, y = drawY };
            }

            for (int i = 0; i < points.Length - 1; i++)
            {
                SDL.SDL_RenderDrawLine(renderer, points[i].x, points[i].y, points[i+1].x, points[i+1].y);
            }

        }


        public static void DrawAlphaFadeArc(IntPtr rendererPtr, int posX, int posY, double xWidth, double yWidth, double startAngleRadians, double arcAngleRadians, byte startAlpha, byte endAlpha)
        {
            byte r, g, b, a;
            SDL.SDL_GetRenderDrawColor(rendererPtr, out r, out g, out b, out a);

            SDL.SDL_BlendMode blendMode;
            SDL.SDL_GetRenderDrawBlendMode(rendererPtr, out blendMode);

            SDL.SDL_SetRenderDrawBlendMode(rendererPtr, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
            byte _numberOfArcSegments = 254;


            byte alpha = endAlpha;

            double incrementAngle = (Math.PI * 2.0) / (_numberOfArcSegments);

            int lastX = posX + (int)Math.Round(xWidth * Math.Sin(startAngleRadians));
            int lastY = posY + (int)Math.Round(yWidth * Math.Cos(startAngleRadians));
            int drawX;
            int drawY;
            int totalSegments = (int)(arcAngleRadians / incrementAngle);
            double alphaIncrement = (startAlpha - endAlpha) / totalSegments;
            for (int i = 1; i < totalSegments; i++)
            {
                alpha += (byte)(alphaIncrement);
                SDL.SDL_SetRenderDrawColor(rendererPtr, r, g, b, alpha);

                double nextAngle = startAngleRadians + incrementAngle * i;
                drawX = posX + (int)Math.Round(xWidth * Math.Sin(nextAngle));
                drawY = posY + (int)Math.Round(yWidth * Math.Cos(nextAngle));
                SDL.SDL_RenderDrawLine(rendererPtr, lastX, lastY, drawX, drawY);
                lastX = drawX;
                lastY = drawY;
            }

            SDL.SDL_SetRenderDrawColor(rendererPtr, r, g, b, a); //set the colour back to what it was origionaly
            SDL.SDL_SetRenderDrawBlendMode(rendererPtr, blendMode);
        }
    }

    public static class CreatePrimitiveShapes
    {

        public const double PI2 = Math.PI * 2;
        public const double HalfCircle = Math.PI;
        public const double QuarterCircle = Math.PI * 0.5;
        public const double ThreeQuarterCircle = HalfCircle + QuarterCircle;


        /// <summary>
        /// Creates the arc.
        /// </summary>
        /// <returns>The arc.</returns>
        /// <param name="posX">Position x.</param>
        /// <param name="posY">Position y.</param>
        /// <param name="xRadius">X width.</param>
        /// <param name="yRadius">Y width.</param>
        /// <param name="startAngleRadians">Start angle in radians.</param>
        /// <param name="arcAngleRadians">Arc angle in radians.</param>
        /// <param name="segments">Number of segments this arc will have, resolution. ie a full circle with 6 arcs will draw a hexigon.</param>
        public static Point[] CreateArc(int posX, int posY, double xRadius, double yRadius, double startAngleRadians, double arcAngleRadians, int segments)
        {
            Point[] points = new SDL.SDL_Point[segments + 1];

            double incrementAngle = arcAngleRadians / segments;

            int drawX;
            int drawY;

            for (int i = 0; i < segments + 1; i++)
            {
                double nextAngle = startAngleRadians + incrementAngle * i;
                drawX = posX + (int)Math.Round(xRadius * Math.Sin(nextAngle));
                drawY = posY + (int)Math.Round(yRadius * Math.Cos(nextAngle));
                points[i] = new SDL.SDL_Point() { x = drawX, y = drawY };
            }

            return points;
        }

        public static Point[] RoundedCylinder(int minorRadius, int majorRadius, int offsetX, int offsetY)
        {
            List<Point> points = new List<Point>();
            int x1 = (int)(minorRadius * 0.5);
            int y1 = (int)(majorRadius * 0.5 - minorRadius * 0.5);

            points.AddRange(CreateArc(offsetX, y1 + offsetY, x1, x1, ThreeQuarterCircle, HalfCircle, 16));
            points.Add(new Point() { x = x1 + offsetX, y = y1 + offsetY });
            points.Add(new Point() { x = x1 + offsetX, y = -y1 + offsetY });

            points.AddRange(CreateArc(offsetX, -y1 + offsetY, x1, x1, QuarterCircle, HalfCircle, 16));
            points.Add(new Point() { x = -x1 + offsetX, y = -y1 + offsetY });
            points.Add(new Point() { x = -x1 + offsetX, y = y1 + offsetY });
            return points.ToArray();
        }

        public enum PosFrom
        {
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight,
            Center
        }
        public static Point[] Rectangle(int posX, int posY, int width, int height, PosFrom positionFrom = PosFrom.TopLeft)
        {

            var points = new Point[4] ;
            Point tl;
            Point tr;
            Point br;
            Point bl;

            switch (positionFrom)
            {
                case PosFrom.TopLeft:
                    {
                        tl.x = posX;
                        tl.y = posY;
                        tr.x = posX + width;
                        tr.y = posY;
                        br.x = posX + width;
                        br.y = posY + height;
                        bl.x = posX;
                        bl.y = posY + height;
                        points = new Point[] { tl, tr, br, bl };
                    }
                    break;
                case PosFrom.TopRight:
                    { 
                        tr.x = posX;
                        tr.y = posY;
                        br.x = posX;
                        br.y = posY + height;
                        bl.x = posX - width;
                        bl.y = posY + height;
                        tl.x = posX - width;
                        tl.y = posY;
                        points = new Point[] { tr, br, bl, tl };
                    }
                    break;
                case PosFrom.BottomRight:
                    {
                        br.x = posX;
                        br.y = posY;
                        bl.x = posX - width;
                        bl.y = posY;
                        tl.x = posX - width;
                        tl.y = posY - height;
                        tr.x = posX;
                        tr.y = posY - height;
                        points = new Point[] { br, bl, tl, tr };
                    }
                    break;
                case PosFrom.BottomLeft:
                    {

                        bl.x = posX;
                        bl.y = posY;
                        tl.x = posX;
                        tl.y = posY - height;
                        tr.x = posX + width;
                        tr.y = posY - height;
                        br.x = posX + width;
                        br.y = posY;
                        points = new Point[] { bl, tl, tr, br };
                    }
                    break;
                case PosFrom.Center:
                    {
                        tl.x = posX - (int)(width * 0.5);
                        tl.y = posY - (int)(height * 0.5);
                        tr.x = posX + (int)(width * 0.5);
                        tr.y = posY - (int)(height * 0.5);
                        br.x = posX + (int)(width * 0.5);
                        br.y = posY + (int)(height * 0.5);
                        bl.x = posX - (int)(width * 0.5);
                        bl.y = posY + (int)(height * 0.5);
                        points = new Point[] { tl, tr, br, bl, tl };
                    }
                    break;
            }
            return points;
        }
    }



    public static class DrawShapes
    {
        public static void Draw(IntPtr rendererPtr, Camera camera, Shape[] shapes)
        {
            //first get the current colour and blend mode and store this. 
            byte r, g, b, a;
            SDL.SDL_BlendMode blendMode;
            SDL.SDL_GetRenderDrawColor(rendererPtr, out r, out g, out b, out a);
            SDL.SDL_GetRenderDrawBlendMode(rendererPtr, out blendMode);

            //change the blendmode to blend (maybe we should store this in Shape? probilby not, I think we'll always want blend.)
            SDL.SDL_SetRenderDrawBlendMode(rendererPtr, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);

            //go through each of the shapes 
            foreach (var shape in shapes)
            {
                //set the colour as defined in the shape. 
                SDL.SDL_SetRenderDrawColor(rendererPtr, shape.Color.r, shape.Color.g, shape.Color.b, shape.Color.a);
                //then go through each of the points and draw a line from one point to the next. 
                for (int i = 0; i < shape.Points.Length - 1; i++)
                {
                    SDL.SDL_RenderDrawLine(rendererPtr, shape.Points[i].x, shape.Points[i].y, shape.Points[i + 1].x, shape.Points[i + 1].y);
                }
            }

            //set the colour and blendmode back to what it was origionaly.
            SDL.SDL_SetRenderDrawColor(rendererPtr, r, g, b, a); //set the colour back to what it was origionaly
            SDL.SDL_SetRenderDrawBlendMode(rendererPtr, blendMode);
        }
    }

}
