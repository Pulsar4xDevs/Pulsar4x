using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class  EntityResearchDB : BaseDataBlob
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
    }
}