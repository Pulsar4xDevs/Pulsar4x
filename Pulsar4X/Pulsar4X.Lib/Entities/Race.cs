using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.Entities
{
    public class Race
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public Species Species { get; set;
        }
        public List<Population> Populations { get; set; } 
        public List<TaskForce> TaskForces { get; set; } 


    }
}
