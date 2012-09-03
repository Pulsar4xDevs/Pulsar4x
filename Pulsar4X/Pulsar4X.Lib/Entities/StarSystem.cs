using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Pulsar4X.Entities
{
    public class StarSystem
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public BindingList<Star> Stars { get; set; }

        public StarSystem()
            : this(string.Empty)
        {
        }

        public StarSystem(string name)
        {
            Id = Guid.NewGuid();
            Name = name;
            Stars = new BindingList<Star>();
        }
    }
}
