using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public class InstallationsDB : BaseDataBlob
    {
        /// <summary>
        /// a dictionary of instalationtype, and the number of that specific type including partial instalations.
        /// </summary>
        public JDictionary<InstallationSD, float> Instalations { get; set; }



        public InstallationsDB()
        {
            Instalations = new JDictionary<InstallationSD, float>();
        }

        public InstallationsDB(InstallationsDB db)
        {
            Instalations = new JDictionary<InstallationSD, float>(db.Instalations);
        }

        public override object Clone()
        {
            return new InstallationsDB(this);
        }
    }
}