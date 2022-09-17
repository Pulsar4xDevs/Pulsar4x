using System;
using Pulsar4X.ECSLib;
using Pulsar4X.Orbital;
using SDL2;
using ImGuiNET;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.SDL2UI
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
        protected SDL.SDL_Point[] _drawPoints = new SDL.SDL_Point[0];
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

            
            if (_orbitDB.Inclination > 1/2 * Math.PI && _orbitDB.Inclination < 3/2 * Math.PI) //orbitDB is in degrees.
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
            _drawPoints = new SDL.SDL_Point[_numberOfDrawSegments];

        }
        protected abstract void CreatePointArray();

        IPosition IKepler.PositionDB => BodyPositionDB;

        IPosition IKepler.ParentPosDB => _positionDB;

        double IKepler.SemiMaj => SemiMaj;

        double IKepler.SemiMin => SemiMinor;

        double IKepler.LoP_radians => _loP_radians;

        double IKepler.Eccentricity => _eccentricity;

        double IKepler.LinearEccent => _linearEccentricity;
    }
}
