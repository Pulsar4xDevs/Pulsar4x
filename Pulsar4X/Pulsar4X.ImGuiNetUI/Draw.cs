using System;
using System.Collections.Generic;
using SDL2;

namespace Pulsar4X.SDL2UI
{
    /// <summary>
    /// Drawing helpers class, inverts Y on drawcalls 
    /// </summary>
    public static class DrawTools
    {
        /// <summary>
        /// Calls SDL_RenderDrawLine 
        /// </summary>
        /// <param name="rendererPtr">Renderer ptr.</param>
        /// <param name="x1">The first x value.</param>
        /// <param name="y1">The first y value.</param>
        /// <param name="x2">The second x value.</param>
        /// <param name="y2">The second y value.</param>
        public static int DrawLine(IntPtr rendererPtr, int x1, int y1, int x2, int y2)
        {
            return SDL.SDL_RenderDrawLine(rendererPtr, x1, y1, x2, y2);
        }

        /// <summary>
        /// Calls SDL_DrawPoint 
        /// </summary>
        /// <param name="rendererPtr">Renderer ptr.</param>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        public static int DrawPoint(IntPtr rendererPtr, int x, int y)
        {
            return SDL.SDL_RenderDrawPoint(rendererPtr, x, y);
        }

        /// <summary>
        /// Rotates a given point to a given angle.
        /// </summary>
        /// <returns>The point.</returns>
        /// <param name="point">Point.</param>
        /// <param name="angle">Angle.</param>
        public static PointD RotatePoint(PointD point, double angle)
        {
            PointD newPoint = new PointD()
            {
                X = (point.X * Math.Cos(angle)) - (point.Y * Math.Sin(angle)),
                Y = (point.X * Math.Sin(angle)) + (point.Y * Math.Cos(angle))
            };
            return newPoint;
        }
    }

    /// <summary>
    /// A collection of points and a single color.
    /// </summary>
    public struct Shape
    {
        public SDL.SDL_Color Color;    //could change due to entity changes. 
        public PointD[] Points; //ralitive to the IconPosition. could change with entity changes. 
    }

    public class MutableShape
    {
        public SDL.SDL_Color Color;
        public List<PointD> Points;
        public bool Scales = true;
    }


    public class ComplexShape
    {
        public PointD StartPoint;
        public PointD[] Points;
        public SDL.SDL_Color[] Colors;
        public (int pointIndex, int colourIndex)[] ColourChanges; //at Points[item1] we change to Colors[item2]
        public bool Scales;

    }

    internal class ElementItem
    {
        internal string NameString;
        internal double DataItem;
        internal string DataString;
        internal ComplexShape Shape;
        internal SDL.SDL_Color[] Colour;
        internal SDL.SDL_Color[] HighlightColour;

        internal void SetHighlight(bool isHighlighted)
        {
            if (isHighlighted)
                Shape.Colors = HighlightColour;
            else
                Shape.Colors = Colour;
        }
    }

}
