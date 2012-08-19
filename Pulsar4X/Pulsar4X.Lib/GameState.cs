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

        private ObservableCollection<Race> _races;
        public ObservableCollection<Race> Races
        {
            get
            {
                if (_races == null)
                {
                    //Load from DB here
                }
                return _races;
            }
            set { _races = value; }
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
