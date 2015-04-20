using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public class TeamsDB : BaseDataBlob
    {

        public int Teamsize { get; set; }

        /// <summary>
        /// not sure if this should be a blob, entity or guid. and maybe a queue as well. 
        /// </summary>
        public object TeamTask { get; set; }

        public TeamsDB() { } // need by json

        public TeamsDB(int maxTeamsize)
        {
            Teamsize = 0;
            TeamTask = null;
        }

        public TeamsDB(TeamsDB teamsdb)
        {
            Teamsize = teamsdb.Teamsize;
            TeamTask = teamsdb.TeamTask;
        }
        public override object Clone()
        {
            return new TeamsDB(this);
        }
    }
}