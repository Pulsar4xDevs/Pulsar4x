using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using Pulsar4X.Entities;

namespace Pulsar4X
{
    public class GameState
    {
        public void Commit()
        {
            // Save all changes to db
            throw new NotImplementedException();
        }


        public string Name { get; set; }

#region Entities

        private ObservableCollection<Faction> _factions;
        public ObservableCollection<Faction> Factions
        {
            get
            {
                if (_factions == null)
                {
                    //Load from DB here
                }
                return _factions;
            }
            set { _factions = value; }
        }

        private ObservableCollection<StarSystem> _starsystems;
        public ObservableCollection<StarSystem> StarSystems
        {
            get
            {
                if (_starsystems == null)
                {
                    // Load from DB here
                }
                return _starsystems;
            }
            set { _starsystems = value; }
        }

        private ObservableCollection<Star> _stars;
        public ObservableCollection<Star> Stars
        {
            get
            {
                if (_stars == null)
                {
                    // Load from DB here
                }
                return _stars;
            }
            set { _stars = value; }
        }

        private ObservableCollection<Planet> _planets;
        public ObservableCollection<Planet> Planets
        {
            get
            {
                if (_planets == null)
                {
                    // Load from DB here
                }
                return _planets;
            }
            set { _planets = value; }
        }
#endregion Entities

    }
}
