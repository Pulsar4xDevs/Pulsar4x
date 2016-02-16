using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public class DB : BaseDataBlob
    {

        public DB()
        {

        }

        public DB(DB db)
        {

        }

        public override object Clone()
        {
            return new DB(this);
        }
    }
}