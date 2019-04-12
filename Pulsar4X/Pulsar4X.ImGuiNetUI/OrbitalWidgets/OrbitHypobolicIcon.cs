using System;
using Pulsar4X.ECSLib;
using SDL2;
using System.Linq;

namespace Pulsar4X.SDL2UI
{
    public class OrbitHypobolicIcon : OrbitIconBase
    {
        public OrbitHypobolicIcon(EntityState entityState, UserOrbitSettings settings) : base(entityState, settings)
        {
            UpdateUserSettings();
            CreatePointArray();
            OnPhysicsUpdate();
        }

        protected override void CreatePointArray()
        {
            double soi = OrbitProcessor.GetSOI(_orbitDB.OwningEntity);
            double e = _orbitDB.Eccentricity;
            double p = EllipseMath.SemiLatusRectum(_orbitEllipseSemiMaj, e);
            double angleToSOIPoint = Math.Abs(OrbitMath.AngleAtRadus(soi, p, _orbitDB.Eccentricity));

            double arc = angleToSOIPoint * 2;
            int numberOfPoints = (int)(_numberOfArcSegments / arc) + 1;
            _points = new PointD[numberOfPoints];
            double angle = angleToSOIPoint;

            for (int i = 0; i < numberOfPoints; i++)
            {

                //double x1 = _orbitEllipseSemiMaj * Math.Sin(angle) - _linearEccentricity; //we add the focal distance so the focal point is "center"
                //double y1 = _orbitEllipseSemiMinor * Math.Cos(angle);
                double r = p / (1 + e * Math.Cos(angle));
                double x1 = r * Math.Sin(angle); // - _linearEccentricity; we don't do this here because we're doing the angles from the focal point. 
                double y1 = r * Math.Cos(angle);


                //rotates the points to allow for the LongditudeOfPeriapsis. 
                double x2 = (x1 * Math.Cos(_orbitAngleRadians)) - (y1 * Math.Sin(_orbitAngleRadians));
                double y2 = (x1 * Math.Sin(_orbitAngleRadians)) + (y1 * Math.Cos(_orbitAngleRadians));
                angle += _segmentArcSweepRadians;
                _points[i] = new PointD() { X = x2, Y = y2 };
            }
        }

        public override void OnFrameUpdate(Matrix matrix, Camera camera)
        {
            ViewScreenPos = matrix.Transform(WorldPosition.X, WorldPosition.Y);//sets the zoom position. 

            var camerapoint = camera.CameraViewCoordinate();

            var vsp = new PointD
            {
                X = ViewScreenPos.x + camerapoint.x,
                Y = ViewScreenPos.y + camerapoint.y
            };


            _drawPoints = new SDL.SDL_Point[_numberOfDrawSegments];

            for (int i = 0; i < _numberOfDrawSegments; i++)
            {

                PointD translated = matrix.TransformD(_points[i].X, _points[i].Y); //add zoom transformation. 

                //translate everything to viewscreen & camera positions
                //int x = (int)(ViewScreenPos.x + translated.X + camerapoint.x);
                //int y = (int)(ViewScreenPos.y + translated.Y + camerapoint.y);
                int x = (int)(vsp.X + translated.X);
                int y = (int)(vsp.Y + translated.Y);

                _drawPoints[i] = new SDL.SDL_Point() { x = x, y = y };
            }

        }

        public override void Draw(IntPtr rendererPtr, Camera camera)
        {
            //now we draw a line between each of the points in the translatedPoints[] array.
            if (_drawPoints.Count() < _numberOfDrawSegments - 1)
                return;
            float alpha = _userSettings.MaxAlpha;
            for (int i = 0; i < _numberOfDrawSegments - 1; i++)
            {
                SDL.SDL_SetRenderDrawColor(rendererPtr, _userSettings.Red, _userSettings.Grn, _userSettings.Blu, (byte)alpha);//we cast the alpha here to stop rounding errors creaping up. 
                SDL.SDL_RenderDrawLine(rendererPtr, _drawPoints[i].x, _drawPoints[i].y, _drawPoints[i + 1].x, _drawPoints[i + 1].y);
                alpha -= _alphaChangeAmount;
            }
        }
    }
}
