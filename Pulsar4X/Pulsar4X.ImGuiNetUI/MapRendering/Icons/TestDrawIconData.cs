using System;
using Pulsar4X.ECSLib;
using SDL2;
using System.Collections.Generic;

namespace Pulsar4X.SDL2UI
{
    public static class TestDrawIconData
    {
        


        public static List<Icon> GetTestIcons()
        {

            List<Icon> icons = new List<Icon>();
            Setup(icons);
            return icons;
        }
        private static void Setup(List<Icon> icons)
        {
            
            List<Shape> shapes = new List<Shape>();
            PointD[] lpoints1 = new PointD[] {
                new PointD { X = 0, Y = -160 },
                new PointD { X = 0, Y = 160 },
            };
            PointD[] lpoints2 = new PointD[] {
                new PointD { X = -25, Y = 0 },
                new PointD { X = 25, Y = 0 }
            };
            SDL.SDL_Color lcolor = new SDL.SDL_Color() { r = 0, g = 255, b = 0, a = 255 };
            shapes.Add( new Shape() { Points = lpoints1, Color = lcolor });
            shapes.Add( new Shape() { Points = lpoints2, Color = lcolor });
            PositionDB lpos = new PositionDB(new Vector4(0, 0, 0, 0), new Guid());

            icons.Add(new Icon(lpos) { Shapes = shapes });

            for (int i = 0; i < 4; i++)
            {
                PointD[] points = CreatePrimitiveShapes.CreateArc(50 + 50 * i, 400, 100, 100, 0, 4.71, 160);
                SDL.SDL_Color color = new SDL.SDL_Color() { r = (byte)(i * 60), g = 100, b = 100, a = 255 };
                Shape shape = new Shape() { Points = points, Color = color };
                PositionDB pos1 = new PositionDB(new Vector4(0, 0, 0, 0), new Guid());

                icons.Add(new Icon(pos1) { Shapes = new List<Shape> { shape } });
            }

            /*
            PositionDB pos2 = new PositionDB(new Vector4(0, -0, 0, 0), new Guid());
            var shape2 = new Shape() { Color = new SDL.SDL_Color() { r = 255, g = 0, b = 0, a = 255 }, Points = CreatePrimitiveShapes.RoundedCylinder(50, 100, 0, 0) };
            var shapes2 = new List<Shape>() { shape2 };

            icons.Add(new Icon(pos2) { Shapes = shapes2 });
*/

            PositionDB pos3 = new PositionDB(new Vector4(100, 0, 0, 0), new Guid());
            icons.Add(new ShipIcon(pos3));

        }
    }
}
