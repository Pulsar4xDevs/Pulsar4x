using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.Helpers
{
    /// <summary>
    /// Class to contain information about the current Subpulse Limit.
    /// </summary>
    public class SubpulseLimitRequest
    {
        public int MaxSeconds;
        public Type RequestingProcessor;
        public string Reason;
    }
}
