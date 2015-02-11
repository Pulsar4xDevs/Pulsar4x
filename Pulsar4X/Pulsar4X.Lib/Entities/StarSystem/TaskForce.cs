using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Pulsar4X.Entities
{
    public class TaskForce
    {
        public Guid Id { get; set; }
        public Faction Faction { get; set; }
        public string Name { get; set; }
        public List<TaskGroupTN> Fleets { get; set; }
    }
}
