using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Pulsar4X.ECSLib;

namespace Pulsar4X.WPFUI.ViewModels
{
    public class StarVM : IViewModel
    {
        public Entity Entity { get; set; }

        #region Children And Parents
        // these are the stars children, if any. Mostly used for rendering and/or navigating to children.

        private BindingList<StarVM> _childStars;
        public BindingList<StarVM> ChildStars { get { return _childStars; }}


        private BindingList<PlanetVM> _childPlanets;
        public BindingList<PlanetVM> ChildPlanets { get { return _childPlanets;}} 

        // the stars parent system and star (if it is not the root/primary star of the system) 
        private SystemVM _system;

        private StarVM _parentStar;

        #endregion

        #region Stars Properties

        private string _name;
        public string Name { get { return _name; } }

        private Guid _id;

        private string _starClass;

        private int _age;

        private double _ecoSphereRadius;

        private double _luminosity;

        private int _temperature;

        // in solar masses would be best.
        private double _mass;

        private double _density;

        private double _radius;

        private double _volume;

        private double _surfaceGravity;

        private double _semiMajorAxis;

        private double _apoapsis;

        private double _periapsis;

        private double _eccentricity;

        private double _inclination;

        private TimeSpan _year;


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
        #endregion

        #region Functions

        #region Construction

        private StarVM(Entity entity)
        {
            Entity = entity;
            _name = entity.GetDataBlob<NameDB>().DefaultName;
        }

        /// <summary>
        /// Creates and fills out the properties of this ViewModel from the provided entity.
        /// </summary>
        public static StarVM Create(Entity entity, SystemVM systemVM)
        {
            StarVM newVM = new StarVM(entity);

            // Initialize the data.
            newVM.Init(systemVM);

            newVM.Refresh();

            return newVM;
        }

        /// <summary>
        /// Creates and fills out the properties of this ViewModel from the entity with the provided Guid.
        /// </summary>
        /// <exception cref="InvalidOperationException">Cannot create a Planet ViewModel without an initialized game.</exception>
        /// <exception cref="GuidNotFoundException">Thrown when the supplied Guid is not found in the game.</exception>
        internal static StarVM Create(Guid guid, SystemVM systemVM)
        {
            if (App.Current.Game == null)
            {
                throw new InvalidOperationException("Cannot create a StarVM without an initialized game.");
            }

            Entity entity;
            if (!App.Current.Game.GlobalManager.FindEntityByGuid(guid, out entity))
            {
                throw new GuidNotFoundException(guid);
            }

            return Create(entity, systemVM);
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

        public void Refresh(bool partialRefresh = false)
        {
            _position = Entity.GetDataBlob<PositionDB>().Position;   
            
        }

        public void Init(SystemVM systemVM)
        {
            _system = systemVM;
            _childStars = new BindingList<StarVM>();
            _childPlanets = new BindingList<PlanetVM>();
            foreach (var childOrbit in Entity.GetDataBlob<OrbitDB>().Children)
            {
                if(childOrbit.HasDataBlob<StarInfoDB>())
                    _childStars.Add(_system.GetStar(childOrbit.Guid));
                else if(childOrbit.HasDataBlob<SystemBodyDB>())
                    _childPlanets.Add(_system.GetPlanet(childOrbit.Guid));
            }
        }

        #endregion

        #endregion

    }
}