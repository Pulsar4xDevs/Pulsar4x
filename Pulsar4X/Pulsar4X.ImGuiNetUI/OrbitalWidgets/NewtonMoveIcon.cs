using System;
using System.Collections.Generic;
using Pulsar4X.ECSLib;
using Pulsar4X.Orbital;
using SDL2;

namespace Pulsar4X.SDL2UI
{
    
    /// <summary>
    /// The key parts of this are taken from the paper
    /// "Drawing ellipses, hyperbolas or parabolas with a
    ///  fixed number of points and maximum inscribed area"
    /// by L.R. Smith
    /// </summary>
    public class NewtonMoveIcon : Icon
    {
        protected EntityManager _mgr;
        NewtonMoveDB _newtonMoveDB;
        PositionDB parentPosDB;
        PositionDB myPosDB;
        double _sgp;
        private double _sgpAU;
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
        private KeplerElements _ke;
        
        
        
        public NewtonMoveIcon(EntityState entityState, List<List<UserOrbitSettings>> settings) : base(entityState.Entity.GetDataBlob<NewtonMoveDB>().SOIParent.GetDataBlob<PositionDB>())
        {
            BodyType = entityState.BodyType;
            TrajectoryType = UserOrbitSettings.OrbitTrajectoryType.Hyperbolic;
            _mgr = entityState.Entity.Manager;
            _newtonMoveDB = entityState.Entity.GetDataBlob<NewtonMoveDB>();
            parentPosDB = _newtonMoveDB.SOIParent.GetDataBlob<PositionDB>();
            _positionDB = parentPosDB;
            myPosDB = entityState.Entity.GetDataBlob<PositionDB>();
            _userOrbitSettingsMtx = settings;
            var parentMass = entityState.Entity.GetDataBlob<NewtonMoveDB>().ParentMass;
            var myMass = entityState.Entity.GetDataBlob<MassVolumeDB>().MassDry;
            var _sgp1 = UniversalConstants.Science.GravitationalConstant * (parentMass + myMass) / 3.347928976e33;

            _sgp = OrbitMath.CalculateStandardGravityParameterInM3S2(myMass, parentMass);
            _sgpAU = GeneralMath.GravitiationalParameter_Au3s2(parentMass + myMass);
            _ke = _newtonMoveDB.GetElements();
            
            
            UpdateUserSettings();
            //CreatePointArray();
            OnPhysicsUpdate();
        }
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


        public override void OnPhysicsUpdate()
        {
            if (_newtonMoveDB.OwningEntity == null)
                return;
            var ke = _newtonMoveDB.GetElements();
            if (ke.Eccentricity != _ke.Eccentricity)
            {
                _ke = ke;
                CreatePointArray();
            }
        }

        internal void CreatePointArray()
        {
            if(_ke.Eccentricity < 1)
            {
                TrajectoryType = UserOrbitSettings.OrbitTrajectoryType.Elliptical;
                CreateEllipsePoints();
            }
            else
            {
                TrajectoryType = UserOrbitSettings.OrbitTrajectoryType.Hyperbolic;
                CreateHyperbolicPoints();
            }
        }

        private void CreateHyperbolicPoints()
        {
            Vector3 vel = _newtonMoveDB.CurrentVector_ms;
            Vector3 pos = myPosDB.RelativePosition_m;
            //Vector3 eccentVector = OrbitMath.EccentricityVector(_sgp, pos, vel);

            
            double e = _ke.Eccentricity; 
            double r = pos.Length();
            double v = vel.Length();
            double a = _ke.SemiMajorAxis;    //semiMajor Axis
            double b = _ke.SemiMinorAxis;     //semiMinor Axis

            double a1 = 1 / (2 / r - Math.Pow(v, 2) / _sgp);    //semiMajor Axis
            double b1 = -a * Math.Sqrt(Math.Pow(e, 2) - 1);     //semiMinor Axis
           Vector3 eccentVector = OrbitMath.EccentricityVector(_sgp, pos, vel);
           double e1 = eccentVector.Length();         

            double linierEccentricity = e * a;
            double soi = OrbitProcessor.GetSOI_m(_newtonMoveDB.SOIParent);

            //longditudeOfPeriapsis;
            double _lop = _ke.AoP + _ke.LoAN;

            double p = EllipseMath.SemiLatusRectum(a, e);
            double angleToSOIPoint = Math.Abs(OrbitMath.AngleAtRadus(soi, p, e));
            double p1 = EllipseMath.SemiLatusRectum(a1, e1);
            double ang1 = Math.Abs(OrbitalMath.AngleAtRadus(soi, p1, e1));
            
            //the angle used in the calculation is not from the focal point, but from the center( x=a y=0)
            //angleToSOIPoint however is from the focal point so we need to translate from that frame of reference to one from the center. 

            double y = Math.Abs(Math.Sin(angleToSOIPoint) * soi);
            double x = Math.Abs(Math.Cos(angleToSOIPoint) * soi);
            double x2 = linierEccentricity + x;
            double thetaMax = Math.Atan2(y, x2);
            
            //make it an odd number of points
            if (_numberOfPoints % 2 == 0)
                _numberOfPoints += 1;
            int ctrIndex = _numberOfPoints / 2;
            
            double dtheta = thetaMax / (ctrIndex - 1);
            double fooA = Math.Cosh(dtheta);
            double fooB = (a / b) * Math.Sinh(dtheta);
            double fooC = (b / a) * Math.Sinh(dtheta);
            double xn = a;
            double yn = 0;

            //first create a list of points for one side of the raw hyperbol
            var points = new PointD[ctrIndex + 1];
            points[0] = new PointD() { X = -xn, Y = yn }; //periapsis
            for (int i = 1; i < ctrIndex + 1; i++)
            {
                var lastx = xn;
                var lasty = yn;
                xn = fooA * lastx + fooB * lasty;
                yn = fooC * lastx + fooA * lasty;
                points[i] = new PointD() { X = -xn, Y = yn };
            }


            //now translate and rotate it into place, and mirror. 
            var mtxtr = Matrix.IDTranslate(linierEccentricity, 0);
            var mtxrt = Matrix.IDRotate(_lop);
            var mtx = mtxtr * mtxrt;
            var mtxmr =  Matrix.IDMirror(true, false) * mtx;
            
            
            _points = new PointD[_numberOfPoints];
            _points[ctrIndex] = mtx.TransformD(points[0]); //periapsis
            
            
            int j = ctrIndex + 1;
            int k = ctrIndex - 1;
            for (int i = 1; i < ctrIndex + 1; i++)
            {
                _points[j] = mtx.TransformD(points[i]);
                _points[k] = mtxmr.TransformD(points[i]);
                j++;
                k--;
            }

        }

        private void CreateEllipsePoints()
        {

            double a = _ke.SemiMajorAxis;
            double b = _ke.SemiMinorAxis;
            double linierEccentricity = _ke.Eccentricity * a;
            double _lop = _ke.AoP + _ke.LoAN;            
            
            double dTheta = 2 * Math.PI / (_numberOfPoints - 1);
            
            double ct = Math.Cos(_lop);
            double st = Math.Sin(_lop);
            double cdp = Math.Cos(dTheta);
            double sdp = Math.Sin(dTheta);
            double fooA = cdp + sdp * st * ct * (a / b - b / a);
            double fooB = -sdp * ((b * st) * (b * st) + ((a * ct) * (a * ct))) / (a * b);
            double fooC = sdp * ((b * st) * (b * st) + ((a * ct) * (a * ct))) / (a * b);
            double fooD = cdp + sdp * st * ct * (b / a - a / b);
            fooD -= (fooC * fooB) / fooA;
            fooC = fooC / fooA;

            double x = a * ct;
            double y = a * st;
            
            //we want the focal point of the ellipse to be at the 'center'  
            //linier ecccentricity is the offset ie the distance between focal and center.
            //we have to rotate the offset since we're already rotating the ellipse above.
            double xc = Math.Cos(_lop) * -linierEccentricity;
            double yc = Math.Sin(_lop) * -linierEccentricity;
            
            _points = new PointD[_numberOfPoints];

            for (int i = 0; i < _numberOfPoints; i++)
            {
                _points[i] = new PointD()
                {
                    X = xc + x,
                    Y = yc + y,
                };
                x = fooA * x + fooB * y;
                y = fooC * x + fooD * y;
            }
            

        }
        
        public override void OnFrameUpdate(Matrix matrix, Camera camera)
        {

            
            //resize from m to au because zoom is au
            //resize for zoom
            //translate to position
            var foo = camera.ViewCoordinate_m(WorldPosition_m);
            var trns = Matrix.IDTranslate(foo.x, foo.y);
            var scAU = Matrix.IDScale(6.6859E-12, 6.6859E-12);
            var scZm = Matrix.IDScale(camera.ZoomLevel, camera.ZoomLevel);
            var mtrx = scAU * scZm *  trns;
            _drawPoints = new SDL.SDL_Point[_numberOfDrawSegments];
            for (int i = 0; i < _numberOfDrawSegments; i++)
            {
                _drawPoints[i] = mtrx.Transform(_points[i].X, _points[i].Y);
            }
        }
        
        public override void Draw(IntPtr rendererPtr, Camera camera)
        {
            //now we draw a line between each of the points in the translatedPoints[] array.
            if (_drawPoints.Length < _numberOfDrawSegments - 1)
                return;
            float alpha = _userSettings.MaxAlpha;
            for (int i = 0; i < _numberOfDrawSegments - 1; i++)
            {
                SDL.SDL_SetRenderDrawColor(rendererPtr, _userSettings.Red, _userSettings.Grn, _userSettings.Blu, (byte)alpha);//we cast the alpha here to stop rounding errors creeping up. 
                SDL.SDL_RenderDrawLine(rendererPtr, _drawPoints[i].x, _drawPoints[i].y, _drawPoints[i + 1].x, _drawPoints[i +1].y);
                alpha -= _alphaChangeAmount; 
            }
        }


    }
}