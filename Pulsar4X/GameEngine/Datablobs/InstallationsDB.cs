using System;
using System.Collections.Generic;

namespace Pulsar4X.Datablobs
{
    //public struct ConstructJob
    //{        
    //    public ID Type;
    //    public float ItemsRemaining;
    //    public PercentValue PriorityPercent;
    //    public Dictionary<ID, int> RawMaterialsRemaining;
    //    public int BuildPointsRemaining;
    //    public int BuildPointsPerItem;
    //}

    /// <summary>
    /// this is used to turn installations on and off, 
    /// and also used by the Processor to check pop requirements.
    /// </summary>
    public struct InstallationEmployment
    {
        public string Type;
        public bool Enabled;
    }

    public class InstallationsDB : BaseDataBlob
    {
        /// <summary>
        /// a dictionary of installationtype, and the number of that specific type including partial installations.
        /// </summary>
        public Dictionary<string, float> Installations { get; set; }

        public Dictionary<string,int> WorkingInstallations { get; set; }

        public List<InstallationEmployment> EmploymentList { get; set; } 
        /// <summary>
        /// list of ConstructJob Structs.
        /// </summary>
        //public List<ConstructJob> InstallationJobs { get; set; }
        //public List<ConstructJob> OrdnanceJobs { get; set; }
        //public List<ConstructJob> ComponentJobs { get; set; }
        //public List<ConstructJob> FigherJobs { get; set; }
        //public List<ConstructJob> RefineryJobs { get; set; }
        public InstallationsDB()
        {
            Installations = new Dictionary<string, float>();
            WorkingInstallations = new Dictionary<string, int>();
            EmploymentList = new List<InstallationEmployment>();
            //InstallationJobs = new List<ConstructJob>();
            //ComponentJobs = new List<ConstructJob>(); 
            //OrdnanceJobs = new List<ConstructJob>();
            //FigherJobs = new List<ConstructJob>();
            //RefineryJobs = new List<ConstructJob>();
        }

        public InstallationsDB(InstallationsDB db)
        {
            Installations = new Dictionary<string, float>(db.Installations);
            WorkingInstallations = new Dictionary<string, int>(db.WorkingInstallations);
            EmploymentList = new List<InstallationEmployment>(db.EmploymentList);
            //InstallationJobs = new List<ConstructJob>(db.InstallationJobs);
            //ComponentJobs = new List<ConstructJob>(db.ComponentJobs);
            //OrdnanceJobs = new List<ConstructJob>(db.OrdnanceJobs);
            //FigherJobs = new List<ConstructJob>(db.FigherJobs);
            //RefineryJobs = new List<ConstructJob>(db.RefineryJobs);
        }

        public override object Clone()
        {
            return new InstallationsDB(this);
        }
    }
}