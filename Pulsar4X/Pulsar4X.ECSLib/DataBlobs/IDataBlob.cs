using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib.DataBlobs
{
    public interface IDataBlob
    {
        int Entity { get; }

        IDataBlob UpdateEntityID(int newEntityID);
    }
}
