using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public struct ConstructionJob
    {        
        public Guid Type;
        public float ItemsRemaining;
        public PercentValue PriorityPercent;
    }

    public class InstallationsDB : BaseDataBlob
    {
        /// <summary>
        /// a dictionary of installationtype, and the number of that specific type including partial instalations.
        /// </summary>
        public JDictionary<InstallationSD, float> Installations { get; set; }

        /// <summary>
        /// guid for installationtype, and a weighted list for priority and amount remaining.
        /// </summary>
        public List<ConstructionJob> InstallationJobs { get; set; }
        public List<ConstructionJob> OrdnanceJobs { get; set; }
        public List<ConstructionJob> FigherJobs { get; set; }

        public InstallationsDB()
        {
            Installations = new JDictionary<InstallationSD, float>();
        }

        public InstallationsDB(InstallationsDB db)
        {
            Installations = new JDictionary<InstallationSD, float>(db.Installations);
        }

        public override object Clone()
        {
            return new InstallationsDB(this);
        }
    }
}