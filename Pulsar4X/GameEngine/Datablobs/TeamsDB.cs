using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.Engine;

namespace Pulsar4X.Datablobs
{
    /// <summary>
    /// TeamsDB defines this entity has being a team of scientists/spies/etc, which can be given orders (ex: Survey Mars)
    /// </summary>
    public class TeamsDB : BaseDataBlob
    {
        [JsonProperty]
        private int _teamSize;
        [JsonProperty]
        private object _teamTask;

        /// <summary>
        /// Determines how many Labs this team can manage
        /// </summary>
        /// TODO: Pre-release
        /// Ensure Property/Fields are consistant throughout all DB usage.
        /// Example: TransitableDB uses TeamSize { public get; internal set;}
        [PublicAPI]
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

        public TeamsDB() { }

        public TeamsDB(int teamSize = 0, object initialTask = null)
        {
            TeamSize = teamSize;
            TeamTask = initialTask;
        }

        public TeamsDB(TeamsDB teamsdb)
        {
            TeamSize = teamsdb.TeamSize;
            TeamTask = teamsdb.TeamTask;
        }
        public override object Clone()
        {
            return new TeamsDB(this);
        }
    }
}