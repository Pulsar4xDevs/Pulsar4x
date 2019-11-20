using System;
using Pulsar4X.ECSLib;
using SDL2;
using System.Linq;
using System.Collections.Generic;

namespace Pulsar4X.SDL2UI
{
    public class OrbitHypobolicIcon : Icon
    {
        protected EntityManager _mgr;
        NewtonMoveDB _newtonMoveDB;
        PositionDB parentPosDB;
        PositionDB myPosDB;
        double _sgp;
        int _index = 0;
        int _numberOfPoints;
        //internal float a;
        //protected float b;
        protected PointD[] _points; //we calculate points around the ellipse and add them here. when we draw them we translate all the points. 
        protected SDL.SDL_Point[] _drawPoints = new SDL.SDL_Point[0];
        PointD[] _debugPoints;
        SDL.SDL_Point[] _debugDrawPoints = new SDL.SDL_Point[0];

        //user adjustable variables:
        internal UserOrbitSettings.OrbitBodyType BodyType = UserOrbitSettings.OrbitBodyType.Unknown;
        internal UserOrbitSettings.OrbitTrajectoryType TrajectoryType = UserOrbitSettings.OrbitTrajectoryType.Unknown;
        protected List<List<UserOrbitSettings>> _userOrbitSettingsMtx;
        protected UserOrbitSettings _userSettings { get { return _userOrbitSettingsMtx[(int)BodyType][(int)TrajectoryType]; } }

        //change after user makes adjustments:
        protected byte _numberOfArcSegments = 255; //how many segments in a complete 360 degree ellipse. this is set in UserOrbitSettings, localy adjusted because the whole point array needs re-creating when it changes. 
        protected int _numberOfDrawSegments; //this is now many segments get drawn in the ellipse, ie if the _ellipseSweepAngle or _numberOfArcSegments are less, less will be drawn.
        protected float _segmentArcSweepRadians; //how large each segment in the drawn portion of the ellipse.  
        protected float _alphaChangeAmount;

        private double _dv = 0;

        public OrbitHypobolicIcon(EntityState entityState, List<List<UserOrbitSettings>> settings) : base(entityState.Entity.GetDataBlob<NewtonMoveDB>().SOIParent.GetDataBlob<PositionDB>())
        {
            _mgr = entityState.Entity.Manager;
            _newtonMoveDB = entityState.Entity.GetDataBlob<NewtonMoveDB>();
            parentPosDB = _newtonMoveDB.SOIParent.GetDataBlob<PositionDB>();
            _positionDB = parentPosDB;
            myPosDB = entityState.Entity.GetDataBlob<PositionDB>();
            _userOrbitSettingsMtx = settings;
            var parentMass = entityState.Entity.GetDataBlob<NewtonMoveDB>().ParentMass;
            var myMass = entityState.Entity.GetDataBlob<MassVolumeDB>().Mass;
            _sgp = GameConstants.Science.GravitationalConstant * (parentMass + myMass) / 3.347928976e33;


            UpdateUserSettings();
            CreatePointArray();
            OnPhysicsUpdate();
        }
        /// <summary>
        ///calculate anything that could have changed from the users input. 
        /// </summary>
        public void UpdateUserSettings()
        {
            //if this happens, we need to rebuild the whole set of points. 
            if (_userSettings.NumberOfArcSegments != _numberOfArcSegments)
            {
                _numberOfArcSegments = _userSettings.NumberOfArcSegments;
                CreatePointArray();
            }

            _segmentArcSweepRadians = (float)(Math.PI * 2.0 / _numberOfArcSegments);
            _numberOfDrawSegments = (int)Math.Max(1, (_userSettings.EllipseSweepRadians / _segmentArcSweepRadians));
            _alphaChangeAmount = ((float)_userSettings.MaxAlpha - _userSettings.MinAlpha) / _numberOfDrawSegments;
            _numberOfPoints = _numberOfDrawSegments + 1;
        }

        internal void CreatePointArray()
        {
            _dv = _newtonMoveDB.DeltaVForManuver_m.Length();
            Vector3 vel = Distance.MToAU(_newtonMoveDB.CurrentVector_ms);
            Vector3 pos = myPosDB.RelativePosition_AU;
            Vector3 eccentVector = OrbitMath.EccentricityVector(_sgp, pos, vel);
            double e = eccentVector.Length();
            double r = pos.Length();
            double v = vel.Length();
            double a = 1 / (2 / r - Math.Pow(v, 2) / _sgp);    //semiMajor Axis
            double b = -a * Math.Sqrt(Math.Pow(e, 2) - 1);     //semiMinor Axis
            double linierEccentricity = e * a;
            double soi = OrbitProcessor.GetSOI_AU(_newtonMoveDB.SOIParent);

            //longditudeOfPeriapsis;
            double _lop = Math.Atan2(eccentVector.Y, eccentVector.X);
            if (Vector3.Cross(pos, vel).Z < 0) //anti clockwise orbit
                _lop = Math.PI * 2 - _lop;

            double p = EllipseMath.SemiLatusRectum(a, e);
            double angleToSOIPoint = Math.Abs(OrbitMath.AngleAtRadus(soi, p, e));
            //double thetaMax = angleToSOIPoint;

            double maxX = soi * Math.Cos(angleToSOIPoint);
            //maxX = maxX - a + linierEccentricity;
            double foo = maxX / a;
            double thetaMax = Math.Log(foo + Math.Sqrt(foo * foo - 1));


            if (_numberOfPoints % 2 == 0)
                _numberOfPoints += 1;
            int ctrIndex = _numberOfPoints / 2;
            double dtheta = thetaMax / (ctrIndex - 1);
            double fooA = Math.Cosh(dtheta);
            double fooB = (a / b) * Math.Sinh(dtheta);
            double fooC = (b / a) * Math.Sinh(dtheta);
            double xn = a;
            double yn = 0;

            var points = new PointD[ctrIndex + 1];
            points[0] = new PointD() { X = xn, Y = yn };
            for (int i = 1; i < ctrIndex + 1; i++)
            {
                var lastx = xn;
                var lasty = yn;
                xn = fooA * lastx + fooB * lasty;
                yn = fooC * lastx + fooA * lasty;
                points[i] = new PointD() { X = xn, Y = yn };
            }


            _points = new PointD[_numberOfPoints];
            _points[ctrIndex] = new PointD()
            {
                X = ((points[0].X - linierEccentricity )* Math.Cos(_lop)) - (points[0].Y * Math.Sin(_lop)),
                Y = ((points[0].X - linierEccentricity) * Math.Sin(_lop)) + (points[0].Y * Math.Cos(_lop))
            };
            for (int i = 1; i < ctrIndex + 1; i++)
            {
                double x = points[i].X - linierEccentricity; //adjust for the focal point
                double ya = points[i].Y;
                double yb = -points[i].Y;
                double x2a = (x * Math.Cos(_lop)) - (ya * Math.Sin(_lop)); //rotate to loan
                double y2a = (x * Math.Sin(_lop)) + (ya * Math.Cos(_lop));
                double x2b = (x * Math.Cos(_lop)) - (yb * Math.Sin(_lop));
                double y2b = (x * Math.Sin(_lop)) + (yb * Math.Cos(_lop));
                _points[ctrIndex + i] = new PointD()
                {
                    X = x2a,
                    Y = y2a 
                };

                _points[ctrIndex - i] = new PointD()
                {
                    X = x2b,
                    Y = y2b
                };
            }

        }

        public override void OnFrameUpdate(Matrix matrix, Camera camera)
        {
            ViewScreenPos = camera.ViewCoordinate_AU(WorldPosition_AU);

            _drawPoints = new SDL.SDL_Point[_numberOfPoints];

            for (int i = 0; i < _numberOfPoints; i++)
            {

                PointD translated = matrix.TransformD(_points[i].X, _points[i].Y); //add zoom transformation. 

                //translate everything to viewscreen & camera positions
                //int x = (int)(ViewScreenPos.x + translated.X + camerapoint.x);
                //int y = (int)(ViewScreenPos.y + translated.Y + camerapoint.y);
                int x = (int)(ViewScreenPos.x + translated.X);
                int y = (int)(ViewScreenPos.y + translated.Y);

                _drawPoints[i] = new SDL.SDL_Point() { x = x, y = y };
            }

        }

        public override void OnPhysicsUpdate()
        {

            if (_dv != _newtonMoveDB.DeltaVForManuver_m.Length())
                CreatePointArray();
            
            
            Vector3 pos = myPosDB.RelativePosition_AU;
            var ralitivePos = new PointD() { X = pos.X, Y = pos.Y };

            double minDist = PointDFunctions.Length(PointDFunctions.Sub(ralitivePos, _points[_index]));

            for (int i = 0; i < _points.Count(); i++)
            {
                double dist = PointDFunctions.Length(PointDFunctions.Sub(ralitivePos, _points[i]));
                if (dist < minDist)
                {
                    minDist = dist;
                    _index = i;
                }
            }
        }

        public override void Draw(IntPtr rendererPtr, Camera camera)
        {
            //now we draw a line between each of the points in the translatedPoints[] array.
            if (_drawPoints.Count() < _numberOfPoints - 1)
                return;
            float postalpha = _userSettings.MaxAlpha;
            float predAlpha = _userSettings.MaxAlpha / 2;

            for (int i = 0; i < _numberOfPoints - 1; i++)
            {
                float alpha;
                if (i < _index)
                    alpha = postalpha;
                else
                    alpha = predAlpha;
                SDL.SDL_SetRenderDrawColor(rendererPtr, _userSettings.Red, _userSettings.Grn, _userSettings.Blu, (byte)alpha);//we cast the alpha here to stop rounding errors creaping up. 
                SDL.SDL_RenderDrawLine(rendererPtr, _drawPoints[i].x, _drawPoints[i].y, _drawPoints[i + 1].x, _drawPoints[i + 1].y);
                alpha -= _alphaChangeAmount;
            }
        }
    }
}
