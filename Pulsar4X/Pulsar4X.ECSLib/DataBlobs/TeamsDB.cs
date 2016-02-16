using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class TeamsDB : BaseDataBlob
    {
        [JsonProperty]
        private int _teamSize;
        [JsonProperty]
        private object _teamTask;

        [PublicAPI]
        public int TeamSize
        {
            get { return _teamSize; }
            internal set { _teamSize = value; }
        }

        /// <summary>
        /// not sure if this should be a blob, entity or guid. and maybe a queue as well. 
        /// </summary>
        [PublicAPI]
        public object TeamTask
        {
            get { return _teamTask; }
            internal set { _teamTask = value; }
        }

        public TeamsDB() { } // need by json

        public TeamsDB(int maxTeamsize)
        {
            TeamSize = 0;
            TeamTask = null;
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