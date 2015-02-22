using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Pulsar4X.Helpers.GameMath;

namespace Pulsar4X.Entities
{
    /// <summary>
    /// Ths class repesents a Gas Which can make upo part of aplanetes Atmosphere. 
    /// @note While we refere to the as gases they could be in a liquid state or even frozen on a planets surface (or both). It will depend on the planet.
    /// </summary>
    public class AtmosphericGas
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

        /// <summary>
        /// Create and populate a hard coded Weighted list of Atmospheric Gases. This is a hack, it shoul be loadded fom disk.
        /// @todo Read AtmosphericGases in from json!
        /// </summary>
        private static WeightedList<AtmosphericGas> _atmosphericGases = new WeightedList<AtmosphericGas>()
        {
             { 50, new AtmosphericGas()
             { 
                 Name = "Hydrogen", 
                 ChemicalSymbol = "H",
                 IsToxic = false,
                 MeltingPoint = -259.16,
                 BoilingPoint = -252.879,
                 GreenhouseEffect = 1
             }},

             { 50, new AtmosphericGas()
             { 
                 Name = "Helium", 
                 ChemicalSymbol = "He",
                 IsToxic = false,
                 MeltingPoint = -272.20,
                 BoilingPoint = -268.928,
                 GreenhouseEffect = 0
             }},

             { 10, new AtmosphericGas()
             { 
                 Name = "Methane", 
                 ChemicalSymbol = "CH4",
                 IsToxic = true,
                 MeltingPoint = -182.5,
                 BoilingPoint = -161.49,
                 GreenhouseEffect = 1
             }},

             { 10, new AtmosphericGas()
             { 
                 Name = "Water", 
                 ChemicalSymbol = "H2O",
                 IsToxic = false,
                 MeltingPoint = 0,
                 BoilingPoint = 100,
                 GreenhouseEffect = 1
             }},

             { 10, new AtmosphericGas()
             { 
                 Name = "Ammonia", 
                 ChemicalSymbol = "NH3",
                 IsToxic = true,
                 MeltingPoint = -77.73,
                 BoilingPoint = -33.34,
                 GreenhouseEffect = 1
             }},

             { 5, new AtmosphericGas()
             { 
                 Name = "Neon", 
                 ChemicalSymbol = "Ne",
                 IsToxic = false,
                 MeltingPoint = -248.59,
                 BoilingPoint = -246.046,
                 GreenhouseEffect = 0
             }},

             { 50, new AtmosphericGas()
             { 
                 Name = "Nitrogen", 
                 ChemicalSymbol = "N",
                 IsToxic = false,
                 MeltingPoint = -210.00,
                 BoilingPoint = -195.795,
                 GreenhouseEffect = 0
             }},

             { 10, new AtmosphericGas()
             { 
                 Name = "Carbon monoxide", 
                 ChemicalSymbol = "CO",
                 IsToxic = true,
                 MeltingPoint = -205.02,
                 BoilingPoint = -191.5,
                 GreenhouseEffect = 1
             }},

             { 1, new AtmosphericGas()
             { 
                 Name = "Nitrogen oxide", 
                 ChemicalSymbol = "NO",
                 IsToxic = false,
                 MeltingPoint = -164,
                 BoilingPoint = -152,
                 GreenhouseEffect = 1
             }},

             { 25, new AtmosphericGas()
             { 
                 Name = "Oxygen", 
                 ChemicalSymbol = "O",
                 IsToxic = false,
                 MeltingPoint = -218.79,
                 BoilingPoint = -182.962,
                 GreenhouseEffect = 0
             }},

             { 1, new AtmosphericGas()
             { 
                 Name = "Hydrogen sulfide", 
                 ChemicalSymbol = "H2S",
                 IsToxic = true,
                 MeltingPoint = -82,
                 BoilingPoint = -60,
                 GreenhouseEffect = 1
             }},

             { 10, new AtmosphericGas()
             { 
                 Name = "Argon", 
                 ChemicalSymbol = "Ar",
                 IsToxic = false,
                 MeltingPoint = -189.34,
                 BoilingPoint = -185.848,
                 GreenhouseEffect = 0
             }},

             { 15, new AtmosphericGas()
             { 
                 Name = "Carbon dioxide", 
                 ChemicalSymbol = "CO2",
                 IsToxic = false,
                 MeltingPoint = -56.6,
                 BoilingPoint = -56.6,  // no boiling point on Wikipedia!!
                 GreenhouseEffect = 1
             }},

             { 1, new AtmosphericGas()
             { 
                 Name = "Nitrogen dioxide", 
                 ChemicalSymbol = "NO2",
                 IsToxic = false,
                 MeltingPoint = -11.2,
                 BoilingPoint = 21.2,
                 GreenhouseEffect = 1
             }},

             { 1, new AtmosphericGas()
             { 
                 Name = "Sulfur dioxide", 
                 ChemicalSymbol = "SO2",
                 IsToxic = false,
                 MeltingPoint = -72,
                 BoilingPoint = -10,
                 GreenhouseEffect = 1
             }},

             { 1, new AtmosphericGas()
             { 
                 Name = "Chlorine", 
                 ChemicalSymbol = "Cl",
                 IsToxic = true,
                 MeltingPoint = -101.5,
                 BoilingPoint = -34.04,
                 GreenhouseEffect = 1
             }},

             { 1, new AtmosphericGas()
             { 
                 Name = "Fluorine", 
                 ChemicalSymbol = "F",
                 IsToxic = true,
                 MeltingPoint = -219.67,
                 BoilingPoint = -188.11,
                 GreenhouseEffect = 1
             }},

             { 1, new AtmosphericGas()
             { 
                 Name = "Bromine", 
                 ChemicalSymbol = "Br",
                 IsToxic = true,
                 MeltingPoint = -7.2,
                 BoilingPoint = 58.8,
                 GreenhouseEffect = 1
             }},

             { 1, new AtmosphericGas()
             { 
                 Name = "Iodine", 
                 ChemicalSymbol = "I",
                 IsToxic = true,
                 MeltingPoint = 113.7,
                 BoilingPoint = 184.3,
                 GreenhouseEffect = 1
             }},

             { 0, new AtmosphericGas()
             { 
                 Name = "Safe Greenhouse Gas", 
                 ChemicalSymbol = "SGG",
                 IsToxic = false,
                 MeltingPoint = 0,
                 BoilingPoint = 100,
                 GreenhouseEffect = 1
             }},

             { 0, new AtmosphericGas()
             { 
                 Name = "Anti-Greenhouse Gas", 
                 ChemicalSymbol = "AGG",
                 IsToxic = false,
                 MeltingPoint = 0,
                 BoilingPoint = 100,
                 GreenhouseEffect = -1
             }}
        };

        /// <summary>
        /// Static list that holds a reference to each instance/type of AtmosphericGas.
        /// </summary>
        public static WeightedList<AtmosphericGas> AtmosphericGases
        {
            get { return _atmosphericGases; }
        }
        
    }
}
