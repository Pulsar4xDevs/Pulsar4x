using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X
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
