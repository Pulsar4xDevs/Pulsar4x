using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Pulsar4X.Entities
{
    /// <summary>
    /// Ths class repesents a Gas Which can make upo part of aplanetes Atmosphere. 
    /// @note While we refere to the as gases they could be in a liquid state or even frozen on a planets surface (or both). It will depend on the planet.
    /// </summary>
    class AtmosphericGas
    {
        public string Name { get; set; }
        public string ChemicalSymbol { get; set; }
        public bool IsToxic { get; set; }
        public double BoilingPoint { get; set; }
        public double MeltingPoint { get; set; }

        /// <summary>
        /// A value repesenting the Greehouse effect this gas has (if any).
        ///  0 = Inert/No Effect
        ///  A negative number would be an Anti-Greenhouse gas.
        ///  A positive Number would be a Greenhouse gas.
        ///  The Magnitude of the number could be used to have different gases have a greater or lessser greenhouse effect.
        /// </summary>
        public double GreenhouseEffect { get; set; }                

        public AtmosphericGas()
        {
            Name = "BadGas";
        }

        public AtmosphericGas(string name, string chemicalSym, bool isToxic,
            double boilingPoint, double meltingPoint, double greenhouseEffect)
        {
            Name = name;
            ChemicalSymbol = chemicalSym;
            IsToxic = isToxic;
            BoilingPoint = boilingPoint;
            MeltingPoint = meltingPoint;
            GreenhouseEffect = greenhouseEffect;
        }

        // Create and populate a hard coded list of Atmospheric Gases. This is a hack, it shoul be loadded fom disk.
        ///< @todo Read AtmosphericGases in from json!
        private static BindingList<AtmosphericGas> _atmosphericGases = new BindingList<AtmosphericGas>()
        {
             new AtmosphericGas()
             { 
                 Name = "Hydrogen", 
                 ChemicalSymbol = "H",
                 IsToxic = false,
                 MeltingPoint = -259.16,
                 BoilingPoint = -252.879,
                 GreenhouseEffect = 1
             }
        };

        /// <summary>
        /// Static list that holds a reference to each instance/type of AtmosphericGas.
        /// </summary>
        public static BindingList<AtmosphericGas> AtmosphericGases
        {
            get { return _atmosphericGases; }
        }
        
    }
}
