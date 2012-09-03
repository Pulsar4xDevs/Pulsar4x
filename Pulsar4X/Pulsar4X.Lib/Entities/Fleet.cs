using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Pulsar4X.Entities
{
    public class Fleet
    {
        public Guid Id { get; set; }
        public Guid FactionId { get; set; }
        //[JsonIgnore]
        private Faction _faction;
        //[JsonIgnore]
        public Faction Faction
        {
            get { return _faction; }
            set
            {
                _faction = value;
                if (_faction != null) FactionId = _faction.Id;
            }
        }
        
        public Guid TaskForceId { get; set; }
        //[JsonIgnore]
        private TaskForce _taskForce;
        //[JsonIgnore]
        public TaskForce TaskForce
        {
            get { return _taskForce; }
            set
            {
                _taskForce = value;
                if (_taskForce != null) TaskForceId = _taskForce.Id;
            }
        }
        
        public string Name { get; set; }
        public long XSystem { get; set; }
        public long YSystem { get; set; }
        public int CurrentSpeed { get; set; }
        public int MaxSpeed { get; set; }
        public List<Ship> Ships { get; set; }
    }
}
