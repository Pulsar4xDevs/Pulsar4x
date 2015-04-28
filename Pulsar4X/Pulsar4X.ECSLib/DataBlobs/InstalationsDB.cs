using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public class InstalationsDB : BaseDataBlob
    {
        /// <summary>
        /// a dictionary of instalationtype, and the number of that specific type including partial instalations.
        /// </summary>
        public JDictionary<InstalationSD, float> Instalations { get; set; }

        /// <summary>
        /// This should maybe be in the processor, and also needs to check whether the pop requirements are met. 
        /// </summary>
        public int TotalAbilityofType(InstalationAbilityType type)
        {            
            int totalAbilityValue = 0;
            foreach (KeyValuePair<InstalationSD, float> kvp in Instalations.Where(item => item.Key.AbilityType.ContainsKey(type)))
            {
                totalAbilityValue += kvp.Key.AbilityType[type] * (int)kvp.Value; //the decimal is an incomplete instalation, so ignore it. 
            }
            return totalAbilityValue;           
        }

        public InstalationsDB()
        {
            Instalations = new JDictionary<InstalationSD, float>();
        }

        public InstalationsDB(InstalationsDB db)
        {
            Instalations = new JDictionary<InstalationSD, float>(db.Instalations);
        }

        public override object Clone()
        {
            return new InstalationsDB(this);
        }
    }
}