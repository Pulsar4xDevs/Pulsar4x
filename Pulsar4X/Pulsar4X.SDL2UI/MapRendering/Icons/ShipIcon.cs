using System;
using System.Collections.Generic;
using Pulsar4X.ECSLib;
using SDL2;

namespace Pulsar4X.SDL2UI
{
    public class ShipIcon : Icon
    {
        ShipInfoDB _shipInfo;
        ComponentInstancesDB _componentInstances;

        public ShipIcon(Entity entity) : base(entity.GetDataBlob<PositionDB>())
        {
            _shipInfo = entity.GetDataBlob<ShipInfoDB>();
            _componentInstances = entity.GetDataBlob<ComponentInstancesDB>();


            BasicShape();
        }

        public ShipIcon(PositionDB position) : base(position)
        {
            Front(60, 100, 0, -110);
            Cargo(160, 160, 0, -120);
            Wings(260, 260, 80, 50, 0, 0);
            Reactors(100, 100, 0, 90);
            Engines(100, 60, 0, 130);
        }

        void BasicShape()
        {
            //TODO break the vertical up depending on percentage of ship dedicated to each thing. 
            //Front(6, 10, 0, -11); 
            //Cargo(16, 16, 0, -12);
            //Wings(26, 26, 8, 5, 0, 0);
            //Reactors(10, 10, 0, 9);
            //Engines(10, 6, 0, 13);

            //For now we're just going to use a simple cheveron to represent ships, make something fancier in the future 
            //by somone who has some design mojo. 
            byte r = 50;
            byte g = 50;
            byte b = 200;
            byte a = 255;
            PointD[] points = {
            new PointD { X = 0, Y = -5 },
            new PointD { X = 5, Y = 5 },
            new PointD { X = 0, Y = 0 },
            new PointD { X = -5, Y = 5 },
            new PointD { X = 0, Y = -5 }
            };

            SDL.SDL_Color colour = new SDL.SDL_Color() { r = r, g = g, b = b, a = a };
            Shapes.Add(new Shape() { Points = points, Color = colour });

        }
        void Front(int width, int height, int offsetX, int offsetY) //crew 
        {
 
            var points = CreatePrimitiveShapes.CreateArc(offsetX, offsetY, width * 0.5 , height * 0.5, CreatePrimitiveShapes.QuarterCircle, CreatePrimitiveShapes.HalfCircle, 16);
            byte r = 0;
            byte g = 100;
            byte b = 100;
            byte a = 255;
            SDL.SDL_Color colour = new SDL.SDL_Color() { r = r, g = g, b = b, a = a };
            Shapes.Add(new Shape() { Points = points, Color = colour });

        }

        void Cargo(int width, int height, int offsetX, int offsetY)//and fuel
        {
            byte r = 0;
            byte g = 0;
            byte b = 200;
            byte a = 255;
            SDL.SDL_Color colour = new SDL.SDL_Color() { r = r, g = g, b = b, a = a };

            //TODO: change numbers depending on number of cargo containing components.
            int numberofPodsX = 4;  
            int numberofPodsY = 2;

            int podWidth = width / numberofPodsX;
            int offsetx1 = (int)(offsetX - width * 0.5f + podWidth * 0.5);

            int podHeight = height / numberofPodsY;
            int offsety1 = (int)(offsetY + podHeight * 0.5);

            for (int podset = 0; podset < numberofPodsY; podset++)
            {
                offsety1 += podset * podHeight;

                int offsetx2 = offsetx1 - podWidth;

                for (int i = 0; i < numberofPodsX; i++)
                {
                    offsetx2 += podWidth;
                    Shape shape = new Shape() { Color = colour, Points = CreatePrimitiveShapes.RoundedCylinder(podWidth, height / numberofPodsY, offsetx2, offsety1) };
                    Shapes.Add(shape);
                }
            }
        }

        void Wings(int width, int height, int frontWidth, int backWidth, int offsetX, int offsetY)//FTL & guns
        { 
            byte r = 84;
            byte g = 84;
            byte b = 84;
            byte a = 255;
            SDL.SDL_Color colour = new SDL.SDL_Color() { r = r, g = g, b = b, a = a };


            PointD p0 = new PointD() { X = offsetX, Y = (int)(offsetY - height * 0.5) };
            PointD p1 = new PointD() { X = offsetX + frontWidth, Y = (int)(offsetY - height * 0.5) };
            PointD p2 = new PointD() { X = (int)(offsetX + width * 0.5), Y = (int)(offsetY - height * 0.3) };
            PointD p3 = new PointD() { X = (int)(offsetX + width * 0.5), Y = -(int)(offsetY - height * 0.25) };
            PointD p4 = new PointD() { X = offsetX + backWidth, Y = -(int)(offsetY - height * 0.5) };
            PointD p5 = new PointD() { X = offsetX, Y = -(int)(offsetY - height * 0.5) };
            PointD p6 = new PointD() { X = offsetX - backWidth, Y = (int)(offsetY + height * 0.5) };
            PointD p7 = new PointD() { X = (int)(offsetX + -width * 0.5), Y = (int)(offsetY + height * 0.25) };
            PointD p8 = new PointD() { X = (int)(offsetX + -width * 0.5), Y = -(int)(offsetY + height * 0.3) };
            PointD p9 = new PointD() { X = offsetX - frontWidth, Y = -(int)(offsetY + height * 0.5) };
            PointD p10 = new PointD() { X = offsetX, Y = -(int)(offsetY + height * 0.5) };
            var shape = new Shape() { Color = colour, Points = new PointD[] { p0, p1, p2, p3, p4, p5, p6, p7, p8, p9, p10 } };
            Shapes.Add(shape);
                

        }
        void Reactors(int width, int height, int offsetX, int offsetY)
        {   
            byte r = 100;
            byte g = 0;
            byte b = 0;
            byte a = 255;
            SDL.SDL_Color colour = new SDL.SDL_Color() { r = r, g = g, b = b, a = a };

            var shape = new Shape() { Color = colour, Points = CreatePrimitiveShapes.CreateArc(offsetX, offsetY, (int)(width * 0.5), (int)(height * 0.5), 0, CreatePrimitiveShapes.PI2, 12) };

            Shapes.Add(shape);

        }
        void Engines(int width, int height, int offsetX, int offsetY)
        {
            byte r1 = 200;
            byte g1 = 200;
            byte b1 = 200;
            byte a1 = 255;
            SDL.SDL_Color colourbox = new SDL.SDL_Color() { r = r1, g = g1, b = b1, a = a1 };

            byte r2 = 100;
            byte g2 = 150;
            byte b2 = 0;
            byte a2 = 255;
            SDL.SDL_Color colourCone = new SDL.SDL_Color() { r = r2, g = g2, b = b2, a = a2 };

            int thrusterCount = 3;
            int twidth = width / thrusterCount;
            int toffset = (int)(offsetX - width * 0.5f + twidth * 0.5);

            for (int i = 0; i < thrusterCount; i++)
            {
                int boxHeight = height / 3;
                int boxWidth = twidth;
                int coneHeight = height - boxHeight;
                Shapes.Add(new Shape() { Color = colourbox, Points = CreatePrimitiveShapes.Rectangle(toffset, (int)(offsetY + boxHeight * 0.5), boxWidth, boxHeight, CreatePrimitiveShapes.PosFrom.Center) });
                Shapes.Add(new Shape() { Color = colourCone, Points = CreatePrimitiveShapes.CreateArc(toffset, offsetY + boxHeight + coneHeight, (int)(boxWidth * 0.5), coneHeight, CreatePrimitiveShapes.QuarterCircle, CreatePrimitiveShapes.HalfCircle, 8) });
                toffset += twidth;
            }
        }
    }
}
