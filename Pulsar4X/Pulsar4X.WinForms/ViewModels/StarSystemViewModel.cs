using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.Stargen;
using Pulsar4X.Entities;
using Pulsar4X.WinForms.Helpers;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Forms;
using System.Linq.Expressions;

namespace Pulsar4X.WinForms.ViewModels
{
    public class StarSystemViewModel : INotifyPropertyChanged
    {
        private StarSystem _currentstarsystem;
        public StarSystem CurrentStarSystem
        {
            get { return _currentstarsystem; }
            set
            {
                _currentstarsystem = value;
                //NotifyPropertyChanged("CurrentStarSystem");
                OnPropertyChanged(() => CurrentStarSystem);
                CurrentStarSystemAge = _currentstarsystem.Stars[0].Age.ToString();
                Stars = new BindingList<Star>(_currentstarsystem.Stars);
                StarsSource.DataSource = Stars;
            }
        }
        public BindingList<StarSystem> StarSystems { get; set; }

        private string _currentstarsystemage;
        public string CurrentStarSystemAge
        {
            get { return _currentstarsystemage; }
            set
            {
                _currentstarsystemage = value;
                //NotifyPropertyChanged("CurrentStarSystemAge");
                OnPropertyChanged(() => CurrentStarSystemAge);
            }
        }

        private BindingList<Star> _stars;
        public BindingList<Star> Stars
        {
            get { return _stars; }
            set
            {
                _stars = value;
                //NotifyPropertyChanged("Stars");
                OnPropertyChanged(() => Stars);
                CurrentStar = _stars[0];
            }
        }

        private BindingSource _starssource;
        public BindingSource StarsSource
        {
            get
            {
                if (_starssource == null)
                    _starssource = new BindingSource();
                return _starssource;
            }
            set
            {
                _starssource = value;
            }
        }

        private BindingSource _planetsource;
        public BindingSource PlanetSource
        {
            get
            {
                if (_planetsource == null)
                    _planetsource = new BindingSource();
                return _planetsource;
            }
            set
            {
                _planetsource = value;
            }
        }

        private Star _currentstar;
        public Star CurrentStar
        {
            get
            {
                return _currentstar;
            }
            set
            {
                _currentstar = value;
                //NotifyPropertyChanged("CurrentStar");
                OnPropertyChanged(() => CurrentStar);
                var planetslist = new BindingList<Planet>();
                foreach (Planet planet in CurrentStar.Planets)
                {
                    planetslist.Add(planet);
                    foreach (Planet moon in planet.Moons)
                    {
                        planetslist.Add(moon);
                    }
                }
                PlanetSource.DataSource = planetslist;

            }
        }
        public Planet CurrentPlanet { get; set; }

        public bool isSM { get; set; }
        public bool isNotSM { get { return !isSM; } set { isSM = !value; } }

        public StarSystemViewModel()
        {
            // Just gen a Starsystem
            StarSystems = GameState.Instance.StarSystems;
            CurrentStarSystem = GameState.Instance.StarSystems.First();
            CurrentStar = CurrentStarSystem.Stars.First();
            CurrentPlanet = CurrentStar.Planets.First();
        }

        private void OnPropertyChanged(Expression<Func<object>> property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this,
                    new PropertyChangedEventArgs(BindingHelper.Name(property)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
