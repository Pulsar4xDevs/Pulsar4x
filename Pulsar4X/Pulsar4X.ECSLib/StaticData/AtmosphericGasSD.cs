using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    public struct AtmosphericGasSD
    {
        public string Name;
        public string ChemicalSymbol;
        public bool IsToxic;
        public double BoilingPoint;
        public double MeltingPoint;

        /// <summary>
        /// A value repesenting the Greehouse effect this gas has (if any).
        ///  0 = Inert/No Effect
        ///  A negative number would be an Anti-Greenhouse gas.
        ///  A positive Number would be a Greenhouse gas.
        ///  The Magnitude of the number could be used to have different gases have a greater or lessser greenhouse effect.
        /// </summary>
        public double GreenhouseEffect;

    }
}
