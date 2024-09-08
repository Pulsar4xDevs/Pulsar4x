using System;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;
using Pulsar4X.Orbital;
using Pulsar4X.Extensions;
using SDL2;
using System.Linq;
using System.Collections.Generic;
using Pulsar4X.Interfaces;

namespace Pulsar4X.SDL2UI
{
    public class OrbitHypobolicIcon : Icon, IUpdateUserSettings, IKepler
    {
        protected EntityManager _mgr;
        private KeplerElements _ke;
        NewtonSimDB _newtonSimDB;
        private OrbitDB _orbitDB;
        PositionDB _parentPosDB;
        PositionDB _myPosDB;
        double _sgp;
        private double _soi;
        int _index = 0;
        int _numberOfPoints;
        //internal float a;
        //protected float b;
        protected Vector2[] _points; //we calculate points around the ellipse and add them here. when we draw them we translate all the points. 
        protected SDL.SDL_Point[] _drawPoints = new SDL.SDL_Point[0];
        Vector2[] _debugPoints;
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

        IPosition IKepler.PositionDB => _myPosDB;

        IPosition IKepler.ParentPosDB => _parentPosDB;

        double IKepler.SemiMaj => _ke.SemiMajorAxis;

        double IKepler.SemiMin => _ke.SemiMinorAxis;

        double IKepler.LoP_radians => OrbitMath.GetLongditudeOfPeriapsis(_ke.Inclination, _ke.AoP, _ke.LoAN);

        double IKepler.Eccentricity => _ke.Eccentricity;

        double IKepler.LinearEccent => _ke.LinearEccentricity;
        
        public OrbitHypobolicIcon(EntityState entityState, List<List<UserOrbitSettings>> settings) : base(entityState.Entity.GetSOIParentPositionDB())
        {
            entityState.OrbitIcon = this;
            BodyType = entityState.BodyType;
            TrajectoryType = UserOrbitSettings.OrbitTrajectoryType.Hyperbolic;
            _mgr = entityState.Entity.Manager;
            if (entityState.Entity.TryGetDatablob<NewtonSimDB>(out _newtonSimDB))
            {
                _ke = _newtonSimDB.GetElements();
                _soi = _newtonSimDB.SOIParent.GetSOI_m();
            }
            else if (entityState.Entity.TryGetDatablob(out _orbitDB))
            {
                _ke = _orbitDB.GetElements();
                _soi = entityState.Entity.GetSOI_m();
            }
            _parentPosDB = _newtonSimDB.SOIParent.GetDataBlob<PositionDB>();
            _positionDB = _parentPosDB;
            _myPosDB = entityState.Entity.GetDataBlob<PositionDB>();
            _userOrbitSettingsMtx = settings;
            var parentMass = entityState.Entity.GetDataBlob<NewtonSimDB>().ParentMass;
            var myMass = entityState.Entity.GetDataBlob<MassVolumeDB>().MassDry;
            //_sgp = UniversalConstants.Science.GravitationalConstant * (parentMass + myMass) / 3.347928976e33;

            
            UpdateUserSettings();
            CreatePointArray();
            OnPhysicsUpdate();
        }
        
        /// <summary>
        ///calculate anything that could have changed from the users input. 
        /// </summary>
        public void UpdateUserSettings()
        {
            
            
         
            //if this is the case, we need to rebuild the whole set of points. 
            if (_userSettings.NumberOfArcSegments != _numberOfArcSegments)
            {
                _numberOfArcSegments = _userSettings.NumberOfArcSegments;
                _segmentArcSweepRadians = (float)(Math.PI * 2.0 / _numberOfArcSegments);
                _numberOfDrawSegments = (int)Math.Max(1, (_userSettings.EllipseSweepRadians / _segmentArcSweepRadians));
                _alphaChangeAmount = ((float)_userSettings.MaxAlpha - _userSettings.MinAlpha) / _numberOfDrawSegments;
                _numberOfPoints = _numberOfDrawSegments + 1;
                CreatePointArray();
            }
            _segmentArcSweepRadians = (float)(Math.PI * 2.0 / _numberOfArcSegments);
            _numberOfDrawSegments = (int)Math.Max(1, (_userSettings.EllipseSweepRadians / _segmentArcSweepRadians));
            _alphaChangeAmount = ((float)_userSettings.MaxAlpha - _userSettings.MinAlpha) / _numberOfDrawSegments;
            _numberOfPoints = _numberOfDrawSegments + 1;   
            

        }

        internal void CreatePointArray()
        {
            
            double p = EllipseMath.SemiLatusRectum(_ke.SemiMajorAxis, _ke.Eccentricity);
            double angleToSOIPoint = Math.Abs(EllipseMath.TrueAnomalyAtRadus(_soi, p, _ke.Eccentricity));
            var pos = _myPosDB.RelativePosition;
            Vector2 endPos = new Vector2()
            {
                X = _soi * Math.Cos(angleToSOIPoint),
                Y = _soi * Math.Sin(angleToSOIPoint)
            };
            if (_points is null || _points.Length != _numberOfPoints)
                _points = new Vector2[_numberOfPoints];
            if (_drawPoints.Length != _points.Length)
                _drawPoints = new SDL.SDL_Point[_numberOfPoints];
            CreatePrimitiveShapes.KeplerPoints(_ke, (Vector2)pos, endPos, ref _points);
            
            /*
            _dv = _newtonSimDB.ManuverDeltaVLen;
            Vector3 vel = Distance.MToAU(_newtonSimDB.CurrentVector_ms);
            Vector3 pos = Distance.MToAU(_myPosDB.RelativePosition);
            Vector3 eccentVector = OrbitMath.EccentricityVector(_sgp, pos, vel);
            double e = eccentVector.Length();
            double r = pos.Length();
            double v = vel.Length();
            double a = 1 / (2 / r - Math.Pow(v, 2) / _sgp);    //semiMajor Axis
            double b = -a * Math.Sqrt(Math.Pow(e, 2) - 1);     //semiMinor Axis
            if (double.IsNaN(b)) //eccentricity is close enough to 0 to cause e * e -1 to = -1 (and sqrt(-1) then = i)
            {
                b = a;
                e = 0;
            }

            double linierEccentricity = e * a;


            //longditudeOfPeriapsis;
            double _lop = Math.Atan2(eccentVector.Y, eccentVector.X);
            if (Vector3.Cross(pos, vel).Z < 0) //anti clockwise orbit
                _lop = Math.PI * 2 - _lop;


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

            var points = new Vector2[ctrIndex + 1];
            points[0] = new Vector2() { X = xn, Y = yn };
            for (int i = 1; i < ctrIndex + 1; i++)
            {
                var lastx = xn;
                var lasty = yn;
                xn = fooA * lastx + fooB * lasty;
                yn = fooC * lastx + fooA * lasty;
                points[i] = new Vector2() { X = xn, Y = yn };
            }


            _points = new Vector2[_numberOfPoints];
            _points[ctrIndex] = new Vector2()
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
                _points[ctrIndex + i] = new Vector2()
                {
                    X = x2a,
                    Y = y2a
                };

                _points[ctrIndex - i] = new Vector2()
                {
                    X = x2b,
                    Y = y2b
                };
            }
*/
        }

        public override void OnFrameUpdate(Matrix matrix, Camera camera)
        {
            CreatePointArray(); //remove this, this is for testing only.
            //resize for zoom
            //translate to position
            
            var foo = camera.ViewCoordinate_m(WorldPosition_m);
            var trns = Matrix.IDTranslate(foo.x, foo.y);
            var scAU = Matrix.IDScale(6.6859E-12, 6.6859E-12);
            var scZm = Matrix.IDScale(camera.ZoomLevel, camera.ZoomLevel);
            var mtrx = scAU * scZm *  trns;//scale to au, scale for camera zoom, and move to camera position and zoom

            int index = _index;
            var spos = camera.ViewCoordinateV2_m(_myPosDB.AbsolutePosition);

            //_drawPoints[0] = mtrx.TransformToSDL_Point(_bodyrelativePos.X, _bodyrelativePos.Y);
            //_drawPoints[0] = new SDL.SDL_Point(){x = (int)spos.X, y = (int)spos.Y};
            for (int i = 0; i < _numberOfPoints; i++)
            {

                
                _drawPoints[i] = mtrx.TransformToSDL_Point(_points[index].X, _points[index].Y);
            }

        }

        public override void OnPhysicsUpdate()
        {
/*
            if (_dv != _newtonSimDB.ManuverDeltaVLen)
                CreatePointArray();
            
            
            Vector3 pos = Distance.MToAU(_myPosDB.RelativePosition);
            var relativePos = new Vector2() { X = pos.X, Y = pos.Y };
 
            
            double minDist = (relativePos - _points[_index]).Length();

            for (int i = 0; i < _points.Count(); i++)
            {
                double dist = (relativePos - _points[i]).Length();
                if (dist < minDist)
                {
                    minDist = dist;
                    _index = i;
                }
            }
            */
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
