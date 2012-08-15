using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace Pulsar4X.Entities
{
    public class Star
    {
        public ObservableCollection<Planet> Planets { get; set; }
        public StarSystem StarSystem { get; set; }
    }
}
