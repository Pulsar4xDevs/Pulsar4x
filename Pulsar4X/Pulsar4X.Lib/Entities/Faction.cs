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
        public Guid SpeciesId { get; set; }
        private Species _species;
        //[JsonIgnore]
        public Species Species
        {
            get { return _species; }
            set
            {
                _species = value;
                if (_species != null) SpeciesId = _species.Id;
            }
        }
        
        public Guid ThemeId { get; set; }
        private Theme _theme;
        //[JsonIgnore]
        public Theme Theme
        {
            get { return _theme; }
            set
            {
                _theme = value;
                if (_theme != null) ThemeId = _theme.Id;
            }
        }

        //[JsonIgnore]
        public List<StarSystem> KnownSystems { get; set; }
        public List<TaskForce> TaskForces { get; set; }
        public List<Commander> Commanders { get; set; }
    }
}
