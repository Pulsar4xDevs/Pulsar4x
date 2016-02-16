using System;

namespace Pulsar4X.ECSLib
{
    public class PulseInterrupt
    {
        [PublicAPI]
        public Type RequestingProcessor { get; internal set; }

        [PublicAPI]
        public string Reason { get; internal set; }
    }

    public class SubpulseLimit
    {
        [PublicAPI]
        public int MaxSeconds { get; internal set; }

        [PublicAPI]
        public Type RequestingProcessor { get; internal set; }

        [PublicAPI]
        public string Reason { get; internal set; }

        internal SubpulseLimit()
        {
            MaxSeconds = int.MaxValue;
            RequestingProcessor = null;
            Reason = string.Empty;
        }
    }
}
