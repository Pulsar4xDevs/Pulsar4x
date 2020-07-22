using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{

    public enum TeamTypes
    {
        Science,
        ArmyCdr,
        SpaceNavyCdr,
        CivAdmin,
    }

    public class TeamsHousedDB : BaseDataBlob
    {
        public Dictionary<TeamTypes, List< TeamObject>> TeamsByType = new Dictionary<TeamTypes, List<TeamObject>>();


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

        public Guid ID { get; } = Guid.NewGuid();
        public string Name
        {
            get { return LeaderName; }
        }
        public Guid CargoTypeID { get; set; } = new Guid("7e08074d-682c-4452-a45a-dc97968f53ca");
        public int MassPerUnit
        {
            get { return _teamSize * 100; }
        }

        public double VolumePerUnit
        {
            get { return 0.065 * _teamSize; }
        }

        public double Density
        {
            get { return _teamSize * 985; } //avg density of a human. 
        }
    }

    public class Scientist : TeamObject
    {
        
        /// <summary>
        /// Bonuses that this scentist imparts.
        /// </summary>
        [JsonProperty]
        public Dictionary<ResearchCategories, float> Bonuses { get; internal set; }

        /// <summary>
        /// Max number of labs this scientist can manage.
        /// </summary>
        [JsonProperty]
        public byte MaxLabs { get; internal set; }

        /// <summary>
        /// Current number of labs assigned to this scientist.
        /// </summary>
        [JsonProperty]
        public byte AssignedLabs { get; internal set; }

        /// <summary>
        /// Queue of projects currently being worked on by this scientist.
        /// </summary>
        public List<(Guid techID, bool cycle)> ProjectQueue { get; internal set; } = new List<(Guid,bool)>();

        public new string Name { get; set; }

        public Scientist()
        {
            TeamType = TeamTypes.Science;
        }

        public Scientist(Dictionary<ResearchCategories,float> bonuses, byte maxLabs )
        {
            Bonuses = bonuses;
            MaxLabs = maxLabs;
            AssignedLabs = 0;
            ProjectQueue = new List<(Guid, bool)>();
        }

        public Scientist(Scientist dB)
        {
            Bonuses = new Dictionary<ResearchCategories, float>(dB.Bonuses);
            MaxLabs = dB.MaxLabs;
            AssignedLabs = dB.AssignedLabs;
            ProjectQueue = dB.ProjectQueue;
        }

        public object Clone()
        {
            return new Scientist(this);
        }
    }
}