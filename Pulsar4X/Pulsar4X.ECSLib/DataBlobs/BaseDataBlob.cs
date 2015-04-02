using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib.DataBlobs
{
    public abstract class BaseDataBlob
    {
        public virtual EntityManager ContainingManager { get; set; }
        public virtual int EntityID { get; set; }
        public virtual Guid EntityGuid { get; set; }

        public readonly object LockObject = new object();
    }
}
