using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public class InstallationsDB : BaseDataBlob
    {
        /// <summary>
        /// a dictionary of installationtype, and the number of that specific type including partial instalations.
        /// </summary>
        public JDictionary<InstallationSD, float> Installations { get; set; }

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