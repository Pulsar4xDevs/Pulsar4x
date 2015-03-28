using System;

namespace Pulsar4X.ECSLib.Helpers
{
    /// <summary>
    /// Class to contain interrupt information.
    /// </summary>
    public class Interrupt
    {
        public bool StopProcessing;
        public Type RequestingProcessor;
        public string Reason;
    }
}
