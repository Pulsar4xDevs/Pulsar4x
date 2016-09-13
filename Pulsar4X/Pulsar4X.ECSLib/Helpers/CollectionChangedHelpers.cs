using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    internal struct PostStateForCollectionChange
    {
        internal object sender;
        internal NotifyCollectionChangedEventArgs e;
    }
}
