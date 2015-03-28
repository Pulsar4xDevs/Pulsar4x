using System;

namespace Pulsar4X.ECSLib.Helpers
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
