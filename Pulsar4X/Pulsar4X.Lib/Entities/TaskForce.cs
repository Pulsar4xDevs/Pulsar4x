using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.Entities
{
    public class TaskForce
    {
        public int Id { get; set; }
        public int RaceId { get; set; }
        public string Name { get; set; }

        public List<Fleet> Fleets { get; set; } 
    }
}
