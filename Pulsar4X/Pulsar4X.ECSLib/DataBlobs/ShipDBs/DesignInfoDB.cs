using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// stores the Design entity for specific instances of a ship, component, or other designed entity. 
    /// </summary>
    public class DesignInfoDB : BaseDataBlob
    {
        internal Entity DesignEntity { get; private set; }


        public DesignInfoDB() { }

        public DesignInfoDB(Entity designEntity)
        {
            DesignEntity = designEntity;
        }

        public DesignInfoDB(DesignInfoDB db)
        {
            DesignEntity = db.DesignEntity;
        }

        public override object Clone()
        {
            return new DesignInfoDB(this);
        }
    }
}
