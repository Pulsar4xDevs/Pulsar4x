using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class  EntityResearchDB : BaseDataBlob, IAbilityDescription
    {
        public Dictionary<ComponentInstance, int> Labs = new Dictionary<ComponentInstance, int>();
        public EntityResearchDB()
        {
        }

        public EntityResearchDB(EntityResearchDB db)
        {

        }

        public override object Clone()
        {
            return new EntityResearchDB(this);
        }

        public string AbilityName()
        {
            return "Research Point Production";
        }

        public string AbilityDescription()
        {
            int labcount = Labs.Count;
            int total = 0;
            string desc = "";
            foreach (var line in Labs)
            {
                total += line.Value;
                string labname = line.Key.Name;
                desc += labname + " : " + line.Value + "\n";
            }

            desc += labcount.ToString() + " Labs, generating a total of " + total.ToString() + " Research Points";
            return desc;
        }
    }
}