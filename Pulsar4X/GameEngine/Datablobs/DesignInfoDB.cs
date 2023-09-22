using System;
using System.Collections.Generic;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Datablobs
{
    /// <summary>
    /// stores the Design entity for specific instances of a ship, component, or other designed entity. 
    /// </summary>
    public class DesignInfoDB : BaseDataBlob
    {
        internal Entity DesignEntity { get; private set; }

        internal List<IComponentDesignAttribute> DesignAttributes = new List<IComponentDesignAttribute>();

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
