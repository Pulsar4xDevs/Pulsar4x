using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;

namespace Pulsar4X.Datablobs
{
    public class TeamsHousedDB : BaseDataBlob
    {
        public Dictionary<TeamTypes, List<TeamObject>> TeamsByType = new Dictionary<TeamTypes, List<TeamObject>>();


        [JsonConstructor]
        internal TeamsHousedDB()
        {
        }

        internal TeamsHousedDB(TeamsHousedDB db)
        {
            TeamsByType = new Dictionary<TeamTypes, List<TeamObject>>(db.TeamsByType);
        }

        public void AddTeam(TeamObject team)
        {
            if (!TeamsByType.ContainsKey(team.TeamType))
            {
                TeamsByType.Add(team.TeamType, new List<TeamObject>());
            }
            TeamsByType[team.TeamType].Add(team);
        }

        public void RemoveTeam(TeamObject team)
        {
            TeamsByType[team.TeamType].Remove(team);
        }

        public override object Clone()
        {
            return new TeamsHousedDB(this);
        }
    }
}