using System;
using Pulsar4X.ECSLib;
using SDL2;
using System.Collections.Generic;

namespace Pulsar4X.SDL2UI
{
    class StarIcon : Icon
    {
        double _tempK;
        SDL.SDL_Color _color;
        float _iconMinSize = 16;
        double _bodyRadiusAU;
        public StarIcon(Entity entity): base(entity.GetDataBlob<PositionDB>())
        {
            StarInfoDB starInfo = entity.GetDataBlob<StarInfoDB>();
            _tempK = starInfo.Temperature + 273.15;
            var massVol = entity.GetDataBlob<MassVolumeDB>();
            _bodyRadiusAU = massVol.Radius;

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
            float spikeheight = 1;
            float spikeDepth = 0.5f;
            double arc = (2 * Math.PI) / spikes;
            double startAngle = 1.5708 - arc / 2;
            List<PointD> shapePoints = new List<PointD>();
            for (int i = 0; i < spikes; i++)
            {
                var a1 = arc * i;
                double x1 = (0 * Math.Cos(a1) - spikeheight * Math.Sin(a1));
                double y1 = (0 * Math.Sin(a1) + spikeheight * Math.Cos(a1));
                var p1 = new PointD() { X = x1, Y = y1 };

                var a2 = a1 + arc * 0.5;
                double x2 = (0 * Math.Cos(a2) - spikeDepth * Math.Sin(a2));
                double y2 = (0 * Math.Sin(a2) + spikeDepth * Math.Cos(a2));
                var p2 = new PointD() { X = x2, Y = y2 };

                shapePoints.Add(p1);
                shapePoints.Add(p2);

                /*
                 * this was an attempt at making slightly nicer looking stars using an elipsed curve instead of just straight lines. couldnt get it working though
                 * the idea was make an arc, then rotate it.
                List<SDL.SDL_Point> points = new List<SDL.SDL_Point>();
                points.AddRange(CreatePrimitiveShapes.CreateArc(32, 0, 32 - spikeDepth, 32 + spikeDepth, startAngle, arc, 32)); //32 segments is probilby way overkill maybe adjust this by the camera zoom level?
                //rotate it at i * arc; 
                var a = arc * i;
                for (int i2 = 0; i2 < points.Count; i2++)
                {
                    int x = (int)(points[i2].x * Math.Cos(a) - points[i2].y * Math.Sin(a));
                    int y = (int)(points[i2].x * Math.Sin(a) + points[i2].y * Math.Cos(a));
                    points[i2] = new SDL.SDL_Point() { x = x, y = y };
                }
                shapePoints.AddRange(points);
                startAngle += arc;
                */
            }
            shapePoints.Add(shapePoints[0]); //ensure the last point is the same as the first, so it joins up. 
            List<Shape> shapes = new List<Shape>();
            shapes.Add(new Shape() { Color = _color, Points = shapePoints.ToArray() });
            Shapes.AddRange(shapes);
        }

        public override void OnFrameUpdate(Matrix matrix, Camera camera)
        {
            var viewRadius = camera.ViewDistance(_bodyRadiusAU);
            if (viewRadius < _iconMinSize)
                Scale = _iconMinSize;
            else
                Scale = viewRadius;
            base.OnFrameUpdate(matrix, camera);
            base.OnFrameUpdate(matrix, camera);
        }
    }
}
