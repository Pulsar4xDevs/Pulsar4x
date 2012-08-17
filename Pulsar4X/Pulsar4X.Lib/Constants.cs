using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.Entities;

namespace Pulsar4X
{
    /// <summary>
    /// Container class for all the constants used elsewhere in the game
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Constants dealing with units and measurements
        /// </summary>
        public static class Units
        {
            /// <summary>
            /// Mass in grams of one solar mass
            /// </summary>
            public const double SOLAR_MASS_IN_GRAMS = Constants.Sol.Sun.MASS_IN_GRAMS;

            public const double SOLAR_MASS_IN_KILOGRAMS = Constants.Sol.Sun.MASS_IN_KILOGRAMS;

            public const double EARTH_MASS_IN_GRAMS = Constants.Sol.Earth.MASS_IN_GRAMS;

            public const double SUN_MASS_IN_EARTH_MASSES = SOLAR_MASS_IN_GRAMS / EARTH_MASS_IN_GRAMS;

            public const double CM_PER_AU = 1.495978707E13;

            public const double CM_PER_KM = 1.0E5;

            public const double CM_PER_METER = 100.0;

            public const double KM_PER_LIGHTYEAR = 9460730472580.8;

            public const double KM_PER_AU = CM_PER_AU / CM_PER_KM;

            public const double SECONDS_PER_HOUR = 3600.0;

            public const double MILLIBARS_PER_BAR = 1000.00;

            /// <summary>
            /// units of dyne cm2/gram2	
            /// </summary>
            public const double GRAV_CONSTANT = 6.672E-8;

            /// <summary>
            /// units: g*m2/(sec2*K*mol)
            /// </summary>
            public const double MOLAR_GAS_CONST = 8314.41;	

            /// <summary>
            ///  ratio of esc vel to RMS vel
            /// </summary>
            public const double GAS_RETENTION_THRESHOLD = 6.0;

            /// <summary>
            /// Number of radians in 360 degrees
            /// </summary>
            public const double RADIANS_PER_ROTATION = 2.0 * Math.PI;

            public const double ECCENTRICITY_COEFF = 0.077;	// Dole's was 0.077	

            public const double PROTOPLANET_MASS = 1.0E-15;	// Units of solar masses

            /// <summary>
            /// Km2/kg 
            /// </summary>
            public const double CLOUD_COVERAGE_FACTOR = 1.839E-8;
            public const double ICE_ALBEDO = 0.7;
            public const double CLOUD_ALBEDO = 0.52;
            public const double GAS_GIANT_ALBEDO = 0.5;
            public const double AIRLESS_ICE_ALBEDO = 0.5;
            public const double EARTH_ALBEDO = 0.3;	
            public const double GREENHOUSE_TRIGGER_ALBEDO = 0.20;
            public const double ROCKY_ALBEDO = 0.15;
            public const double ROCKY_AIRLESS_ALBEDO = 0.07;
            public const double WATER_ALBEDO = 0.04;

            public const double INCREDIBLY_LARGE_NUMBER = 9.9999E37;
        }

        /// <summary>
        /// Constants detailing our solar system
        /// </summary>
        public static class Sol
        {
            /// <summary>
            /// Constants involving our Sun
            /// </summary>
            public class Sun
            {
                public const double MASS_IN_GRAMS = 1.989E33;

                /// <summary>
                /// Units of kg
                /// </summary>
                public const double MASS_IN_KILOGRAMS = 1.989E30;		 	
                                                                             
                public const double MASS_IN_EARTH_MASSES = 332775.64;
            }

            /// <summary>
            /// Constants involving Earth
            /// </summary>
            public class Earth
            {
                public const double MASS_IN_GRAMS = 5.977E27;

                /// <summary>
                /// Density in grams per cubic centimetre
                /// </summary>
                public const double DENSITY = 5.52;

                /// <summary>
                /// Radius in centimeters
                /// </summary>
                public const double RADIUS = 6.378E8;

                public const double RADIUS_IN_KM = 6378.0;

                /// <summary>
                /// Units of radians/sec/year
                /// </summary>
                public const double CHANGE_IN_ANG_VEL = -1.3E-15;

                /// <summary>
                /// Units of cm/sec2
                /// </summary>
                public const double ACCELERATION = 980.7;

                public const double DAYS_IN_A_YEAR = 365.256;

                /// <summary>
                /// Units of degrees	
                /// </summary>
                public const double AXIAL_TILT = 23.4;

                public const double ALBEDO = 0.3;

                /// <summary>
                /// Units of degrees Kelvin
                /// </summary>
                public const double FREEZING_POINT_OF_WATER = 273.15;

                public const double CONVECTION_FACTOR = 0.43;

                /// <summary>
                /// grams per square km
                /// </summary>
                public const double WATER_MASS_PER_AREA = 3.83E15;

                /// <summary>
                /// Average Earth Temperature
                /// </summary>
                public const double AVERAGE_CELSIUS = 14.0;

                public const double AVERAGE_KELVIN = (AVERAGE_CELSIUS + FREEZING_POINT_OF_WATER);

                /// <summary>
                /// Units of degrees Kelvin
                /// </summary>
                public const double EXOSPHERE_TEMP = 1273.0;
                
                /// <summary>
                /// Units of degrees Kelvin was 255
                /// </summary>
                public const double EFFECTIVE_TEMP = 250.0;

                public const double SURF_PRES_IN_MILLIBARS = 1013.25;
                public const double SURF_PRES_IN_MMHG = 760.0; // Dole p. 15
                
                /// <summary>
                /// Pounds per square inch
                /// </summary>
                public const double SURF_PRES_IN_PSI = 14.696; 
                public const double MMHG_TO_MILLIBARS = (SURF_PRES_IN_MILLIBARS / SURF_PRES_IN_MMHG);
                public const double PSI_TO_MILLIBARS = (SURF_PRES_IN_MILLIBARS / SURF_PRES_IN_PSI);
                public const double PPM_PRSSURE = (SURF_PRES_IN_MILLIBARS / 1000000.0);
            }
        }

        public static class Gasses
        {

            public static Molecule H = new Molecule
            {
                Id = 1,
                Symbol = "H",
                Name = "Hydrogen",
                AtomicWeight = 1.0079,
                MeltingPoint = 14.06,
                BoilingPoint = 20.40,
                Density = 8.99e-05,
                AbundanceE = 0.00125893,
                AbundanceS = 27925.4,
                Reactivity = 1,
                MaximumInspiredPartialPressure = Constants.Units.INCREDIBLY_LARGE_NUMBER
            };

            public static Molecule He = new Molecule
            {
                Id = 2,
                Symbol = "He",
                Name = "Helium",
                AtomicWeight = 4.0026,
                MeltingPoint = 3.46,
                BoilingPoint = 4.20,
                Density = 0.0001787,
                AbundanceE = 7.94328e-09,
                AbundanceS = 2722.7,
                Reactivity = 0,
                MaximumInspiredPartialPressure = (61000.0 * Constants.Sol.Earth.MMHG_TO_MILLIBARS)
            };

            public static Molecule N = new Molecule
            {
                Id = 7,
                Symbol = "N",
                Name = "Nitrogen",
                AtomicWeight = 14.0067,
                MeltingPoint = 63.34,
                BoilingPoint = 77.40,
                Density = 0.0012506,
                AbundanceE = 1.99526e-05,
                AbundanceS = 3.13329,
                Reactivity = 0,
                MaximumInspiredPartialPressure = (2330.0 * Constants.Sol.Earth.MMHG_TO_MILLIBARS)
            };

            public static Molecule O = new Molecule
            {
                Id = 8,
                Symbol = "O",
                Name = "Oxygen",
                AtomicWeight = 15.9994,
                MeltingPoint = 54.80,
                BoilingPoint = 90.20,
                Density = 0.001429,
                AbundanceE = 0.501187,
                AbundanceS = 23.8232,
                Reactivity = 10,
                MaximumInspiredPartialPressure = (400.0 * Constants.Sol.Earth.MMHG_TO_MILLIBARS)
            };

            public static Molecule Ne = new Molecule
            {
                Id = 10,
                Symbol = "Ne",
                Name = "Neon",
                AtomicWeight = 20.1700,
                MeltingPoint = 24.53,
                BoilingPoint = 27.10,
                Density = 0.0009,
                AbundanceE = 5.01187e-09,
                AbundanceS = 3.4435e-5,
                Reactivity = 0,
                MaximumInspiredPartialPressure = (3900.0 * Constants.Sol.Earth.MMHG_TO_MILLIBARS)
            };

            public static Molecule Ar = new Molecule
            {
                Id = 18,
                Symbol = "Ar",
                Name = "Argon",
                AtomicWeight = 39.9480,
                MeltingPoint = 84.00,
                BoilingPoint = 87.30,
                Density = 0.0017824,
                AbundanceE = 3.16228e-06,
                AbundanceS = 0.100925,
                Reactivity = 0,
                MaximumInspiredPartialPressure = (1220.0 * Constants.Sol.Earth.MMHG_TO_MILLIBARS)
            };

            public static Molecule Kr = new Molecule
            {
                Id = 36,
                Symbol = "Kr",
                Name = "Krypton",
                AtomicWeight = 83.8000,
                MeltingPoint = 116.60,
                BoilingPoint = 119.70,
                Density = 0.003708,
                AbundanceE = 1e-10,
                AbundanceS = 4.4978e-05,
                Reactivity = 0,
                MaximumInspiredPartialPressure = Constants.Gasses.InspiredPartialPressure.MAX_KR_IPP
            };

            public static Molecule Xe = new Molecule
            {
                Id = 54,
                Symbol = "Xe",
                Name = "Xenon",
                AtomicWeight = 131.3000,
                MeltingPoint = 161.30,
                BoilingPoint = 165.00,
                Density = 0.00588,
                AbundanceE = 3.16228e-11,
                AbundanceS = 4.69894e-06,
                Reactivity = 0,
                MaximumInspiredPartialPressure = (160.0 * Constants.Sol.Earth.MMHG_TO_MILLIBARS)
            };

            public static Molecule NH3 = new Molecule
            {
                Id = 900,
                Symbol = "NH3",
                Name = "Ammonia",
                AtomicWeight = 17.0000,
                MeltingPoint = 195.46,
                BoilingPoint = 239.66,
                Density = 0.001,
                AbundanceE = 0.002,
                AbundanceS = 0.0001,
                Reactivity = 1,
                MaximumInspiredPartialPressure = (100.0 * Constants.Sol.Earth.PPM_PRSSURE)
            };

            public static Molecule H2O = new Molecule
            {
                Id = 901,
                Symbol = "H2O",
                Name = "Water",
                AtomicWeight = 18.0000,
                MeltingPoint = 273.16,
                BoilingPoint = 373.16,
                Density = 1.000,
                AbundanceE = 0.03,
                AbundanceS = 0.001,
                Reactivity = 0,
                MaximumInspiredPartialPressure = Constants.Units.INCREDIBLY_LARGE_NUMBER
            };

            public static Molecule CO2 = new Molecule
            {
                Id = 902,
                Symbol = "CO2",
                Name = "CarbonDioxide",
                AtomicWeight = 44.0000,
                MeltingPoint = 194.66,
                BoilingPoint = 194.66,
                Density = 0.001,
                AbundanceE = 0.01,
                AbundanceS = 0.0005,
                Reactivity = 0,
                MaximumInspiredPartialPressure = (7.0 * Constants.Sol.Earth.MMHG_TO_MILLIBARS)
            };

            public static Molecule O3 = new Molecule
            {
                Id = 903,
                Symbol = "O3",
                Name = "Ozone",
                AtomicWeight = 48.0000,
                MeltingPoint = 80.16,
                BoilingPoint = 161.16,
                Density = 0.001,
                AbundanceE = 0.001,
                AbundanceS = 0.000001,
                Reactivity = 2,
                MaximumInspiredPartialPressure = (0.1 * Constants.Sol.Earth.PPM_PRSSURE)
            };

            public static Molecule CH4 = new Molecule
            {
                Id = 904,
                Symbol = "CH4",
                Name = "Methane",
                AtomicWeight = 16.0000,
                MeltingPoint = 90.16,
                BoilingPoint = 109.16,
                Density = 0.010,
                AbundanceE = 0.005,
                AbundanceS = 0.0001,
                Reactivity = 1,
                MaximumInspiredPartialPressure = (50000.0 * Constants.Sol.Earth.PPM_PRSSURE)
            };

            public static Molecule F = new Molecule
            {
                Id = 9,
                Symbol = "F",
                Name = "Fluorine",
                AtomicWeight = 18.9984,
                MeltingPoint = 53.58,
                BoilingPoint = 85.10,
                Density = 0.001696,
                AbundanceE = 0.000630957,
                AbundanceS = 0.000843335,
                Reactivity = 50,
                MaximumInspiredPartialPressure = (0.1 * Constants.Sol.Earth.PPM_PRSSURE)
            };

            public static Molecule Cl = new Molecule
            {
                Id = 17,
                Symbol = "Cl",
                Name = "Chlorine",
                AtomicWeight = 35.4530,
                MeltingPoint = 172.22,
                BoilingPoint = 239.20,
                Density = 0.003214,
                AbundanceE = 0.000125893,
                AbundanceS = 0.005236,
                Reactivity = 40,
                MaximumInspiredPartialPressure = (1.0 * Constants.Sol.Earth.PPM_PRSSURE)
            };

            public static Dictionary<int, Molecule> GasLookup = new Dictionary<int, Molecule>()
            {
                {H.Id, H},
                {He.Id, He},
                {N.Id, N},
                {O.Id, O},
                {Ne.Id, Ne},
                {Ar.Id, Ar},
                {Kr.Id, Kr},
                {Xe.Id, Xe},
                {NH3.Id, NH3},
                {H2O.Id, H2O},
                {CO2.Id, CO2},
                {O3.Id, O3},
                {CH4.Id, CH4},
                {F.Id, F},
                {Cl.Id, Cl}
            };


            /// <summary>
            /// Atomic numbers for use in the ElementalTable as lookups
            /// </summary>
            /*
            public class AtomicNumbers
            {
                public const int AN_H = 1;
                public const int AN_HE = 2;
                public const int AN_N = 7;
                public const int AN_O = 8;
                public const int AN_F = 9;
                public const int AN_NE = 10;
                public const int AN_P = 15;
                public const int AN_CL = 17;
                public const int AN_AR = 18;
                public const int AN_BR = 35;
                public const int AN_KR = 36;
                public const int AN_I = 53;
                public const int AN_XE = 54;
                public const int AN_HG = 80;
                public const int AN_AT = 85;
                public const int AN_RN = 86;
                public const int AN_FR = 87;

                public const int AN_NH3 = 900;
                public const int AN_H2O = 901;
                public const int AN_CO2 = 902;
                public const int AN_O3 = 903;
                public const int AN_CH4 = 904;
                public const int AN_CH3CH2OH = 905;
            }
            */
            /// <summary>
            /// Inspired Partial Pressure constants
            /// </summary>
            public class InspiredPartialPressure
            {
                public const double H20_ASSUMED_PRESSURE = (47.0 * Constants.Sol.Earth.MMHG_TO_MILLIBARS);
                public const double MIN_O2_IPP = (72.0 * Constants.Sol.Earth.MMHG_TO_MILLIBARS);	/* Dole, p. 15				*/
                public const double MAX_O2_IPP = (400.0 * Constants.Sol.Earth.MMHG_TO_MILLIBARS);	/* Dole, p. 15				*/
                public const double MAX_HE_IPP = (61000.0 * Constants.Sol.Earth.MMHG_TO_MILLIBARS);	/* Dole, p. 16			*/
                public const double MAX_NE_IPP = (3900.0 * Constants.Sol.Earth.MMHG_TO_MILLIBARS);/* Dole, p. 16				*/
                public const double MAX_N2_IPP = (2330.0 * Constants.Sol.Earth.MMHG_TO_MILLIBARS);	/* Dole, p. 16				*/
                public const double MAX_AR_IPP = (1220.0 * Constants.Sol.Earth.MMHG_TO_MILLIBARS);	/* Dole, p. 16				*/
                public const double MAX_KR_IPP = (350.0 * Constants.Sol.Earth.MMHG_TO_MILLIBARS);	/* Dole, p. 16				*/
                public const double MAX_XE_IPP = (160.0 * Constants.Sol.Earth.MMHG_TO_MILLIBARS);	/* Dole, p. 16				*/
                public const double MAX_CO2_IPP = (7.0 * Constants.Sol.Earth.MMHG_TO_MILLIBARS);	/* Dole, p. 16				*/

                //The next gasses are listed as poisonous in parts per million by volume at 1 atm:
                public const double MAX_F_IPP = (0.1 * Constants.Sol.Earth.PPM_PRSSURE);		/* Dole, p. 18				*/
                public const double MAX_CL_IPP = (1.0 * Constants.Sol.Earth.PPM_PRSSURE);		/* Dole, p. 18				*/
                public const double MAX_NH3_IPP = (100.0 * Constants.Sol.Earth.PPM_PRSSURE);		/* Dole, p. 18				*/
                public const double MAX_O3_IPP = (0.1 * Constants.Sol.Earth.PPM_PRSSURE);		/* Dole, p. 18				*/
                public const double MAX_CH4_IPP = (50000.0 * Constants.Sol.Earth.PPM_PRSSURE);	/* Dole, p. 18				*/
            }

            /// <summary>
            /// Constants for molecular weights of elements and molecules that are used in 
            /// the gasses simulation
            /// 
            /// Molecular weights (used for RMS velocity calcs):
            /// This table is from Dole's book "Habitable Planets for Man", p. 38
            /// </summary>
            public class MolecularWeights
            {
                //public const double ATOMIC_HYDROGEN = 1.0;	/* H   */
                public const double MOL_HYDROGEN = 2.0;	/* H2  */
                public const double HELIUM = 4.0;	/* He  */
                //public const double ATOMIC_NITROGEN = 14.0;	/* N   */
                //public const double ATOMIC_OXYGEN = 16.0;	/* O   */
                //public const double METHANE = 16.0;/* CH4 */
                //public const double AMMONIA = 17.0;	/* NH3 */
                public const double WATER_VAPOR = 18.0;/* H2O */
                //public const double NEON = 20.2;	/* Ne  */
                public const double MOL_NITROGEN = 28.0;	/* N2  */
                //public const double CARBON_MONOXIDE = 28.0;	/* CO  */
                //public const double NITRIC_OXIDE = 30.0;/* NO  */
                //public const double MOL_OXYGEN = 32.0;	/* O2  */
                //public const double HYDROGEN_SULPHIDE = 34.1;	/* H2S */
                //public const double ARGON = 39.9;	/* Ar  */
                //public const double CARBON_DIOXIDE = 44.0;	/* CO2 */
                //public const double NITROUS_OXIDE = 44.0;	/* N2O */
                //public const double NITROGEN_DIOXIDE = 46.0;	/* NO2 */
                //public const double OZONE = 48.0;	/* O3  */
                //public const double SULPH_DIOXIDE = 64.1;	/* SO2 */
                //public const double SULPH_TRIOXIDE = 80.1;	/* SO3 */
                //public const double KRYPTON = 83.8;/* Kr  */
                //public const double XENON = 131.3; /* Xe  */ 
            }
        }

        /// <summary>
        /// Constants specifically used in the stargen module
        /// </summary>
        public class Stargen
        {
            /// <summary>
            /// Initial mass of injected protoplanet
            /// </summary>
            public const double PROTOPLANET_MASS = 1.0E-15;

            public const double ASTEROID_MASS_LIMIT = 0.001;

            /// <summary>
            /// Constant representing the gas/dust ratio
            /// </summary>
            public const double K = 50.0;

            /// <summary>
            /// Constant used in Crit_mass calc
            /// </summary>
            public const double B = 1.2E-5;

            /// <summary>
            /// Coefficient of dust density (A in Dole's paper)
            /// </summary>
            public const double DUST_DENSITY_COEFF = 2.0E-3;

            /// <summary>
            /// Constant used in density calcs
            /// </summary>
            public const double ALPHA = 5.0;

            /// <summary>
            /// Constant used in density calcs
            /// </summary>
            public const double N = 3.0;

            /// <summary>
            /// Constant used in density calcs
            /// </summary>
            public const double J = 1.46E-19;

            //	The following defines are used in the kothari_radius function in
            public const double A1_20 = 6.485E12; // All units are in cgs system.	

            public const double A2_20 = 4.0032E-8; // ie: cm, g, dynes, etc.	

            public const double BETA_20 = 5.71E12;

            // The following defines are used in determining the fraction of a planet 
            // covered with clouds in function CloudFraction
            public const double Q1_36 = 1.258E19; // grams

            public const double Q2_36 = 0.0698; // 1/Kelvin

            public const double JIMS_FUDGE = 1.004;
        }
    }
}
