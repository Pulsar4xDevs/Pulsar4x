using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Pulsar4X.Entities
{
    public class Faction
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public Species Species { get; set; }

        public FactionTheme FactionTheme { get; set; }
        public FactionCommanderTheme CommanderTheme { get; set; }

        public List<StarSystem> KnownSystems { get; set; }
        public List<TaskForce> TaskForces { get; set; }
        public List<Commander> Commanders { get; set; }
    }
}
