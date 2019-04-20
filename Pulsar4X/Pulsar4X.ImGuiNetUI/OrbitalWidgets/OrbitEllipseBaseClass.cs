using System;
using Pulsar4X.ECSLib;
using SDL2;
using ImGuiNET;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.SDL2UI
{

    /// <summary>
    /// A Collection of Shapes which will make up an icon.
    /// </summary>
    public abstract class OrbitIconBase : Icon
    {
        #region Static properties
        protected EntityManager _mgr;
        protected OrbitDB _orbitDB;
        internal IPosition BodyPositionDB;
        protected PointD _bodyRalitivePos;
        protected float _orbitEllipseMajor;
        internal float _orbitEllipseSemiMaj;
        protected float _orbitEllipseMinor;
        internal float _orbitEllipseSemiMinor;
        protected float _orbitAngleDegrees; //the orbit is an ellipse which is rotated arround one of the focal points. 
        internal float _orbitAngleRadians; //the orbit is an ellipse which is rotated arround one of the focal points. 
        internal float _linearEccentricity; //distance from the center of the ellpse to one of the focal points. 
        protected PointD[] _points; //we calculate points around the ellipse and add them here. when we draw them we translate all the points. 
        protected SDL.SDL_Point[] _drawPoints = new SDL.SDL_Point[0];
        protected bool IsClockwiseOrbit = true;
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
            BodyPositionDB = entityState.Position; //entityState.Entity.GetDataBlob<PositionDB>();
            if (_orbitDB.Parent == null) //primary star
            {
                _positionDB = BodyPositionDB;
            }
            else
            {
                _positionDB = _orbitDB.Parent.GetDataBlob<PositionDB>(); //orbit's position is parent's body position. 
            }

            _orbitEllipseSemiMaj = (float)_orbitDB.SemiMajorAxis;
            _orbitEllipseMajor = _orbitEllipseSemiMaj * 2;
            _orbitEllipseSemiMinor = (float)EllipseMath.SemiMinorAxis(_orbitDB.SemiMajorAxis, _orbitDB.Eccentricity);
            _orbitEllipseMinor = _orbitEllipseSemiMinor * 2;


            _linearEccentricity = (float)(_orbitDB.Eccentricity * _orbitDB.SemiMajorAxis); //linear ecentricity

            if (_orbitDB.Inclination > 90 && _orbitDB.Inclination < 270) //orbitDB is in degrees.
            {
                IsClockwiseOrbit = false;
                _orbitAngleDegrees = (float)(_orbitDB.LongitudeOfAscendingNode - _orbitDB.ArgumentOfPeriapsis);
            }
            else
            {

                _orbitAngleDegrees = (float)(_orbitDB.LongitudeOfAscendingNode + _orbitDB.ArgumentOfPeriapsis);
            }
            _orbitAngleRadians = (float)(Angle.ToRadians(_orbitAngleDegrees));



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

        }
        protected abstract void CreatePointArray();
    }
}
