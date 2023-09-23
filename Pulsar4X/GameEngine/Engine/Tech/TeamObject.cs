using System;
using Newtonsoft.Json;
using Pulsar4X.Interfaces;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Engine
{
    public class TeamObject : ICargoable
    {
        public TeamTypes TeamType { get; protected set; }
        public string LeaderName;
        public int Age;

        [JsonProperty]
        private int _teamSize;
        [JsonProperty]
        private object _teamTask;

        /// <summary>
        /// Determines how many Labs this team can manage
        /// </summary>
        public int TeamSize
        {
            get { return _teamSize; }
            internal set { _teamSize = value; }
        }

        /// <summary>
        /// not sure if this should be a blob, entity or guid. and maybe a queue as well.
        /// </summary>
        /// TODO: Communications Review
        /// Detemine team orders system
        [PublicAPI]
        public object TeamTask
        {
            get { return _teamTask; }
            internal set { _teamTask = value; }
        }

        public TeamObject() { }

        public TeamObject(int teamSize = 0, object initialTask = null)
        {
            TeamSize = teamSize;
            TeamTask = initialTask;
        }

        public TeamObject(TeamObject teamsdb)
        {
            TeamSize = teamsdb.TeamSize;
            TeamTask = teamsdb.TeamTask;
        }

        public  object Clone()
        {
            return new TeamObject(this);
        }

        public string UniqueID { get; } = Guid.NewGuid().ToString();
        public string Name
        {
            get { return LeaderName; }
        }
        public string CargoTypeID { get; set; } = "passenger-storage";
        public long MassPerUnit
        {
            get { return Convert.ToInt64(_teamSize * (long)100); }
        }

        public double VolumePerUnit
        {
            get { return 0.065 * _teamSize; }
        }

        public double Density
        {
            get { return _teamSize * 985.0; } //avg density of a human.
        }
    }
}