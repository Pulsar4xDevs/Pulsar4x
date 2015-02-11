using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.Entities
{
    public class WreckTN : StarSystemEntity
    {
        /// <summary>
        /// This "constructs" the wreck itself. Later component definitions, mineral counts, and wealth will be calculated here.
        /// </summary>
        /// <param name="Ship">Ship from which this wreck originated</param>
        public WreckTN(ShipTN Ship)
        {
        }
    }
}
