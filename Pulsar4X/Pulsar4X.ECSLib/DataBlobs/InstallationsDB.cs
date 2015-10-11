using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    //public struct ConstructionJob
    //{        
    //    public Guid Type;
    //    public float ItemsRemaining;
    //    public PercentValue PriorityPercent;
    //    public JDictionary<Guid, int> RawMaterialsRemaining;
    //    public int BuildPointsRemaining;
    //    public int BuildPointsPerItem;
    //}

    /// <summary>
    /// this is used to turn installations on and off, 
    /// and also used by the Processor to check pop requirements.
    /// </summary>
    public struct InstallationEmployment
    {
        public Guid Type;
        public bool Enabled;
    }

    public class InstallationsDB : BaseDataBlob
    {
        /// <summary>
        /// a dictionary of installationtype, and the number of that specific type including partial installations.
        /// </summary>
        public JDictionary<Guid, float> Installations { get; set; }

        public JDictionary<Guid,int> WorkingInstallations { get; set; }

        public List<InstallationEmployment> EmploymentList { get; set; } 
        /// <summary>
        /// list of ConstructionJob Structs.
        /// </summary>
        //public List<ConstructionJob> InstallationJobs { get; set; }
        //public List<ConstructionJob> OrdnanceJobs { get; set; }
        //public List<ConstructionJob> ComponentJobs { get; set; }
        //public List<ConstructionJob> FigherJobs { get; set; }
        //public List<ConstructionJob> RefineryJobs { get; set; }
        public InstallationsDB()
        {
            Installations = new JDictionary<Guid, float>();
            WorkingInstallations = new JDictionary<Guid, int>();
            EmploymentList = new List<InstallationEmployment>();
            //InstallationJobs = new List<ConstructionJob>();
            //ComponentJobs = new List<ConstructionJob>(); 
            //OrdnanceJobs = new List<ConstructionJob>();
            //FigherJobs = new List<ConstructionJob>();
            //RefineryJobs = new List<ConstructionJob>();
        }

        public InstallationsDB(InstallationsDB db)
        {
            Installations = new JDictionary<Guid, float>(db.Installations);
            WorkingInstallations = new JDictionary<Guid, int>(db.WorkingInstallations);
            EmploymentList = new List<InstallationEmployment>(db.EmploymentList);
            //InstallationJobs = new List<ConstructionJob>(db.InstallationJobs);
            //ComponentJobs = new List<ConstructionJob>(db.ComponentJobs);
            //OrdnanceJobs = new List<ConstructionJob>(db.OrdnanceJobs);
            //FigherJobs = new List<ConstructionJob>(db.FigherJobs);
            //RefineryJobs = new List<ConstructionJob>(db.RefineryJobs);
        }

        public override object Clone()
        {
            return new InstallationsDB(this);
        }
    }
}