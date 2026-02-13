using System;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;
using Pulsar4X.Orbital;
using SDL3;
using System.Collections.Generic;
using Pulsar4X.Orbits;
using Pulsar4X.Movement;

namespace Pulsar4X.Client
{

    public interface IKepler
    {
        internal IPosition PositionDB { get; }
        internal IPosition ParentPosDB{ get; }
        internal double SemiMaj{ get; }
        internal double SemiMin{ get; }
        internal double LoP_radians{ get; }
        internal double Eccentricity{ get; }
        internal double LinearEccent{ get; }

    }

    /// <summary>
    /// A Collection of Shapes which will make up an icon.
    /// </summary>
    public abstract class OrbitIconBase : Icon, IUpdateUserSettings, IKepler
    {
        #region Static properties
        protected EntityManager _mgr;
        protected OrbitDB _orbitDB;
        internal IPosition BodyPositionDB;
        protected Vector2 _bodyrelativePos;
        protected Vector2 _bodyAbsolutePos;
        internal float SemiMaj;
        internal float SemiMinor;
        protected float _loP_Degrees; //longditudeOfPeriapsis (loan + aop)
        internal float _loP_radians; //longditudeOfPeriapsis (loan + aop) in radians
        internal float _aop;
        internal float _eccentricity;
        internal float _linearEccentricity; //distance from the center of the ellpse to one of the focal points.
        protected Vector2[] _points; //we calculate points around the ellipse and add them here. when we draw them we translate all the points.
        protected SDL.Point[] _drawPoints = new SDL.Point[0];
        protected SDL.Point[] _fullOrbitDrawPoints = new SDL.Point[0];
        protected bool IsRetrogradeOrbit = false;
        #endregion

        #region Dynamic Properties
        //change each game update
        //internal float _ellipseStartArcAngleRadians;
        protected int _index;

        //user adjustable variables:
        internal UserOrbitSettings.OrbitBodyType BodyType = UserOrbitSettings.OrbitBodyType.Unknown;
        internal UserOrbitSettings.OrbitTrajectoryType TrajectoryType = UserOrbitSettings.OrbitTrajectoryType.Unknown;
        protected List<List<UserOrbitSettings>> _userOrbitSettingsMtx;
        protected UserOrbitSettings _userSettings { get { return _userOrbitSettingsMtx[(int)BodyType][(int)TrajectoryType]; } }

        //change after user makes adjustments:
        protected byte _numberOfArcSegments = 180; //how many segments in a complete 360 degree ellipse. this is set in UserOrbitSettings, localy adjusted because the whole point array needs re-creating when it changes.
        protected int _numberOfDrawSegments; //this is now many segments get drawn in the ellipse, ie if the _ellipseSweepAngle or _numberOfArcSegments are less, less will be drawn.
        protected float _segmentArcSweepRadians; //how large each segment in the drawn portion of the ellipse.
        protected float _alphaChangeAmount;




        #endregion
        public OrbitIconBase(EntityState entityState, List<List<UserOrbitSettings>> settings) : base(entityState.Entity.GetDataBlob<PositionDB>())
        {
            BodyType = entityState.BodyType;

            entityState.OrbitIcon = this;
            _mgr = entityState.Entity.Manager;
            _userOrbitSettingsMtx = settings;
            _orbitDB = entityState.Entity.GetDataBlob<OrbitDB>();
            if (entityState.Entity.HasDataBlob<OrbitUpdateOftenDB>())
                _orbitDB = entityState.Entity.GetDataBlob<OrbitUpdateOftenDB>();
            BodyPositionDB = entityState.Position; //entityState.Entity.GetDataBlob<PositionDB>();
            if (_orbitDB.Parent == null) //primary star
            {
                _positionDB = BodyPositionDB;
            }
            else
            {
                _positionDB = _orbitDB.Parent.GetDataBlob<PositionDB>(); //orbit's position is parent's body position.
            }

            SemiMaj = (float)_orbitDB.SemiMajorAxis;

            SemiMinor = (float)EllipseMath.SemiMinorAxis(_orbitDB.SemiMajorAxis, _orbitDB.Eccentricity);


            _eccentricity = (float)_orbitDB.Eccentricity;
            _linearEccentricity = (float)(_eccentricity * _orbitDB.SemiMajorAxis); //linear ecentricity

            var inclination = Angle.NormaliseRadiansPositive(_orbitDB.Inclination);
            if (inclination > 0.5 * Math.PI && inclination < 1.5 * Math.PI)
            {
                IsRetrogradeOrbit = true;
                //_loP_Degrees = (float)(_orbitDB.LongitudeOfAscendingNode - _orbitDB.ArgumentOfPeriapsis);
            }
            /*
            else
            {

                _loP_Degrees = (float)(_orbitDB.LongitudeOfAscendingNode + _orbitDB.ArgumentOfPeriapsis);
            }
            _loP_radians = (float)(Angle.ToRadians(_loP_Degrees));
            */
            var i = _orbitDB.Inclination;
            var _aoP = _orbitDB.ArgumentOfPeriapsis;
            var loan = _orbitDB.LongitudeOfAscendingNode;
            var lop = OrbitMath.GetLongditudeOfPeriapsis(i, _aoP, loan);
            _loP_radians = (float)lop;
            _loP_Degrees = (float)Angle.ToDegrees(lop);

        }
        /// <summary>
        ///calculate anything that could have changed from the users input.
        /// </summary>
        public virtual void UpdateUserSettings()
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
            _drawPoints = new SDL.Point[_numberOfDrawSegments];
            _fullOrbitDrawPoints = new SDL.Point[_numberOfArcSegments + 1];

        }
        protected abstract void CreatePointArray();

        /// <summary>
        /// Tests if a screen-space mouse position is near the orbit line.
        /// Returns the segment index of the closest point if within threshold, or -1.
        /// Also outputs the interpolated true anomaly at the closest point.
        /// </summary>
        public (int segmentIndex, double trueAnomaly) HitTest(SDL.Point mousePos, float threshold = 10f)
        {
            if (_fullOrbitDrawPoints == null || _fullOrbitDrawPoints.Length < 2)
                return (-1, 0);

            float thresholdSq = threshold * threshold;
            float bestDistSq = thresholdSq;
            int bestIndex = -1;
            float bestT = 0;

            for (int i = 0; i < _numberOfArcSegments; i++)
            {
                var a = _fullOrbitDrawPoints[i];
                var b = _fullOrbitDrawPoints[i + 1];

                float distSq = PointToSegmentDistSq(mousePos.X, mousePos.Y, a.X, a.Y, b.X, b.Y, out float t);
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestIndex = i;
                    bestT = t;
                }
            }

            if (bestIndex < 0)
                return (-1, 0);

            // Interpolate the true anomaly between segment endpoints
            double ta = bestIndex * _segmentArcSweepRadians + bestT * _segmentArcSweepRadians;
            return (bestIndex, ta);
        }

        /// <summary>
        /// Returns the screen position of a point on the orbit at a given index.
        /// </summary>
        public SDL.Point GetScreenPointAtIndex(int index)
        {
            if (index >= 0 && index < _fullOrbitDrawPoints.Length)
                return _fullOrbitDrawPoints[index];
            return new SDL.Point();
        }

        /// <summary>
        /// Squared distance from point (px,py) to line segment (ax,ay)-(bx,by).
        /// Outputs t in [0,1] for the closest interpolation parameter.
        /// </summary>
        private static float PointToSegmentDistSq(float px, float py, float ax, float ay, float bx, float by, out float t)
        {
            float dx = bx - ax;
            float dy = by - ay;
            float lenSq = dx * dx + dy * dy;
            if (lenSq < 0.0001f)
            {
                t = 0;
                float ex = px - ax;
                float ey = py - ay;
                return ex * ex + ey * ey;
            }
            t = ((px - ax) * dx + (py - ay) * dy) / lenSq;
            t = Math.Clamp(t, 0f, 1f);
            float cx = ax + t * dx;
            float cy = ay + t * dy;
            float fx = px - cx;
            float fy = py - cy;
            return fx * fx + fy * fy;
        }

        IPosition IKepler.PositionDB => BodyPositionDB;

        IPosition IKepler.ParentPosDB => _positionDB;

        double IKepler.SemiMaj => SemiMaj;

        double IKepler.SemiMin => SemiMinor;

        double IKepler.LoP_radians => _loP_radians;

        double IKepler.Eccentricity => _eccentricity;

        double IKepler.LinearEccent => _linearEccentricity;
    }
}
