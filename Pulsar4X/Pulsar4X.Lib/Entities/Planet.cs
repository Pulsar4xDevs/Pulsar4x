using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace Pulsar4X.Entities
{
    public class Planet
    {
        public ObservableCollection<Planet> Planet { get; set; } // Moons
        public Star Star { get; set; }
    }
}
