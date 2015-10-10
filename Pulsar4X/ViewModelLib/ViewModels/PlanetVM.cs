using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Pulsar4X.ECSLib;

namespace Pulsar4X.ViewModel
{
    /// <summary>
    /// This class is a view model for all planets, asteroids, comets, moons, etc.
    /// </summary>
    public class PlanetVM : IViewModel
    {
        #region Properties


        #region Metadata

        private GameVM _gameVM;
        public Entity Entity
        {
            get { return _entity; }
            private set
            {
                _entity = value;
                OnPropertyChanged();
            }
        }
        private Entity _entity;

        public Guid ID { get { return Entity.Guid; } }

        #endregion

        #region PositionDB Properties

        public Vector4 Position
        {
            get { return _position; }
            private set
            {
                _position = value;
                OnPropertyChanged();
            }
        }
        private Vector4 _position;

        public Vector4 SystemPosition
        {
            get
            {
                Vector4 parentPos;
                if (ParentPlanet != null)
                {
                    parentPos = ParentPlanet.SystemPosition;
                }
                else
                {
                    parentPos = ParentStar.SystemPosition;
                }

                return Position + parentPos;
            }
        }

        #endregion

        #region OrbitDB Properties

        public BindingList<PlanetVM> Children
        {
            get { return _children; }
            set
            {
                _children = value;
                OnPropertyChanged();
            }
        }
        private BindingList<PlanetVM> _children;

        public StarVM ParentStar
        {
            get
            {
                if (_parentStar == null)
                    _parentStar = _gameVM.GetSystem(Entity.Guid).GetStar(_parentStarGuid);
                return _parentStar;
            }
            set
            {
                _parentStar = value;
                OnPropertyChanged();
            }
        }
        private StarVM _parentStar;
        private Guid _parentStarGuid;
        public PlanetVM ParentPlanet
        {
            get
            {
                if (_parentPlanet == null && _parentPlanetGuid != null)
                {
                    _parentPlanet = _gameVM.GetSystem(Entity.Guid).GetPlanet(Entity.GetDataBlob<OrbitDB>().Parent.Guid);
                }

                return _parentPlanet;
            }
            set
            {
                _parentPlanet = value;
                OnPropertyChanged();
            }
        }
        private PlanetVM _parentPlanet; // null if not a moon.
        private Guid? _parentPlanetGuid;

        public double SemiMajorAxis
        {
            get { return _semiMajorAxis; }
            set
            {
                _semiMajorAxis = value;
                OnPropertyChanged();
            }
        }
        private double _semiMajorAxis;

        public double Apoapsis
        {
            get { return _apoapsis; }
            set
            {
                _apoapsis = value;
                OnPropertyChanged();
            }
        }
        private double _apoapsis;

        public double Periapsis
        {
            get { return _periapsis; }
            set
            {
                _periapsis = value;
                OnPropertyChanged();
            }
        }
        private double _periapsis;
        
        public double ArgumentOfPeriapsis
        {
            get { return _argOfPeriapsis;}
            set
            {
                _argOfPeriapsis = value;
                OnPropertyChanged();
            }
        }
        public double _argOfPeriapsis;


        public double LongitudeOfAscendingNode
        {
            get { return _longitudeOfAscendingNode;}
            set
            {
                _longitudeOfAscendingNode = value;
                OnPropertyChanged();
            }
        }

        private double _longitudeOfAscendingNode;

        public double Eccentricity
        {
            get { return _eccentricity; }
            set
            {
                _eccentricity = value;
                OnPropertyChanged();
            }
        }
        private double _eccentricity;

        public double Inclination
        {
            get { return _inclination; }
            set
            {
                _inclination = value;
                OnPropertyChanged();
            }
        }
        private double _inclination;

        public TimeSpan OrbitalPeriod
        {
            get { return _orbitalPeriod; }
            set
            {
                _orbitalPeriod = value;
                OnPropertyChanged();
            }
        }
        private TimeSpan _orbitalPeriod;

        #endregion

        #region NameDB Properties

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }
        private string _name;

        #endregion

        #region MassVolumeDB Properties

        // in earth masses would be best.
        public double Mass
        {
            get { return _mass; }
            set
            {
                _mass = value;
                OnPropertyChanged();
            }
        } 
        private double _mass;

        public double Density
        {
            get { return _density; }
            set
            {
                _density = value;
                OnPropertyChanged();
            }
        }
        private double _density;

        public double Radius
        {
            get { return _radius; }
            set
            {
                _radius = value;
                OnPropertyChanged();
            }
        }
        private double _radius;

        public double Volume
        {
            get { return _volume; }
            set
            {
                _volume = value;
                OnPropertyChanged();
            }
        }
        private double _volume;

        public double SurfaceGravity
        {
            get { return _surfaceGravity; }
            set
            {
                _surfaceGravity = value;
                OnPropertyChanged();
            }
        }
        private double _surfaceGravity;

        #endregion

        #region SystemBodyDB Properties

        public string PlanetType
        {
            get { return _planetType; }
            set
            {
                _planetType = value;
                OnPropertyChanged();
            }
        }
        private string _planetType;

        public double AxialTilt
        {
            get { return _axialTilt; }
            set
            {
                _axialTilt = value;
                OnPropertyChanged();
            }
        }
        private double _axialTilt;

        public double BaseTemperature
        {
            get { return _baseTemperature; }
            set
            {
                _baseTemperature = value;
                OnPropertyChanged();
            }
        }
        private double _baseTemperature;

        public TimeSpan LengthOfDay
        {
            get { return _lengthOfDay; }
            set
            {
                _lengthOfDay = value;
                OnPropertyChanged();
            }
        }
        private TimeSpan _lengthOfDay;

        public double MagneticField
        {
            get { return _magneticField; }
            set
            {
                _magneticField = value;
                OnPropertyChanged();
            }
        }
        private double _magneticField;

        public string Tectonics
        {
            get { return _tectonics; }
            set
            {
                _tectonics = value;
                OnPropertyChanged();
            }
        }
        private string _tectonics;

        #endregion

        #region AtmosphereDB Properties

        public string AtmosphereInAtm
        {
            get { return _atmosphereInAtm; }
            set
            {
                _atmosphereInAtm = value;
                OnPropertyChanged();
            }
        }
        private string _atmosphereInAtm;

        public string AtmosphereInPercent
        {
            get { return _atmosphereInPercent; }
            set
            {
                _atmosphereInPercent = value;
                OnPropertyChanged();
            }
        }
        private string _atmosphereInPercent;

        public double Pressure
        {
            get { return _pressure; }
            set
            {
                _pressure = value;
                OnPropertyChanged();
            }
        }
        private double _pressure;

        public double Albedo
        {
            get { return _albedo; }
            set
            {
                _albedo = value;
                OnPropertyChanged();
            }
        }
        private double _albedo;

        public double SurfaceTemp
        {
            get { return _surfaceTemp; }
            set
            {
                _surfaceTemp = value;
                OnPropertyChanged();
            }
        }
        private double _surfaceTemp;

        public double GreenhouseFactor
        {
            get { return _greenhouseFactor; }
            set
            {
                _greenhouseFactor = value;
                OnPropertyChanged();
            }
        }
        private double _greenhouseFactor;

        public double GreenhousePressure
        {
            get { return _greenhousePressure; }
            set
            {
                _greenhousePressure = value;
                OnPropertyChanged();
            }
        }
        private double _greenhousePressure;

        public bool Hydrosphere
        {
            get { return _hydrosphere; }
            set
            {
                _hydrosphere = value;
                OnPropertyChanged();
            }
        }
        private bool _hydrosphere;

        public float HydrosphereExtent
        {
            get { return _hydrosphereExtent; }
            set
            {
                _hydrosphereExtent = value;
                OnPropertyChanged();
            }
        }
        private float _hydrosphereExtent;

        #endregion

        /// < @todo add runis data.

        #endregion

        #region Functions

        #region Construction

        private PlanetVM(GameVM gameVM, Entity entity)
        {
            _gameVM = gameVM;
            Entity = entity;
        }

        /// <summary>
        /// Creates and fills out the properties of this ViewModel from the provided entity.
        /// </summary>
        internal static PlanetVM Create(GameVM gameVM, Entity entity)
        {
            PlanetVM newVM = new PlanetVM(gameVM, entity);

            // Initialize the data.
            newVM.Refresh();

            return newVM;
        }

        /// <summary>
        /// Creates and fills out the properties of this ViewModel from the entity with the provided Guid.
        /// </summary>
        /// <exception cref="InvalidOperationException">Cannot create a Planet ViewModel without an initialized game.</exception>
        /// <exception cref="GuidNotFoundException">Thrown when the supplied Guid is not found in the game.</exception>
        internal static PlanetVM Create(GameVM gameVM, Guid guid)
        {
            if (gameVM.Game == null)
            {
                throw new InvalidOperationException("Cannot create a PlanetVM without an initialized game.");
            }

            Entity entity;
            if (!gameVM.Game.GlobalManager.FindEntityByGuid(guid, out entity))
            {
                throw new GuidNotFoundException(guid);
            }

            return Create(gameVM, entity);
        }

        #endregion

        #region IViewModel

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Refreshes the properties of this ViewModel.
        /// 
        /// If partialRefresh is set to true, the ViewModel will try to update only data changes during a pulse.
        /// </summary>
        public void Refresh(bool partialRefresh = false)
        {
            // Get up-to-date datablobs for this entity.
            PositionDB positionDB = _entity.GetDataBlob<PositionDB>();
            UpdateProperties(positionDB);

            SystemBodyDB systemBodyDB = _entity.GetDataBlob<SystemBodyDB>();
            UpdateProperties(systemBodyDB, partialRefresh);

            NameDB nameDB = _entity.GetDataBlob<NameDB>();
            UpdateProperties(nameDB);

            //AtmosphereDB atmosphereDB = _entity.GetDataBlob<AtmosphereDB>();
            //UpdateProperties(atmosphereDB);

            // Check if we're doing a full refresh.
            if (!partialRefresh)
            {
                OrbitDB orbitDB = _entity.GetDataBlob<OrbitDB>();
                UpdateProperties(orbitDB);

                MassVolumeDB massVolumeDB = _entity.GetDataBlob<MassVolumeDB>();
                UpdateProperties(massVolumeDB);
            }
        }

        #endregion

        #region Property Update Functions

        private void UpdateProperties([NotNull] PositionDB positionDB)
        {
            if (positionDB == null)
            {
                throw new ArgumentNullException("positionDB");
            }

            Position = positionDB.Position;
            if (Name == "Earth")
            {
            }
        }

        private void UpdateProperties([NotNull] OrbitDB orbitDB)
        {
            if (orbitDB == null)
            {
                throw new ArgumentNullException("orbitDB");
            }

            //Children.Clear();

            //foreach (Entity child in orbitDB.Children)
            //{
            //    // Create a ViewModel for this child.
            //    Children.Add(Create(child));
            //}

            if (orbitDB.Parent == null || orbitDB.ParentDB == null)
            {
                throw new InvalidOperationException("PlanetVM provided invalid OrbitDB. Planets must have a valid parent.");
            }

            if (orbitDB.Parent.GetDataBlob<StarInfoDB>() != null)
            {
                // Parent is a star.
                _parentPlanetGuid = null;
                _parentStarGuid = orbitDB.Parent.Guid;
            }
            else
            {
                // Parent is a planet.
                _parentPlanetGuid = orbitDB.Parent.Guid;
                // Parent's parent is the star.
                _parentStarGuid = orbitDB.Parent.GetDataBlob<OrbitDB>().Parent.Guid;
            }

            SemiMajorAxis = orbitDB.SemiMajorAxis;
            Apoapsis = orbitDB.Apoapsis;
            Periapsis = orbitDB.Periapsis;
            ArgumentOfPeriapsis = orbitDB.ArgumentOfPeriapsis;
            LongitudeOfAscendingNode = orbitDB.LongitudeOfAscendingNode;
            Eccentricity = orbitDB.Eccentricity;
            Inclination = orbitDB.Inclination;
            OrbitalPeriod = orbitDB.OrbitalPeriod;
        }

        private void UpdateProperties([NotNull] NameDB nameDB)
        {
            if (nameDB == null)
            {
                throw new ArgumentNullException("nameDB");
            }
            Name = nameDB.DefaultName;
            //todo: Name = nameDB.GetName(App.Current.Faction);
        }

        private void UpdateProperties([NotNull] MassVolumeDB massVolumeDB)
        {
            if (massVolumeDB == null)
            {
                throw new ArgumentNullException("massVolumeDB");
            }

            Mass = massVolumeDB.Mass;
            Density = massVolumeDB.Density;
            Radius = massVolumeDB.Radius;
            Volume = massVolumeDB.Volume;
            SurfaceGravity = massVolumeDB.SurfaceGravity;
        }

        private void UpdateProperties([NotNull] SystemBodyDB systemBodyDB, bool partialRefresh)
        {
            if (systemBodyDB == null)
            {
                throw new ArgumentNullException("systemBodyDB");
            }

            if (!partialRefresh)
            {
                // Full Refresh. Update unchanging variables.
                PlanetType = systemBodyDB.Type.ToString();
                AxialTilt = systemBodyDB.AxialTilt;
                BaseTemperature = systemBodyDB.BaseTemperature;
                LengthOfDay = systemBodyDB.LengthOfDay;
                MagneticField = systemBodyDB.MagneticField;
                Tectonics = systemBodyDB.Tectonics.ToString();
            }
            // Unused currently. TODO: Review if this should be moved to AtmosphereDB in the game lib.
            // AtmosphericDust = systemBodyDB.AtmosphericDust;
            // RadiationLevel = systemBodyDB.RadiationLevel;
            // SupportsPopulation = systemBodyDB.SupportsPopulation;
            // Minerals = systemBodyDB.Minerals;
        }

        private void UpdateProperties([NotNull] AtmosphereDB atmosphereDB)
        {
            if (atmosphereDB == null)
            {
                throw new ArgumentNullException("atmosphereDB");
            }
            AtmosphereInAtm = atmosphereDB.AtomsphereDescriptionAtm;
            AtmosphereInPercent = atmosphereDB.AtomsphereDescriptionInPercent;
            Pressure = atmosphereDB.Pressure;
            Albedo = atmosphereDB.Albedo;
            SurfaceTemp = atmosphereDB.SurfaceTemperature;
            GreenhouseFactor = atmosphereDB.GreenhouseFactor;
            GreenhousePressure = atmosphereDB.GreenhousePressure;
            Hydrosphere = atmosphereDB.Hydrosphere;
            HydrosphereExtent = atmosphereDB.HydrosphereExtent;
        }

        #endregion

        #endregion
    }
}