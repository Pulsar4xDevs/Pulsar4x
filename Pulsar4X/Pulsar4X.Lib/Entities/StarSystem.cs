using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace Pulsar4X.Entities
{
    public class StarSystem
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public ObservableCollection<Star> Stars { get; set; }

        public StarSystem()
            : this(string.Empty)
        {
        }

        public StarSystem(string name)
        {
            Id = Guid.NewGuid();
            Name = name;
            Stars = new ObservableCollection<Star>();
        }
    }
}
