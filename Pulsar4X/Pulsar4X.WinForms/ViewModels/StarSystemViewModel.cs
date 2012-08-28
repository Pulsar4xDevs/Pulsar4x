using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.Stargen;
using Pulsar4X.Entities;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Pulsar4X.ViewModels
{
    public class StarSystemViewModel : INotifyPropertyChanged
    {
        public StarSystem CurrentStarSystem { get; set; }
        public ObservableCollection<StarSystem> StarSystems { get; set; }

        public Star CurrentStar { get; set; }
        public Planet CurrentPlanet { get; set; }

        public StarSystemViewModel()
        {
            // Just gen a Starsystem
            var ssf = new StarSystemFactory();
            var ss = ssf.Create("Test");
            StarSystems = new ObservableCollection<StarSystem>();
            StarSystems.Add(ss);
            CurrentStarSystem = ss;
            CurrentStar = CurrentStarSystem.Stars.First();
            CurrentPlanet = CurrentStar.Planets.First();
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
