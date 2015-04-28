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

        /// <summary>
        /// This should maybe be in the processor, and also needs to check whether the pop requirements are met. 
        /// </summary>
        public int TotalAbilityofType(InstallationAbilityType type)
        {            
            int totalAbilityValue = 0;
            foreach (KeyValuePair<InstallationSD, float> kvp in Instalations.Where(item => item.Key.AbilityType.ContainsKey(type)))
            {
                totalAbilityValue += kvp.Key.AbilityType[type] * (int)kvp.Value; //the decimal is an incomplete instalation, so ignore it. 
            }
            return totalAbilityValue;           
        }

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