using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public class  ColonyResearchDB : BaseDataBlob
    {




        public ColonyResearchDB()
        {
        }

        public ColonyResearchDB(ColonyResearchDB db)
        {

        }

        public override object Clone()
        {
            return new ColonyResearchDB(this);
        }
    }
}