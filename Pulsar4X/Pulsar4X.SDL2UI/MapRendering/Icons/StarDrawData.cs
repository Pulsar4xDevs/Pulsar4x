using System;
using Pulsar4X.ECSLib;
using SDL2;
using System.Collections.Generic;

namespace Pulsar4X.SDL2UI
{
    class StarDrawData : Icon
    {
        double _tempK;
        SDL.SDL_Color _color;

        public StarDrawData(Entity entity): base(entity.GetDataBlob<PositionDB>())
        {
            StarInfoDB starInfo = entity.GetDataBlob<StarInfoDB>();
            _tempK = starInfo.Temperature + 273.15;

            double calcTemp = GMath.Clamp(_tempK, 1000, 40000);
            calcTemp = calcTemp / 100;

            //Red
            if (calcTemp <= 660)
                _color.r = 255;
            else
            {
                _color.r = (byte)(329.698727446 * Math.Pow(calcTemp - 60, -0.1332047592));
            }

            //Green
            if (calcTemp <= 66)
            {
                _color.g = (byte)(99.4708025861 * Math.Log(calcTemp) - 161.1195681661);
            }
            else
            {
                _color.g = (byte)(288.1221695283 * Math.Pow(calcTemp - 60, -0.0755148492));
            }

            //Blue
            if (calcTemp >= 66)
                _color.b = 255;
            else if (calcTemp <= 19)
                _color.b = 0;
            else
            {
                _color.b = (byte)(138.5177312231 * Math.Log(calcTemp - 10) - 305.0447927307);
            }
            _color.a = 255;


            byte spikes = (byte)(starInfo.SpectralType + 4);
            byte spikeDepth = 8;
            double arc = (2 * Math.PI) / spikes;
            double startAngle = 1.5708 - arc / 2;
            List<SDL.SDL_Point> shapePoints = new List<SDL.SDL_Point>();
            for (int i = 0; i < spikes; i++)
            {
                //need rotation transform to rotate it at i * arc; 

                shapePoints.AddRange(CreatePrimitiveShapes.CreateArc(32, 0, 32 - spikeDepth, 32 + spikeDepth, startAngle, arc, 32)); //32 segments is probilby way overkill maybe adjust this by the camera zoom level?
                startAngle += arc;
            }

            List<Shape> shapes = new List<Shape>();
            shapes.Add(new Shape() { Color = _color, Points = shapePoints.ToArray() });
            Shapes = shapes.ToArray();

        }
    }
}
