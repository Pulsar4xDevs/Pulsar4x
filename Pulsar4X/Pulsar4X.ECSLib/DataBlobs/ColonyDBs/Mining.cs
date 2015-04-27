using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public class MineingDB : BaseDataBlob
    {
        public JDictionary<InstalationSD, int> MineingInstalations { get; set; }

        /// <summary>
        /// This should maybe be in the processor, and also needs to check whether the pop requirements are met. 
        /// </summary>
        public int TotalMineingAbility
        {
            get
            {
                int totalAbilityValue = 0;
                foreach (KeyValuePair<InstalationSD, int> kvp in MineingInstalations.Where(item => item.Key.AbilityType == InstalationAbilityType.Mine)) //just a double check on the type
                {
                    totalAbilityValue += kvp.Key.AbilityAmount * kvp.Value;
                }
                return totalAbilityValue;
            }
        }

        public MineingDB()
        {
            MineingInstalations = new JDictionary<InstalationSD, int>();
        }

        public MineingDB(MineingDB db)
        {
            MineingInstalations = new JDictionary<InstalationSD, int>(db.MineingInstalations);
        }

        public override object Clone()
        {
            return new MineingDB(this);
        }
    }
}