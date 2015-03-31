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
        public int Entity { get; set; }

        public ReaderWriterLockSlim LockObject = new ReaderWriterLockSlim();
    }
}
