using Pulsar4X.Orbital;
using SDL3;
using System.Collections.Generic;

namespace Pulsar4X.Client
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
            SDL.Color lcolor = new SDL.Color() { R = 0, G = 255, B = 0, A = 255 };
            shapes.Add( new Shape() { Points = lpoints1, Color = lcolor });
            shapes.Add( new Shape() { Points = lpoints2, Color = lcolor });
            icons.Add(new Icon(new StaticPosition(Vector3.Zero)) { Shapes = shapes });

            for (int i = 0; i < 4; i++)
            {
                Vector2[] points = CreatePrimitiveShapes.CreateArc(50 + 50 * i, 400, 100, 100, 0, 4.71, 160);
                SDL.Color color = new SDL.Color() { R = (byte)(i * 60), G = 100, B = 100, A = 255 };
                Shape shape = new Shape() { Points = points, Color = color };
                icons.Add(new Icon(new StaticPosition(Vector3.Zero)) { Shapes = new List<Shape> { shape } });
            }

            icons.Add(new ShipIcon(Vector3.UnitX * 100));

        }
    }
}
