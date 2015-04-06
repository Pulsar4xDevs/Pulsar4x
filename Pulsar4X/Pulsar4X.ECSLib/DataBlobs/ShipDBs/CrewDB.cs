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

    }
}