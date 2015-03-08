using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.Helpers
{
    public class SubpulseRequest
    {
        public int MaxSeconds;
        public Type RequestingProcessor;
        public string Reason;
    }

    class SubpulseData
    {
        public int DeltaSeconds;
    }
}
