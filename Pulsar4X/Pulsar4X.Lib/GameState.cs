using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using Pulsar4X.Entities;
using System.ComponentModel;

namespace Pulsar4X
{
    public class GameState
    {

        #region Singleton
        private static GameState instance;
        public static GameState Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GameState();
                }
                return instance;
            }
        }
        #endregion

        public void Commit()
        {
            // Save all changes to db
            throw new NotImplementedException();
        }

        #region Game Meta data
        public string Name { get; set; }
        public string SaveDirectoryPath { get; set; }
        #endregion
        

#region Entities

        private BindingList<Species> _species;
        public BindingList<Species> Species
        {
            get
            {
                if (_species == null)
                {
                    //Load from DB here
                    _species = new BindingList<Species>();
                }
                return _species;
            }
            set { _species = value; }
        }

        private BindingList<Faction> _factions;
        public BindingList<Faction> Factions
        {
            get
            {
                if (_factions == null)
                {
                    //Load from DB here
                    _factions = new BindingList<Faction>();
                }
                return _factions;
            }
            set { _factions = value; }
        }

        private BindingList<StarSystem> _starsystems;
        public BindingList<StarSystem> StarSystems
        {
            get
            {
                if (_starsystems == null)
                {
                    // Load from DB here
                    _starsystems = new BindingList<StarSystem>();
                }
                return _starsystems;
            }
            set { _starsystems = value; }
        }

        private BindingList<Star> _stars;
        public BindingList<Star> Stars
        {
            get
            {
                if (_stars == null)
                {
                    // Load from DB here
                    _stars = new BindingList<Star>();
                }
                return _stars;
            }
            set { _stars = value; }
        }

        private BindingList<Planet> _planets;
        public BindingList<Planet> Planets
        {
            get
            {
                if (_planets == null)
                {
                    // Load from DB here
                    _planets = new BindingList<Planet>();
                }
                return _planets;
            }
            set { _planets = value; }
        }
#endregion Entities

    }
}
