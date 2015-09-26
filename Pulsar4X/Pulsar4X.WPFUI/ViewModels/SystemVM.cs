using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using Pulsar4X.ECSLib;

namespace Pulsar4X.WPFUI.ViewModels
{
    public class SystemVM
    {
        private BindingList<StarVM> _stars;
        public BindingList<StarVM> Stars { get { return _stars;} }
        public List<StarVM> StarList { get { return _starDictionary.Values.ToList(); } } 
        private StarVM _parentStar;
        public StarVM ParentStar { get { return _parentStar;} }

        private BindingList<PlanetVM> _planets;
        public BindingList<PlanetVM> Planets { get { return _planets;} }
        public List<PlanetVM> PlanetList { get { return _planetDictionary.Values.ToList(); } } 
        private string _name;
        public string Name { get { return _name;} }

        public Guid ID { get; set; }

        public StarSystem StarSystem { get; set; }
        // add list of ships

        // add list of waypoints

        // Add list of colonies? maybe?
        private Dictionary<Guid, StarVM> _starDictionary;

        private Dictionary<Guid, PlanetVM> _planetDictionary;

        /// <summary>
        /// returns the StarVM for a given bodies guid.
        /// </summary>
        /// <param name="bodyGuid"></param>
        /// <returns></returns>
        internal StarVM GetStar(Guid bodyGuid)
        {
            Entity bodyEntity;
            Guid starGuid = new Guid();
            if (_starDictionary.ContainsKey(bodyGuid))
                starGuid = bodyGuid;

            else if (App.Current.Game.GlobalManager.FindEntityByGuid(bodyGuid, out bodyEntity))
            {
                if (bodyEntity.HasDataBlob<StarInfoDB>())
                {
                    starGuid = bodyEntity.Guid;
                }
            }
            else throw new GuidNotFoundException(bodyGuid);

            if (!_starDictionary.ContainsKey(starGuid))
            {
                StarVM starVM = StarVM.Create(starGuid, this);
                _starDictionary.Add(starGuid, starVM);
                if(!_stars.Contains(starVM))
                    _stars.Add(starVM);
            }
            return _starDictionary[starGuid];
        }

        internal PlanetVM GetPlanet(Guid bodyGuid)
        {
            Entity bodyEntity;
            Guid planetGuid = new Guid();
            if (_planetDictionary.ContainsKey(bodyGuid))
                planetGuid = bodyGuid;
            else if (App.Current.Game.GlobalManager.FindEntityByGuid(bodyGuid, out bodyEntity))
            {
                if (bodyEntity.HasDataBlob<SystemBodyDB>())
                {
                    planetGuid = bodyEntity.Guid;
                }
            }
            else throw new GuidNotFoundException(bodyGuid);

            if (!_planetDictionary.ContainsKey(planetGuid))
            {
                PlanetVM planetVM = PlanetVM.Create(planetGuid);
                if (!_planets.Contains(planetVM))
                    _planets.Add(planetVM);
                _planetDictionary.Add(planetGuid, planetVM);
            }
            return _planetDictionary[planetGuid];
        }

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

        #endregion

        #region Construction

        private SystemVM(StarSystem starSystem)
        {
            _name = starSystem.NameDB.DefaultName;
            StarSystem = starSystem;
            _stars = new BindingList<StarVM>();
            _planets = new BindingList<PlanetVM>();
            _starDictionary = new Dictionary<Guid, StarVM>();
            _planetDictionary = new Dictionary<Guid, PlanetVM>();
            //find most massive star, this is the parent.
            Entity parentStar = starSystem.SystemManager.GetFirstEntityWithDataBlob<StarInfoDB>();
            StarVM parentstarVM = StarVM.Create(parentStar, this);
            foreach (var star in starSystem.SystemManager.GetAllEntitiesWithDataBlob<StarInfoDB>())
            {
                StarVM starVM = StarVM.Create(star, this);
                if(!_stars.Contains(starVM))
                    _stars.Add(starVM);
                if(!_starDictionary.ContainsKey(star.Guid))
                    _starDictionary.Add(star.Guid, starVM);
                if (star.GetDataBlob<MassVolumeDB>().Mass > parentStar.GetDataBlob<MassVolumeDB>().Mass)
                {
                    parentStar = star;
                    parentstarVM = starVM;
                }
            }
            _parentStar = parentstarVM;
            ID = _parentStar.Entity.Guid;
            foreach (var planet in starSystem.SystemManager.GetAllEntitiesWithDataBlob<SystemBodyDB>())
            {
                PlanetVM planetVM = PlanetVM.Create(planet);
                if (!_planets.Contains(planetVM))
                    _planets.Add(planetVM);
                if(!_planetDictionary.ContainsKey(planet.Guid))
                    _planetDictionary.Add(planet.Guid,planetVM);
               
            }
        }

        /// <summary>
        /// Creates and fills out the properties of this ViewModel from the entity with the provided Guid.
        /// </summary>
        /// <exception cref="InvalidOperationException">Cannot create a Planet ViewModel without an initialized game.</exception>
        /// <exception cref="GuidNotFoundException">Thrown when the supplied Guid is not found in the game.</exception>
        internal static SystemVM Create(Guid guid)
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
            StarSystem starSystem = null;
            
            for (int i = 0; i < App.Current.Game.Systems.Count; i++)
            {
                if (App.Current.Game.Systems[i].SystemManager.FindEntityByGuid(guid, out entity))
                {
                    starSystem = App.Current.Game.Systems[i];
                    i = App.Current.Game.Systems.Count;
                }
            }            
            return Create(starSystem);
        }

        /// <summary>
        /// Creates and fills out the properties of this ViewModel from the provided entity.
        /// </summary>
        public static SystemVM Create(StarSystem starSystem)
        {
            SystemVM newVM = new SystemVM(starSystem);

            // Initialize the data.
            newVM.Refresh();

            return newVM;
        }


        #endregion

        public void Refresh(bool partialRefresh = false)
        {
            foreach (var star in _stars)
            {
                star.Refresh();
            }
            foreach (var planet in _planets)
            {
                planet.Refresh();
            }
        }
    }
}