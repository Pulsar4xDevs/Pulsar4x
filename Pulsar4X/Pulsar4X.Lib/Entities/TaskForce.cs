using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.Entities
{
    public class TaskForce
    {
        public Guid Id { get; set; }
        public Race Race { get; set; }
        public string Name { get; set; }

        public List<Fleet> Fleets { get; set; } 
    }
}
