using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using Pulsar4X.Entities;

namespace Pulsar4X
{
    class Model
    {
        public void Commit()
        {
            // Save all changes to db
            throw new NotImplementedException();
        }


        public string Name { get; set; }

#region Entities

        private ObservableCollection<Race> _races;
       

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
        }
#endregion Entities

    }
}
