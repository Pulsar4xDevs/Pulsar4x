using System;
using SDL2;

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
        /// <summary>
        /// Creates the arc.
        /// </summary>
        /// <returns>The arc.</returns>
        /// <param name="posX">Position x.</param>
        /// <param name="posY">Position y.</param>
        /// <param name="xWidth">X width.</param>
        /// <param name="yWidth">Y width.</param>
        /// <param name="startAngleRadians">Start angle in radians.</param>
        /// <param name="arcAngleRadians">Arc angle in radians.</param>
        /// <param name="segments">Number of segments this arc will have, resolution. ie a full circle with 6 arcs will draw a hexigon.</param>
        public static SDL.SDL_Point[] CreateArc(int posX, int posY, double xWidth, double yWidth, double startAngleRadians, double arcAngleRadians, int segments)
        {
            SDL.SDL_Point[] points = new SDL.SDL_Point[segments];

            double incrementAngle = arcAngleRadians / segments;

            int drawX;
            int drawY;

            for (int i = 0; i < segments; i++)
            {
                double nextAngle = startAngleRadians + incrementAngle * i;
                drawX = posX + (int)Math.Round(xWidth * Math.Sin(nextAngle));
                drawY = posY + (int)Math.Round(yWidth * Math.Cos(nextAngle));
                points[i] = new SDL.SDL_Point() { x = drawX, y = drawY };
            }

            return points;
        }


    }

    public class Camera
    {
        internal double ZoomLevel;
        internal double x;
        internal double y;

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

    public struct Shape
    {
        public SDL.SDL_Color Color;
        public SDL.SDL_Point[] Points;
    }
}
