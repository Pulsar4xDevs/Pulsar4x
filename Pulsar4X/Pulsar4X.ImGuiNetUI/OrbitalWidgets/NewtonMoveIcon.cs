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
    public class NewtonMoveIcon : Icon, IUpdateUserSettings, IKepler
    {
        //protected EntityManager _mgr;
        NewtonMoveDB _newtonMoveDB;
        PositionDB _parentPosDB;
        PositionDB _myPosDB;
        double _sgp;
        //_taIndex is the point closest to the orbiting object, it's used to 
        int _taIndex;
        //_numberOfEllipsePoints is the total number of points around the ellipse, unadjusted for the percentage of the ellipse actualy drawn.
        private int _numberOfEllipsePoints;
        //_numberOfPoints is the number of points drawn in the ellipse. (UserOrbitSettings)
        int _numberOfDrawnPoints;
        //internal float a;
        //protected float b;
        //Points is the world coordinate points of an ellipse or hyperbola.
        //eccentricity, focal offset, and longitude of the periapsis are calculated when populating this array.
        protected Orbital.Vector2[] _points; 
        //_drawpoints is the translated resized screen/pixel location of the above ellipse points. 
        //the above Points are adjusted for camera position and zoom levels when populating this array, as these values can change between frames.
        //[0] is the position of the orbiting object and subsequent positions trail behind the velocity and drawn with decreasing alpha. 
        protected SDL.SDL_Point[] _drawPoints = new SDL.SDL_Point[0];
        
        //for drawing the direction of thrust when newton thrusting (world coordinates)
        private Orbital.Vector2[] _thrustLinePoints = new Vector2[2];
        //above adjusted for camera position and zoom. 
        protected SDL.SDL_Point[] _drawThrustLinePoints = new SDL.SDL_Point[2];
        //PointD[] _debugPoints;
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


        public NewtonMoveIcon(KeplerElements ke, Vector3 position) : base(position)
        {
            
        }

        public NewtonMoveIcon(EntityState entityState, List<List<UserOrbitSettings>> settings) : base(entityState.Entity.GetDataBlob<NewtonMoveDB>().SOIParent.GetDataBlob<PositionDB>())
        {
            entityState.OrbitIcon = this;
            BodyType = entityState.BodyType;
            TrajectoryType = UserOrbitSettings.OrbitTrajectoryType.Hyperbolic;
            //_mgr = entityState.Entity.Manager;
            _newtonMoveDB = entityState.Entity.GetDataBlob<NewtonMoveDB>();
            _parentPosDB = _newtonMoveDB.SOIParent.GetDataBlob<PositionDB>();
            _positionDB = _parentPosDB;
            _myPosDB = entityState.Entity.GetDataBlob<PositionDB>();
            _userOrbitSettingsMtx = settings;
            var parentMass = entityState.Entity.GetDataBlob<NewtonMoveDB>().ParentMass;
            var myMass = entityState.Entity.GetDataBlob<MassVolumeDB>().MassDry;
            var _sgp1 = UniversalConstants.Science.GravitationalConstant * (parentMass + myMass) / 3.347928976e33;

            _sgp = GeneralMath.StandardGravitationalParameter(myMass + parentMass);
            _ke = _newtonMoveDB.GetElements();
            
            
            UpdateUserSettings();
            OnPhysicsUpdate();
        }
        public void UpdateUserSettings()
        {
            
            //if this is the case, we need to rebuild the whole set of points. 
            if (_userSettings.NumberOfArcSegments != _numberOfArcSegments)
            {
                _numberOfArcSegments = _userSettings.NumberOfArcSegments;
                _numberOfEllipsePoints = _numberOfArcSegments;
                _segmentArcSweepRadians = (float)(Math.PI * 2.0 / _numberOfArcSegments);
                _numberOfDrawSegments = (int)Math.Max(1, (_userSettings.EllipseSweepRadians / _segmentArcSweepRadians));
                _alphaChangeAmount = ((float)_userSettings.MaxAlpha - _userSettings.MinAlpha) / _numberOfDrawSegments;
                _numberOfDrawnPoints = _numberOfDrawSegments + 1;//one extra for the object position
                CreatePointArray();
            }
            else
            {
                _segmentArcSweepRadians = (float)(Math.PI * 2.0 / _numberOfArcSegments);
                _numberOfDrawSegments = (int)Math.Max(1, (_userSettings.EllipseSweepRadians / _segmentArcSweepRadians));
                _alphaChangeAmount = ((float)_userSettings.MaxAlpha - _userSettings.MinAlpha) / _numberOfDrawSegments;
                _numberOfDrawnPoints = _numberOfDrawSegments + 1; //one extra for the object position
            }

            _drawPoints = new SDL.SDL_Point[_numberOfDrawnPoints];
        }

        /// <summary>
        /// This is used to find which point in the Points array is closest to the object
        /// we then start drawing from that point and change the alpha
        /// </summary>
        void SetTrueAnomalyIndex()
        {
            Orbital.Vector2 pos = new Vector2(_myPosDB.RelativePosition.X, _myPosDB.RelativePosition.Y);
            double minDist = (pos - _points[_taIndex]).Length();

            for (int i =0; i < _points.Length; i++)
            {
                double dist = (pos - _points[i]).Length();
                if (dist < minDist)
                {
                    minDist = dist;
                    _taIndex = i;
                }
            }
        }
        public override void OnPhysicsUpdate()
        {
            if (_newtonMoveDB.OwningEntity == null) //There's a threaded race condition here which will cause a null...
                return;
            var ke = _newtonMoveDB.GetElements(); //...cause a null ref exception inside this call. 
            if (ke.Eccentricity != _ke.Eccentricity)
            {
                _ke = ke;
                CreatePointArray();
            }
            

            if (_newtonMoveDB.ManuverDeltaVLen > 0)
            {
                var len = 0.1 * _newtonMoveDB.OwningEntity.GetDataBlob<NewtonThrustAbilityDB>().ThrustInNewtons;
                var dv = _newtonMoveDB.ManuverDeltaV;
                var line = Vector3.Normalise(dv) * len ;


                _thrustLinePoints[0] = Vector2.Zero;
                _thrustLinePoints[1] = new Vector2 (line.X, -line.Y);
            }
            else
            {
                _thrustLinePoints[1] = Vector2.Zero;
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
            
            if(_newtonMoveDB.ManuverDeltaV.Length() > 0)
                TrajectoryType = UserOrbitSettings.OrbitTrajectoryType.NewtonionThrust;
            SetTrueAnomalyIndex();
        }

        private void CreateHyperbolicPoints()
        {
            Vector3 vel = _newtonMoveDB.CurrentVector_ms;
            Vector3 pos = _myPosDB.RelativePosition;
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
            double soi = _newtonMoveDB.SOIParent.GetSOI_m();

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
            if (_numberOfDrawnPoints % 2 == 0)
                _numberOfDrawnPoints += 1;
            int ctrIndex = _numberOfDrawnPoints / 2;
            
            double dtheta = thetaMax / (ctrIndex - 1);
            double fooA = Math.Cosh(dtheta);
            double fooB = (a / b) * Math.Sinh(dtheta);
            double fooC = (b / a) * Math.Sinh(dtheta);
            double xn = a;
            double yn = 0;

            //first create a list of points for one side of the raw hyperbol
            var points = new Orbital.Vector2[ctrIndex + 1];
            points[0] = new Orbital.Vector2() { X = -xn, Y = yn }; //periapsis
            for (int i = 1; i < ctrIndex + 1; i++)
            {
                var lastx = xn;
                var lasty = yn;
                xn = fooA * lastx + fooB * lasty;
                yn = fooC * lastx + fooA * lasty;
                points[i] = new Orbital.Vector2() { X = -xn, Y = yn };
            }


            //now translate and rotate it into place, and mirror. 
            var mtxtr = Matrix.IDTranslate(linierEccentricity, 0);
            var mtxrt = Matrix.IDRotate(_lop);
            var mtx = mtxtr * mtxrt;
            var mtxmr =  Matrix.IDMirror(true, false) * mtx;
            
            if(_points is null || _points.Length != _numberOfEllipsePoints)
                _points = new Orbital.Vector2[_numberOfEllipsePoints];
            if (_drawPoints.Length != _numberOfDrawnPoints)
                _drawPoints = new SDL.SDL_Point[_numberOfDrawnPoints];
            
            _points[ctrIndex] = mtx.TransformToVector2(points[0]); //periapsis
            
            
            int j = ctrIndex + 1;
            int k = ctrIndex - 1;
            for (int i = 1; i < ctrIndex + 1; i++)
            {
                _points[j] = mtx.TransformToVector2(points[i]);
                _points[k] = mtxmr.TransformToVector2(points[i]);
                j++;
                k--;
            }

        }

        /// <summary>
        /// Create an array of points for a full ellipse with the correct eccentricity, lop, and focal point offset
        /// ie the focal point should be the "center" or 0,0
        /// </summary>
        private void CreateEllipsePoints()
        {
            if(_points is null || _points.Length != _numberOfEllipsePoints)
                _points = new Orbital.Vector2[_numberOfEllipsePoints];
            if (_drawPoints.Length != _numberOfDrawnPoints)
                _drawPoints = new SDL.SDL_Point[_numberOfDrawnPoints];
            double a = _ke.SemiMajorAxis;
            double b = _ke.SemiMinorAxis;
            double linierEccentricity = _ke.Eccentricity * a;
            double _lop = _ke.AoP + _ke.LoAN;

            double dTheta = Math.PI * 2 / (_numberOfEllipsePoints - 1);
            double ct = Math.Cos(_lop);
            double st = Math.Sin(_lop);
            double cdp = Math.Cos(dTheta);
            double sdp = Math.Sin(dTheta);
            double fooA = cdp + sdp * st * ct * (a / b - b / a);
            double fooB = -sdp * ((b * st) * (b * st) + ((a * ct) * (a * ct))) / (a * b);
            double fooC = sdp * ((b * ct) * (b * ct) + ((a * st) * (a * st))) / (a * b);
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
            
            

            for (int i = 0; i < _numberOfEllipsePoints; i++)
            {
                _points[i] = new Orbital.Vector2()
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
            
            
            int index = _taIndex;
            var spos = camera.ViewCoordinateV2_m(_myPosDB.AbsolutePosition);

            //_drawPoints[0] = mtrx.TransformToSDL_Point(_bodyrelativePos.X, _bodyrelativePos.Y);
            // [0] is the position of the object. 
            _drawPoints[0] = new SDL.SDL_Point(){x = (int)spos.X, y = (int)spos.Y};
            //we should have one less segment than points. 
            //we should have more Points than _drawPoints. (Points is a full ellipse, we normaly only draw an arc)
            for (int i = 1; i < _numberOfDrawSegments; i++) 
            {
                if (index < _numberOfEllipsePoints - 1)

                    index++;
                else
                    index = 0;
                _drawPoints[i] = mtrx.TransformToSDL_Point(_points[index].X, _points[index].Y);
            }

            /*
            for (int i = 0; i < _numberOfDrawSegments; i++)
            {
                _drawPoints[i] = mtrx.TransformToSDL_Point(Points[i].X, Points[i].Y);
            }*/

            var foo2 = camera.ViewCoordinate_m(_myPosDB.AbsolutePosition);
            var trns2 = Matrix.IDTranslate(foo2.x, foo2.y);
            var mtrx2 = scAU * scZm *  trns2;
            for (int i = 0; i < 2; i++)
            {
                _drawThrustLinePoints[i] = mtrx2.TransformToSDL_Point(_thrustLinePoints[i].X, _thrustLinePoints[i].Y);
            }
        }
        
        public override void Draw(IntPtr rendererPtr, Camera camera)
        {
            //now we draw a line between each of the points in the translatedPoints[] array.
            
            if (_drawPoints.Length < _numberOfDrawSegments - 1)
                return;//why was this? maybe prevent a race condition or something?
            float alpha = _userSettings.MaxAlpha;
            for (int i = 0; i < _drawPoints.Length - 1; i++)
            {
                SDL.SDL_SetRenderDrawColor(rendererPtr, _userSettings.Red, _userSettings.Grn, _userSettings.Blu, (byte)alpha);//we cast the alpha here to stop rounding errors creeping up. 
                SDL.SDL_RenderDrawLine(rendererPtr, _drawPoints[i].x, _drawPoints[i].y, _drawPoints[i + 1].x, _drawPoints[i +1].y);
                alpha -= _alphaChangeAmount; 
            }
            byte r = 100;
            byte g = 50;
            byte b = 200;
            byte a = 255;
            
            //now draw the thrust line. 
            SDL.SDL_SetRenderDrawColor(rendererPtr, r, g, b, a);
            SDL.SDL_RenderDrawLine(rendererPtr, _drawThrustLinePoints[0].x, _drawThrustLinePoints[0].y, _drawThrustLinePoints[1].x, _drawThrustLinePoints[1].y);
            
        }


        IPosition IKepler.PositionDB => _myPosDB;

        IPosition IKepler.ParentPosDB => _parentPosDB;

        double IKepler.SemiMaj => _ke.SemiMajorAxis;

        double IKepler.SemiMin => _ke.SemiMinorAxis;

        double IKepler.LoP_radians => OrbitMath.GetLongditudeOfPeriapsis(_ke.Inclination, _ke.AoP, _ke.LoAN);

        double IKepler.Eccentricity => _ke.Eccentricity;

        double IKepler.LinearEccent => _ke.LinearEccentricity;
    }

    /// <summary>
    /// I think this version was just an attempt to re-write another verson that would be more versitile needing less versions to draw the orbit lines?
    /// </summary>
    public class KeIcon : Icon, IUpdateUserSettings
    {

        //double _sgp;
        //private double _sgpAU;
        int _index = 0;
        int _numberOfPoints;
        //internal float a;
        //protected float b;
        protected Orbital.Vector2[] _points; //we calculate points around the ellipse and add them here. when we draw them we translate all the points. 
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
        private KeplerElements _ke;
        private StateVectors _sv;
        private double _parentSOI_m = 0;
        private GlobalUIState _uiState;
        private double _e;
        private int _taIndex;
        
        public KeIcon(KeplerElements ke, StateVectors sv, Vector3 parentPos, double parentSoiM, GlobalUIState uiState) : base(parentPos)
        {
            _ke = ke;
            _sv = sv;
            _e = _ke.Eccentricity;
            _userOrbitSettingsMtx = uiState.UserOrbitSettingsMtx;
            _uiState = uiState;
            _parentSOI_m = parentSoiM;
            TrajectoryType = UserOrbitSettings.OrbitTrajectoryType.Elliptical;
            if (_e > 1)
                TrajectoryType = UserOrbitSettings.OrbitTrajectoryType.Hyperbolic;
            UpdateUserSettings();
            CreatePointArray();
            //DebugShowCenter = true;

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="absoluteParenPos">in meters</param>
        /// <param name="parentSoi"></param>
        public void UpdateParent(Vector3 absoluteParenPos, double parentSoi)
        {
            _worldPosition_m = absoluteParenPos;
            _parentSOI_m = parentSoi;
        }

        public void ForceUpdate(KeplerElements ke, StateVectors sv)
        {
            if(ke.StandardGravParameter == 0)
                throw new Exception("kepler elements are malformed");
            _ke = ke;
            _sv = sv;
            _e = ke.Eccentricity;
            if (_e >= 1)
                TrajectoryType = UserOrbitSettings.OrbitTrajectoryType.Hyperbolic;
            else
                TrajectoryType = UserOrbitSettings.OrbitTrajectoryType.Elliptical;
                
            CreatePointArray();
            SetTrueAnomalyIndex();   
            
        }

        void SetTrueAnomalyIndex()
        {


            Orbital.Vector2 pos = new Vector2(_sv.Position.X, _sv.Position.Y);
            double minDist = (pos - _points[_taIndex]).Length();

            for (int i =0; i < _points.Length; i++)
            {
                double dist = (pos - _points[i]).Length();
                if (dist < minDist)
                {
                    minDist = dist;
                    _taIndex = i;
                }
            }
        }
        

        public override void OnPhysicsUpdate()
        {
            if (_e != _ke.Eccentricity)//check if the eccentricity has changed.
            {
                _e = _ke.Eccentricity;
                if (_e >= 1)
                    TrajectoryType = UserOrbitSettings.OrbitTrajectoryType.Hyperbolic;
                else
                    TrajectoryType = UserOrbitSettings.OrbitTrajectoryType.Elliptical;
                
                CreatePointArray();
            }
        }

        private void CreatePointArray()
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
            
            double e = _ke.Eccentricity; 
            double r = _sv.Position.Length();
            double v = _sv.Velocity.Length();
            double a = _ke.SemiMajorAxis;    //semiMajor Axis
            double b = _ke.SemiMinorAxis;     //semiMinor Axis

            double a1 = 1 / (2 / r - Math.Pow(v, 2) / _ke.StandardGravParameter);    //semiMajor Axis
            double b1 = -a * Math.Sqrt(Math.Pow(e, 2) - 1);     //semiMinor Axis
            Vector3 eccentVector = OrbitMath.EccentricityVector(
                _ke.StandardGravParameter, 
                _sv.Position, 
                new Vector3(_sv.Velocity.X, _sv.Velocity.Y, 0));
            double e1 = eccentVector.Length();         

            double linierEccentricity = e * a;
            

            //longditudeOfPeriapsis;
            double _lop = _ke.AoP + _ke.LoAN;

            double p = EllipseMath.SemiLatusRectum(a, e);
            double angleToSOIPoint = Math.Abs(OrbitMath.AngleAtRadus(_parentSOI_m, p, e));
            double p1 = EllipseMath.SemiLatusRectum(a1, e1);
            double ang1 = Math.Abs(OrbitalMath.AngleAtRadus(_parentSOI_m, p1, e1));
            
            //the angle used in the calculation is not from the focal point, but from the center( x=a y=0)
            //angleToSOIPoint however is from the focal point so we need to translate from that frame of reference to one from the center. 

            double y = Math.Abs(Math.Sin(angleToSOIPoint) * _parentSOI_m);
            double x = Math.Abs(Math.Cos(angleToSOIPoint) * _parentSOI_m);
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
            var points = new Orbital.Vector2[ctrIndex + 1];
            points[0] = new Orbital.Vector2() { X = -xn, Y = yn }; //periapsis
            for (int i = 1; i < ctrIndex + 1; i++)
            {
                var lastx = xn;
                var lasty = yn;
                xn = fooA * lastx + fooB * lasty;
                yn = fooC * lastx + fooA * lasty;
                points[i] = new Orbital.Vector2() { X = -xn, Y = yn };
            }


            //now translate and rotate it into place, and mirror. 
            var mtxtr = Matrix.IDTranslate(linierEccentricity, 0);
            var mtxrt = Matrix.IDRotate(_lop);
            var mtx = mtxtr * mtxrt;
            var mtxmr =  Matrix.IDMirror(true, false) * mtx;
            
            
            _points = new Orbital.Vector2[_numberOfPoints];
            _points[ctrIndex] = mtx.TransformToVector2(points[0]); //periapsis
            
            
            int j = ctrIndex + 1;
            int k = ctrIndex - 1;
            for (int i = 1; i < ctrIndex + 1; i++)
            {
                _points[j] = mtx.TransformToVector2(points[i]);
                _points[k] = mtxmr.TransformToVector2(points[i]);
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

            double dTheta = 2 * Math.PI / _numberOfPoints; // _userSettings.EllipseSweepRadians / (_numberOfPoints - 1);
            
            double ct = Math.Cos(_lop);
            double st = Math.Sin(_lop);
            double cdp = Math.Cos(dTheta);
            double sdp = Math.Sin(dTheta);
            double fooA = cdp + sdp * st * ct * (a / b - b / a);
            double fooB = -sdp * ((b * st) * (b * st) + ((a * ct) * (a * ct))) / (a * b);
            double fooC = sdp * ((b * ct) * (b * ct) + ((a * st) * (a * st))) / (a * b);
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
            
            _points = new Orbital.Vector2[_numberOfPoints];

            for (int i = 0; i < _numberOfPoints; i++)
            {
                _points[i] = new Orbital.Vector2()
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
            
            
            

            ViewScreenPos = camera.ViewCoordinate_m(WorldPosition_m);
            var mir = Matrix.IDMirror(true, false);
            var scAU = Matrix.IDScale(6.6859E-12, 6.6859E-12);
            var scZm = Matrix.IDScale(camera.ZoomLevel, camera.ZoomLevel);
            var trns = Matrix.IDTranslate(ViewScreenPos.x, ViewScreenPos.y);

            var mtrx = mir * scAU * scZm *  trns;
            _drawPoints = new SDL.SDL_Point[_numberOfPoints + 1];

            int shapeCount = Shapes.Count;
            DrawShapes = new Shape[shapeCount];            
            if (DebugShowCenter)
            {
                DrawShapes = new Shape[shapeCount+1];
                var mtxb = Matrix.IDTranslate(ViewScreenPos.x, ViewScreenPos.y);
                DrawShapes[0] = CreatePrimitiveShapes.CenterWidget(trns);
            }

            _drawPoints[0] = mtrx.TransformToSDL_Point(_sv.Position.X, _sv.Position.Y);
            if(TrajectoryType == UserOrbitSettings.OrbitTrajectoryType.Elliptical)
            {
                //we do this so we can start drawing from the position of the entity,
                //and fade the line using alpha in the colour (when draw)
                int index = _taIndex;
                for (int i = 1; i < _numberOfPoints + 1; i++)
                {
                    if (index < _numberOfPoints - 1)

                        index++;
                    else
                        index = 0;
                   _drawPoints[i] = mtrx.TransformToSDL_Point(_points[index].X, _points[index].Y);
                
                }
            }
            else
            {
                _drawPoints = mtrx.TransformToSDL_Point(_points);
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
            base.Draw(rendererPtr, camera);
        }


    }


}