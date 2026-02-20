using System;
using System.Collections.Generic;
using Pulsar4X.Orbital;
using SDL3;

namespace Pulsar4X.Client
{
    /// <summary>
    /// Drawing helpers class, inverts Y on drawcalls
    /// </summary>
    public static class DrawTools
    {

        /// <summary>
        /// Rotates a given point to a given angle.
        /// </summary>
        /// <returns>The point.</returns>
        /// <param name="point">Point.</param>
        /// <param name="angle">Angle.</param>
        public static Vector2 RotatePoint(Vector2 point, double angle)
        {
            Vector2 newPoint = new Vector2()
            {
                X = (point.X * Math.Cos(angle)) - (point.Y * Math.Sin(angle)),
                Y = (point.X * Math.Sin(angle)) + (point.Y * Math.Cos(angle))
            };
            return newPoint;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="point"></param>
        /// <param name="angle">in radians</param>
        /// <param name="orgin">rotate around this point</param>
        /// <returns></returns>
        public static Vector2 RotatePointAround(Vector2 point, double angle, Vector2 orgin)
        {

            var tmtx = Matrix2d.IDTranslate(-orgin.X, -orgin.Y);
            var rotmtx = Matrix2d.IDRotate(-angle);
            var tmtx2 = Matrix2d.IDTranslate(orgin.X, orgin.Y);

            var mtx = tmtx * rotmtx * tmtx2;
            return mtx.Transform(point);
        }
    }

    /*
    FIXME: Improve this.
    Maybe SDL_Vertex and SDL_RenderGeometry would be useful here?
    https://wiki.libsdl.org/SDL3/SDL_RenderGeometry
    */
    public interface IShape
    {
        bool Contains(System.Drawing.PointF point);
    }

    // TODO: Rename to "Polygon"
    /// <summary>
    /// A collection of points and a single color.
    /// </summary>
    public class Shape : IShape
    {
        public SDL.Color Color;    //could change due to entity changes.
        public Vector2[] Points; //relative to the IconPosition. could change with entity changes.

        // https://stackoverflow.com/a/14998816
        public bool Contains(System.Drawing.PointF point)
        {
            bool result = false;
            int j = Points.Length - 1;
            for (int i = 0; i < Points.Length; i++)
            {
                if (Points[i].Y < point.Y && Points[j].Y >= point.Y ||
                        Points[j].Y < point.Y && Points[i].Y >= point.Y)
                {
                    if (Points[i].X + (point.Y - Points[i].Y) /
                            (Points[j].Y - Points[i].Y) *
                            (Points[j].X - Points[i].X) < point.X)
                    {
                        result = !result;
                    }
                }
                j = i;
            }
            return result;
        }
    }

    public class ComplexShape
    {
        public Vector2 StartPoint;
        public Vector2[]? Points;
        public SDL.Color[]? Colors;
        public (int pointIndex, int colourIndex)[]? ColourChanges; //at Points[item1] we change to Colors[item2]
        public bool Scales;

    }

    internal class ElementItem
    {
        internal string? NameString;
        internal double DataItem;
        internal string DataString = "";
        internal ComplexShape? Shape;
        internal SDL.Color[]? Colour;
        internal SDL.Color[]? HighlightColour;
        internal bool IsEnabled = false;
        internal bool ShowLines = false;

        internal void SetHighlight(bool isHighlighted)
        {
            if(Shape == null) return;

            if (isHighlighted)
                Shape.Colors = HighlightColour;
            else
                Shape.Colors = Colour;
        }
    }

}
