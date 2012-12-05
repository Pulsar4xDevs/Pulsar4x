using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Pulsar4X.Entities
{
    public class Faction : GameEntity
    {
        public string Title { get; set; }
        public Species Species { get; set; }

        public FactionTheme FactionTheme { get; set; }
        public FactionCommanderTheme CommanderTheme { get; set; }

        public BindingList<StarSystem> KnownSystems { get; set; }
        public BindingList<TaskForce> TaskForces { get; set; }
        public BindingList<Commander> Commanders { get; set; }
        public BindingList<Population> Populations { get; set; }

        public Faction()
        {
            Name = "Human Federation";
            Species = new Species(); // go with the default Species!
        }

        public Faction(string a_oName, Species a_oSpecies)
        {
            Name = a_oName;
            Species = a_oSpecies;
        }
    }
}
