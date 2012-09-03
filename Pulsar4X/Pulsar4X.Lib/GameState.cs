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
        public void Commit()
        {
            // Save all changes to db
            throw new NotImplementedException();
        }


        public string Name { get; set; }

#region Entities

        private BindingList<Faction> _factions;
        public BindingList<Faction> Factions
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

        private BindingList<StarSystem> _starsystems;
        public BindingList<StarSystem> StarSystems
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

        private BindingList<Star> _stars;
        public BindingList<Star> Stars
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

        private BindingList<Planet> _planets;
        public BindingList<Planet> Planets
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
