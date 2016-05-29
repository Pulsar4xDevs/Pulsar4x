using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    abstract class BaseOrderDB : BaseDataBlob
    {
        // Orders can be given a delay before being executed
        public long DelayTime { get; internal set; }

        abstract public bool isValid();
    }
}
