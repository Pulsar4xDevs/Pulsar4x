using System;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;
using Pulsar4X.Orbital;
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
            Vector2[] lpoints1 = new Vector2[] {
                new Vector2 { X = 0, Y = -160 },
                new Vector2 { X = 0, Y = 160 },
            };
            Vector2[] lpoints2 = new Vector2[] {
                new Vector2 { X = -25, Y = 0 },
                new Vector2 { X = 25, Y = 0 }
            };
            SDL.SDL_Color lcolor = new SDL.SDL_Color() { r = 0, g = 255, b = 0, a = 255 };
            shapes.Add( new Shape() { Points = lpoints1, Color = lcolor });
            shapes.Add( new Shape() { Points = lpoints2, Color = lcolor });
            PositionDB lpos = new PositionDB(Vector3.Zero);

            icons.Add(new Icon(lpos) { Shapes = shapes });

            for (int i = 0; i < 4; i++)
            {
                Vector2[] points = CreatePrimitiveShapes.CreateArc(50 + 50 * i, 400, 100, 100, 0, 4.71, 160);
                SDL.SDL_Color color = new SDL.SDL_Color() { r = (byte)(i * 60), g = 100, b = 100, a = 255 };
                Shape shape = new Shape() { Points = points, Color = color };
                PositionDB pos1 = new PositionDB(Vector3.Zero);

                icons.Add(new Icon(pos1) { Shapes = new List<Shape> { shape } });
            }

            /*
            PositionDB pos2 = new PositionDB(new Vector4(0, -0, 0, 0), new ID());
            var shape2 = new Shape() { Color = new SDL.SDL_Color() { r = 255, g = 0, b = 0, a = 255 }, Points = CreatePrimitiveShapes.RoundedCylinder(50, 100, 0, 0) };
            var shapes2 = new List<Shape>() { shape2 };

            icons.Add(new Icon(pos2) { Shapes = shapes2 });
*/

            PositionDB pos3 = new PositionDB(Vector3.UnitX*100);
            icons.Add(new ShipIcon(pos3));

        }
    }
}
