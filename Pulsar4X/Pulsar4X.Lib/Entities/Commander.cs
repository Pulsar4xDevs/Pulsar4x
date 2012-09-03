using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Pulsar4X.Entities
{
    public class Commander
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
        
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName
        {
            get { return string.Format("{0} {1}", FirstName, LastName); }
        }
        
        public CommanderTypes CommanderType { get; set; }
        public int Rank { get; set; }
        public string RankName { get; set; }
    }
}
