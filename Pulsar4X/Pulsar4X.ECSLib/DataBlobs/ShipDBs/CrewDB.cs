namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// Info about the ships crew and command centres (bridge/flag bridge/Automatic computer) on a ship.
    /// </summary>
    public class CrewDB : BaseDataBlob
    {
        public double DeploymentTime { get; set; } // in months
        public int CrewBerths { get; set; }
        public int RequiredCrew { get; set; }
        public int CurrentCrew { get; set; }
        public int SpareBerths { get; set; }
        public int CryoCrewberths { get; set; }
        public int CrewInCryo { get; set; }

        public int CrewGrade { get; set; }

        public bool HasBridge { get; set; }
        public bool HasFlagBridge { get; set; }

        public CrewDB()
        {
        }

        public CrewDB(CrewDB crewDB)
        {
            DeploymentTime = crewDB.DeploymentTime;
            CrewBerths = crewDB.CrewBerths;
            RequiredCrew = crewDB.RequiredCrew;
            CurrentCrew = crewDB.CurrentCrew;
            SpareBerths = crewDB.SpareBerths;
            CryoCrewberths = crewDB.CryoCrewberths;
            CrewInCryo = crewDB.CrewInCryo;
            CrewGrade = crewDB.CrewGrade;

            //Not sure
            HasBridge = crewDB.HasBridge;
            HasFlagBridge = crewDB.HasFlagBridge;
        }

        public override object Clone()
        {
            return new CrewDB(this);
        }
    }
}