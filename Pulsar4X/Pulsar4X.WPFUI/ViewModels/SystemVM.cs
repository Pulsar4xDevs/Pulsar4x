using System;
using System.ComponentModel;
using System.Windows;
using Pulsar4X.ECSLib;

namespace Pulsar4X.WPFUI.ViewModels
{
    public class SystemVM
    {
        private BindingList<StarVM> _stars;

        private StarVM _parentStar;

        private BindingList<PlanetVM> _Planets;

        public BindingList<PlanetVM> Planets { get { return _Planets;} } 

        private string _name;

        public Guid ID { get; set; }

        public StarSystem StarSystem { get; set; }
        // add list of ships

        // add list of waypoints

        // Add list of colonies? maybe?

        #region IViewModel

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Construction

        private SystemVM(StarSystem starSystem)
        {
            _name = starSystem.NameDB.DefaultName;
            StarSystem = starSystem;
            _stars = new BindingList<StarVM>();
            _Planets = new BindingList<PlanetVM>();
            //find most massive star, this is the parent.
            Entity parentStar = starSystem.SystemManager.GetFirstEntityWithDataBlob<StarInfoDB>();
            StarVM parentstarVM = StarVM.Create(parentStar);
            foreach (var star in starSystem.SystemManager.GetAllEntitiesWithDataBlob<StarInfoDB>())
            {
                StarVM starVM = StarVM.Create(star);
                _stars.Add(starVM);
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
                _Planets.Add(planetVM);

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
            ECSLib.StarSystem starSystem = null;
            
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
            //throw new NotImplementedException();
        }
    }
}