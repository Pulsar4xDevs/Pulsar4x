using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.Entities;
using System.Drawing;
using Pulsar4X.Entities.Components;
using System.ComponentModel;

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

            public const double KM_PER_AU = CM_PER_AU / CM_PER_KM; //149597870.7


            /// <summary>
            /// For anyone worried, these are only to check to see if the large distance model needs to be used or not, we aren't constrained to 32 bit limitations with regards to distances.
            /// </summary>

            /// <summary>
            /// 32 bit limitation number for distances in KM. //14.35504154
            /// </summary>
            public const double MAX_KM_IN_AU = 2147483648.0 / KM_PER_AU;

            /// <summary>
            /// 5 second speed of light limitation for beam weapons.
            /// </summary>
            public const double BEAM_AU_MAX = 1500000.0 / KM_PER_AU; //~0.01002 AU

            /// <summary>
            /// Speed of light limitation, this time in KM.
            /// </summary>
            public const double BEAM_KM_MAX = Constants.Units.BEAM_AU_MAX * Constants.Units.KM_PER_AU;

            /// <summary>
            /// 32 bit limitation for the 10KM unit system used by auroraTN.
            /// </summary>
            public const double TEN_KM_MAX = 214748.0;

            /// <summary>
            /// 32 bit limitation for orbit period days.
            /// </summary>
            public const double MAX_DAYS_IN_SECONDS = 2147483648.0 / Constants.TimeInSeconds.Day;

            /// <summary>
            /// Plus or Minus 65Km
            /// </summary>
            public const double SOLAR_RADIUS_IN_KM = 696342.0;

            public const double SOLAR_RADIUS_IN_AU = SOLAR_RADIUS_IN_KM / KM_PER_AU;

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

            /// <summary>
            /// RADIAN is the value of each degree in radians. PI/180.
            /// </summary>
            public const double RADIAN = Math.PI / 180.0;

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

        public static class Gases
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
                MaximumInspiredPartialPressure = Constants.Gases.InspiredPartialPressure.MAX_KR_IPP
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
        public static class Stargen
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

            public const double CLOUD_ECCENTRICITY = 0.2D;
        }

        public class StarColor
        {
            private Dictionary<string, Color> m_dicStarColors;

            private static StarColor m_oStarColor;

            private StarColor()
            {
                m_dicStarColors = new Dictionary<string, Color>();

                // note the following colors are source fron:
                // http://www.vendian.org/mncharity/dir3/starcolor/UnstableURLs/starcolors.html
                // where this source did not provide a color an aproiximation was done using 
                // the next class above and below the missing one. The are comented as such.
                m_dicStarColors["O5"] = Color.FromArgb(255, 155, 176, 255);
                m_dicStarColors["O6"] = Color.FromArgb(255, 162, 184, 255);
                m_dicStarColors["O7"] = Color.FromArgb(255, 157, 177, 255);
                m_dicStarColors["O8"] = Color.FromArgb(255, 157, 177, 255);
                m_dicStarColors["O9"] = Color.FromArgb(255, 154, 178, 255);
                m_dicStarColors["O9.5"] = Color.FromArgb(255, 164, 186, 255);

                m_dicStarColors["B0"] = Color.FromArgb(255, 156, 178, 255);
                m_dicStarColors["B0.5"] = Color.FromArgb(255, 167, 188, 255);
                m_dicStarColors["B1"] = Color.FromArgb(255, 160, 182, 255);
                m_dicStarColors["B2"] = Color.FromArgb(255, 160, 180, 255);
                m_dicStarColors["B3"] = Color.FromArgb(255, 165, 185, 255);
                m_dicStarColors["B4"] = Color.FromArgb(255, 164, 184, 255);
                m_dicStarColors["B5"] = Color.FromArgb(255, 170, 191, 255);
                m_dicStarColors["B6"] = Color.FromArgb(255, 172, 189, 255);
                m_dicStarColors["B7"] = Color.FromArgb(255, 173, 191, 255);
                m_dicStarColors["B8"] = Color.FromArgb(255, 177, 195, 255);
                m_dicStarColors["B9"] = Color.FromArgb(255, 181, 198, 255);

                m_dicStarColors["A0"] = Color.FromArgb(255, 185, 201, 255);
                m_dicStarColors["A1"] = Color.FromArgb(255, 181, 199, 255);
                m_dicStarColors["A2"] = Color.FromArgb(255, 187, 203, 255);
                m_dicStarColors["A3"] = Color.FromArgb(255, 191, 207, 255);
                m_dicStarColors["A4"] = Color.FromArgb(255, 195, 210, 255);
                m_dicStarColors["A5"] = Color.FromArgb(255, 202, 215, 255);
                m_dicStarColors["A6"] = Color.FromArgb(255, 199, 212, 255);
                m_dicStarColors["A7"] = Color.FromArgb(255, 200, 213, 255);
                m_dicStarColors["A8"] = Color.FromArgb(255, 213, 222, 255);
                m_dicStarColors["A9"] = Color.FromArgb(255, 219, 224, 255);

                m_dicStarColors["F0"] = Color.FromArgb(255, 224, 229, 255);
                m_dicStarColors["F1"] = Color.FromArgb(255, 230, 234, 255);   // This value is estimated using F0 and F2 values.
                m_dicStarColors["F2"] = Color.FromArgb(255, 236, 239, 255);
                m_dicStarColors["F3"] = Color.FromArgb(255, 227, 230, 255);
                m_dicStarColors["F4"] = Color.FromArgb(255, 224, 226, 255);
                m_dicStarColors["F5"] = Color.FromArgb(255, 248, 247, 255);
                m_dicStarColors["F6"] = Color.FromArgb(255, 244, 241, 255);
                m_dicStarColors["F7"] = Color.FromArgb(255, 246, 243, 255);
                m_dicStarColors["F8"] = Color.FromArgb(255, 255, 247, 252);
                m_dicStarColors["F9"] = Color.FromArgb(255, 255, 247, 252);

                m_dicStarColors["G0"] = Color.FromArgb(255, 255, 248, 252);
                m_dicStarColors["G1"] = Color.FromArgb(255, 255, 247, 248);
                m_dicStarColors["G2"] = Color.FromArgb(255, 255, 245, 242);
                m_dicStarColors["G3"] = Color.FromArgb(255, 255, 243, 233);
                m_dicStarColors["G4"] = Color.FromArgb(255, 255, 241, 229);
                m_dicStarColors["G5"] = Color.FromArgb(255, 255, 244, 234);
                m_dicStarColors["G6"] = Color.FromArgb(255, 255, 244, 235);
                m_dicStarColors["G7"] = Color.FromArgb(255, 255, 244, 235);
                m_dicStarColors["G8"] = Color.FromArgb(255, 255, 237, 222);
                m_dicStarColors["G9"] = Color.FromArgb(255, 255, 239, 221);

                m_dicStarColors["K0"] = Color.FromArgb(255, 255, 238, 221);
                m_dicStarColors["K1"] = Color.FromArgb(255, 255, 224, 188);
                m_dicStarColors["K2"] = Color.FromArgb(255, 255, 227, 196);
                m_dicStarColors["K3"] = Color.FromArgb(255, 255, 222, 195);
                m_dicStarColors["K4"] = Color.FromArgb(255, 255, 216, 181);
                m_dicStarColors["K5"] = Color.FromArgb(255, 255, 210, 161);
                m_dicStarColors["K6"] = Color.FromArgb(255, 255, 204, 151);  // This value is estimated using K5 and K7 values.
                m_dicStarColors["K7"] = Color.FromArgb(255, 255, 199, 142);
                m_dicStarColors["K8"] = Color.FromArgb(255, 255, 209, 174);
                m_dicStarColors["K9"] = Color.FromArgb(255, 255, 200, 161);  // This value is estimated using K8 and M0 values.

                m_dicStarColors["M0"] = Color.FromArgb(255, 255, 195, 139);
                m_dicStarColors["M1"] = Color.FromArgb(255, 255, 204, 142);
                m_dicStarColors["M2"] = Color.FromArgb(255, 255, 196, 131);
                m_dicStarColors["M3"] = Color.FromArgb(255, 255, 206, 129);
                m_dicStarColors["M4"] = Color.FromArgb(255, 255, 201, 127);
                m_dicStarColors["M5"] = Color.FromArgb(255, 255, 204, 111);
                m_dicStarColors["M6"] = Color.FromArgb(255, 255, 195, 112);
                m_dicStarColors["M7"] = Color.FromArgb(255, 255, 197, 110);   // This value is estimated using M6 and M8 values.
                m_dicStarColors["M8"] = Color.FromArgb(255, 255, 198, 109);
                m_dicStarColors["M9"] = Color.FromArgb(255, 255, 233, 154);

                m_dicStarColors["O"] = Color.FromArgb(255, 155, 176, 255);
                m_dicStarColors["B"] = Color.FromArgb(255, 170, 191, 255);
                m_dicStarColors["A"] = Color.FromArgb(255, 202, 215, 255);
                m_dicStarColors["F"] = Color.FromArgb(255, 248, 247, 255);
                m_dicStarColors["G"] = Color.FromArgb(255, 255, 244, 234);
                m_dicStarColors["K"] = Color.FromArgb(255, 255, 210, 161);
                m_dicStarColors["M"] = Color.FromArgb(255, 255, 204, 111);
                m_dicStarColors["N"] = Color.FromArgb(255, 255, 157, 000);
            }

            public static Color LookupColor(string a_szSpectralClass)
            {
                if (m_oStarColor == null)
                {
                    m_oStarColor = new StarColor();
                }

                if (m_oStarColor.m_dicStarColors[a_szSpectralClass] != null)
                {
                    return m_oStarColor.m_dicStarColors[a_szSpectralClass];
                }

                return Color.White;
            }
        }

        public static class Minerals
        {
            public enum MinerialNames
            {
                Duranium,
                Neutronium,
                Corbomite,
                Tritanium,
                Boronide,
                Mercassium,
                Vendarite,
                Sorium,
                Uridium,
                Corundium,
                Gallicite,
                MinerialCount
            }

            public const int NO_OF_MINERIALS = (int)MinerialNames.MinerialCount;
        }

        /// <summary>
        /// ShipTN related constants here right now.
        /// </summary>
        public static class ShipTN
        {
            /// <summary>
            /// No sensor may have a resolution greater than 500 HS or about 25,000 tons.
            /// </summary>
            public const int ResolutionMax = 500;

            /// <summary>
            /// there are 50 tons per any single hull space.
            /// </summary>
            public const float TonsPerHS = 50.0f;

            /// <summary>
            /// In seconds per ton.
            /// </summary>
            public const int BaseCargoLoadTimePerTon = 36;

            /// <summary>
            /// In seconds per person. Each cryopod seems to be 1/2 of a ton.
            /// </summary>
            public const int BaseCryoLoadTimePerPerson = 18;

            /// <summary>
            /// In Aurora, load time is a constant 10 days for troop transports.
            /// </summary>
            public const int BaseTroopLoadTime = 864000;

            /// <summary>
            /// I moved the following three enums here to constants to prevent taskgroup from ballooning in size beyond all recognition, I may move them back though once done.
            /// </summary>
            public enum OrderType
            {
                /// <summary>
                /// General Ship Orders:
                /// </summary>
                MoveTo,
                ExtendedOrbit,
                Picket,
                LoadCrewFromColony,
                RefuelFromColony,
                ResupplyFromColony,
                SendMessage,
                EqualizeFuel,
                EqualizeMSP,
                ActivateTransponder,
                DeactivateTransponder,

                /// <summary>
                /// TaskGroups with active sensors:
                /// </summary>
                ActivateSensors,
                DeactivateSensors,

                /// <summary>
                /// Taskgroups with shield equipped ships:
                /// </summary>
                ActivateShields,
                DeactivateShields,

                /// <summary>
                /// Any Taskgroup of more than one vessel.
                /// </summary>
                DivideFleetToSingleShips,

                /// <summary>
                /// Any taskgroup that has sub task groups created from it, such as by a divide order.
                /// </summary>
                IncorporateSubfleet,

                /// <summary>
                /// Military Ship Specific orders:
                /// </summary>
                BeginOverhaul,


                /// <summary>
                /// Targeted on taskforce specific orders:
                /// </summary>
                Follow,
                Join,
                Absorb,

                /// <summary>
                /// JumpPoint Capable orders only:
                /// </summary>
                StandardTransit,
                SquadronTransit,
                TransitAndDivide,

                /// <summary>
                /// Cargo Hold specific orders when targeted on population/planet:
                /// </summary>
                LoadInstallation,
                LoadShipComponent,
                UnloadInstallation,
                UnloadShipComponent,
                UnloadAll,
                LoadAllMinerals,
                UnloadAllMinerals,
                LoadMineral,
                LoadMineralWhenX,
                UnloadMineral,
                LoadOrUnloadMineralsToReserve,

                /// <summary>
                /// Colony ship specific orders:
                /// </summary>
                LoadColonists,
                UnloadColonists,

                /// <summary>
                /// GeoSurvey specific orders:
                /// </summary>
                GeoSurvey,
                DetachNonGeoSurvey,

                /// <summary>
                /// Grav survey specific orders:
                /// </summary>
                GravSurvey,
                DetachNonGravSurvey,

                /// <summary>
                /// Jump Gate Construction Module specific orders:
                /// </summary>
                BuildJumpGate,

                /// <summary>
                /// Tanker Specific:
                /// </summary>
                RefuelTargetFleet,
                RefuelFromOwnTankers,
                UnloadFuelToPlanet,
                DetachTankers,

                /// <summary>
                /// Supply Ship specific:
                /// </summary>
                ResupplyTargetFleet,
                ResupplyFromOwnSupplyShips,
                UnloadSuppliesToPlanet,
                DetachSupplyShips,

                /// <summary>
                /// Collier Specific:
                /// </summary>
                ReloadTargetFleet,
                ReloadFromOwnColliers,
                DetachColliers,

                /// <summary>
                /// Any ship with a magazine:
                /// </summary>
                LoadOrdnanceFromColony,
                UnloadOrdnanceToColony,

                /// <summary>
                /// Any taskgroup, but the target must be a TG with the appropriate ship to fulfill this order.
                /// </summary>
                RefuelFromTargetFleet,
                ResupplyFromTargetFleet,
                ReloadFromTargetFleet,

                /// <summary>
                /// Any taskgroup, but target must have hangar bays, perhaps check to see if capacity is available.
                /// </summary>
                LandOnAssignedMothership,
                LandOnMotherShipNoAssign,
                LandOnMothershipAssign,

                /// <summary>
                /// Tractor Equipped Ships:
                /// </summary>
                TractorSpecifiedShip,
                TractorSpecifiedShipyard,
                ReleaseAt,

                /// <summary>
                /// Number of orders available.
                /// </summary>
                TypeCount
            }

            /// <summary>
            /// What state is the taskgroup in regarding accepting additional orders?
            /// </summary>
            public enum OrderState
            {
                AcceptOrders,
                DisallowOrdersPDC,
                DisallowOrdersSB,
                DisallowOrdersUnknownJump,
                DisallowOrdersFollowingTarget,
                UnableToComply,
                CurrentlyOverhauling,
                CurrentlyLoading,
                CurrentlyUnloading,
                TypeCount
            }

            public enum LoadType
            {
                Cargo,
                Cryo,
                Troop,
                TypeCount
            }
        }

        /// <summary>
        /// Faction related goodness.
        /// </summary>
        public static class Faction
        {
            /// <summary>
            /// FactionMax has to be relatively hard coded or else the sensor model goes to hell.
            /// </summary>
            public const int FactionMax = 64;

            /// <summary>
            /// What should the starting wealth be?
            /// </summary>
            public const decimal StartingWealth = 100000.0m;
        }

        /// <summary>
        /// Colony related constants
        /// </summary>
        public static class Colony
        {
            /// <summary>
            /// Thermal signature per million pop.
            /// </summary>
            public static float CivilianThermalSignature = 5.0f;

            /// <summary>
            /// EM signature per million pop.
            /// </summary>
            public static float CivilianEMSignature = 50.0f;


            /// <summary>
            /// For Thermal and EM signature calculations.
            /// </summary>
            public static float NavalShipyardTonnageDivisor = 50.0f;

            /// <summary>
            /// For Thermal and EM signature calculations.
            /// </summary>
            public static float CommercialShipyardTonnageDivisor = 500.0f;
            /// <summary>
            /// What sensor strength will a single DSTS add? This is about equal to a full sized thermal sensor array at each tech level.
            /// </summary>
            public static int[] DeepSpaceStrength = { 250, 300, 400, 550, 700, 900, 1200, 1600, 2000, 2500, 3000, 3750 };

            /// <summary>
            /// Maximum index to DeepSpaceStrength
            /// </summary>
            public const int DeepSpaceMax = 11;

            /// <summary>
            /// Maintenance supply part cost.
            /// </summary>
            public static decimal MaintenanceSupplyCost = 0.25m;
            public static decimal[] MaintenanceMineralCost = { 0.125m, 0.0m, 0.0m, 0.0m, 0.0m, 0.0m, 0.0m, 0.0m, 0.0625m, 0.0m, 0.0625m };

            /// <summary>
            /// How often should build work be run?
            /// </summary>
            public const uint ConstructionCycle = Constants.TimeInSeconds.FiveDays;

            /// <summary>
            /// How much fuel will one refined unit of Sorium yield?
            /// </summary>
            public const float SoriumToFuel = 2000.0f;

            /// <summary>
            /// YearsOfProduction here being greater than 5475852 means that it will take more than 2 Billion days, or around the 32 bit limit. so don't bother calculating time in that case.
            /// </summary>
            public const int TimerYearMax = 5475852;
        }

        /// <summary>
        /// Tick times to complete said interval.
        /// </summary>
        public static class TimeInSeconds
        {
            public const uint FiveSeconds = 5;
            public const uint ThirtySeconds = 30;
            public const uint Minute = 60;
            public const uint TwoMinutes = 120;
            public const uint FiveMinutes = 300;
            public const uint TwentyMinutes = 1200;
            public const uint Hour = 3600;
            public const uint ThreeHours = 10800;
            public const uint EightHours = 28800;
            public const uint Day = 86400;
            public const uint FiveDays = 432000;
            public const uint Week = 604800;
            public const uint Month = 2592000;
            public const uint Year = 31104000;
            public const uint RealYear = 31556736;
            public const uint Century = 3110400000;
        }

        /// <summary>
        /// Sensor TN describes tech levels for active and passive sensors.
        /// </summary>
        public static class SensorTN
        {
            public static byte[] ActiveStrength = { 10, 12, 16, 21, 28, 36, 48, 60, 80, 100, 135, 180 };
            public static byte[] PassiveStrength = { 5, 6, 8, 11, 14, 18, 24, 32, 40, 50, 60, 75 };
        }

        /// <summary>
        /// Beam fire control tech levels.  multiply these values by 1,000 for Range.
        /// </summary>
        public static class BFCTN
        {
            /// <summary>
            /// Range modifier for the BFC, in 10k increments.
            /// </summary>
            public static byte[] BeamFireControlRange = { 10, 16, 24, 32, 40, 48, 60, 75, 100, 125, 150, 175 };

            /// <summary>
            /// Tracking modifier for the BFC, has to be in km.
            /// </summary>
            public static ushort[] BeamFireControlTracking = { 1250, 2000, 3000, 4000, 5000, 6250, 8000, 10000, 12500, 15000, 20000, 25000 };
        };

        /// <summary>
        /// Power plant tech levels. Power generation per HS.
        /// </summary>
        public static class ReactorTN
        {
            /// <summary>
            /// Reactor Base Power Generation.
            /// </summary>
            public static float[] Power = { 2.0f, 3.0f, 4.5f, 6.0f, 8.0f, 10.0f, 12.0f, 16.0f, 20.0f, 24.0f, 32.0f, 40.0f };
        };

        /// <summary>
        /// Engine related tech levels for Power and Fuel consumption.
        /// </summary>
        public static class EngineTN
        {
            /// <summary>
            /// Engine base Power per 1 HS.
            /// </summary>
            public static float[] EngineBase = { 0.2f, 5.0f, 8.0f, 12.0f, 16.0f, 20.0f, 25.0f, 32.0f, 40.0f, 50.0f, 60.0f, 80.0f, 100.0f };

            /// <summary>
            /// Fuel consumption reduction per engine power hour(or else per hour for standard shields).
            /// </summary>
            public static float[] FuelConsumption = { 1.0f, 0.9f, 0.8f, 0.7f, 0.6f, 0.5f, 0.4f, 0.3f, 0.25f, 0.2f, 0.16f, 0.125f, 0.1f };

            /// <summary>
            /// Thermal reduction for engines.
            /// </summary>
            public static float[] ThermalReduction = { 1.0f, 0.75f, 0.5f, 0.35f, 0.25f, 0.16f, 0.12f, 0.08f, 0.06f, 0.04f, 0.03f, 0.02f, 0.01f };

            /// <summary>
            /// Hyperdrive size modifier for engines.
            /// </summary>
            public static float[] HyperDriveSize = { 2.0f, 1.8f, 1.6f, 1.5f, 1.4f, 1.3f, 1.2f, 1.15f, 1.1f, 1.05f, 1.0f };
        };

        /// <summary>
        /// Beam Weapon constants are related to tech values.
        /// </summary>
        public static class BeamWeaponTN
        {
            /// <summary>
            /// Size in cm of weapons.
            /// </summary>
            public static byte[] SizeClass = { 10, 12, 15, 20, 25, 30, 35, 40, 50, 60, 70, 80 };

            /// <summary>
            /// Capacitor power per tech level
            /// </summary>
            public static byte[] Capacitor = { 1, 2, 3, 4, 5, 6, 8, 10, 12, 16, 20, 25 };

            /// <summary>
            /// Point blank damage value for each calibre of railgun.
            /// </summary>
            public static byte[] RailGunDamage = { 1, 2, 3, 4, 5, 7, 9, 12, 16, 20 };

            /// <summary>
            /// Size value for each calibre of railgun from 10cm to 50cm.
            /// </summary>
            public static byte[] RailGunSize = { 3, 5, 6, 7, 8, 9, 10, 11, 13, 15 };

            /// <summary>
            /// Point blank damage values for lasers and plasma for each tech level from 10cm to 80cm, plasmas lack 10 and 12cm guns however.
            /// Also the power consumption values for mesons and HPMs.
            /// </summary>
            public static byte[] LaserDamage = { 3, 4, 6, 10, 16, 24, 32, 40, 64, 96, 128, 168 };

            /// <summary>
            /// Shared damage values for advanced lasers and plasma.
            /// </summary>
            public static byte[] AdvancedLaserDamage = { 4, 5, 8, 12, 20, 30, 40, 50, 80, 120, 160, 210 };

            /// <summary>
            /// Size of each calibre gun for lasers,plasma,microwave, and mesons from 10cm to 80cm.
            /// </summary>
            public static byte[] LaserSize = { 3, 4, 4, 6, 8, 9, 11, 12, 16, 19, 22, 25 };

            /// <summary>
            /// Damage for each Particle beam.
            /// </summary>
            public static byte[] ParticleDamage = { 2, 3, 4, 6, 9, 12, 16, 20, 25, 36, 50 };

            /// <summary>
            /// Damage for advanced particle beams;
            /// </summary>
            public static byte[] AdvancedParticleDamage = { 3, 4, 5, 8, 11, 15, 20, 25, 32, 45, 64 };

            /// <summary>
            /// Power requirement for each Particle beam.
            /// </summary>
            public static byte[] ParticlePower = { 5, 7, 10, 15, 22, 30, 40, 48, 64, 90, 125 };

            /// <summary>
            /// Size of each Particle beam. Not the advanced particle beams however.
            /// </summary>
            public static byte[] ParticleSize = { 5, 6, 7, 8, 9, 10, 12, 14, 16, 18, 22 };

            /// <summary>
            /// Range modifier for particle beam technology, in 10k units.
            /// </summary>
            public static ushort[] ParticleRange = { 6, 10, 15, 20, 24, 32, 40, 50, 64, 80, 100, 120 };

            /// <summary>
            /// Size reduction and accuracy modifiers for Gauss weapons.
            /// </summary>
            public static float[] GaussSize = { 6.0f, 5.0f, 4.0f, 3.0f, 2.0f, 1.5f, 1.0f, 0.75f, 0.6f, 0.5f };
            public static float[] GaussAccuracy = { 1.0f, 0.85f, 0.67f, 0.5f, 0.33f, 0.25f, 0.17f, 0.125f, 0.1f, 0.08f };

            /// <summary>
            /// How many shots this weapon takes every time it fires.
            /// </summary>
            public static byte[] GaussShots = { 1, 2, 3, 4, 5, 6, 8 };

            /// <summary>
            /// Gear size multipliers for multi-barreled turrets.
            /// </summary>
            public static float[] TurretGearFactor = { 0.1f, 0.095f, 0.0925f, 0.09f };
        }

        /// <summary>
        /// Shield related constants placed here for now.
        /// </summary>
        public static class ShieldTN
        {
            /// <summary>
            /// Cost of each shield component tech level. 4 + CostBase[Str] + CostBase[Regen] = cost.
            /// </summary>
            public static byte[] CostBase = { 0, 1, 2, 3, 4, 6, 8, 10, 14, 18, 22, 28 };

            /// <summary>
            /// Strength and regen values for normal shields.
            /// for absorption shields Strength is 3x this, and radiate rate is 1/2x this.
            /// </summary>
            public static float[] ShieldBase = { 1.0f, 1.5f, 2.0f, 2.5f, 3.0f, 4.0f, 5.0f, 6.0f, 8.0f, 10.0f, 12.0f, 15.0f };
        }

        public static class MagazineTN
        {
            /// <summary>
            /// Internal armor factor for magazines, and for everywhere else that uses armor. Good programming practices.
            /// </summary>
            public static int[] MagArmor = { 2, 5, 6, 8, 10, 12, 15, 18, 21, 25, 30, 36, 45 };

            /// <summary>
            /// Chance of not having catastrophic destruction occur on mag destruction.
            /// </summary>
            public static float[] Ejection = { 0.7f, 0.8f, 0.85f, 0.9f, 0.93f, 0.95f, 0.97f, 0.98f, 0.99f };

            /// <summary>
            /// Internal space not devoted to the feed mechanism.
            /// </summary>
            public static float[] FeedMechanism = { 0.75f, 0.8f, 0.85f, 0.9f, 0.92f, 0.94f, 0.96f, 0.98f, 0.99f };
        }

        public static class LauncherTN
        {
            /// <summary>
            /// Launcher size adjustment
            /// </summary>
            public static float[] Reduction = { 1.0f, 0.75f, 0.5f, 0.33f, 0.25f, 0.15f };

            /// <summary>
            /// Launcher penalty from reduction.
            /// </summary>
            public static float[] Penalty = { 1.0f, 2.0f, 5.0f, 20.0f, 100.0f, 15.0f };

            /// <summary>
            /// Which index is the boxlauncher?
            /// </summary>
            public static int BoxLauncher = 5;
        }


        public static class OrdnanceTN
        {
            public static int[] warheadTech = { 2, 3, 4, 5, 6, 8, 10, 12, 16, 20, 24, 30 };
            public static int[] agilityTech = { 20, 32, 48, 64, 80, 100, 128, 160, 200, 240, 320, 400 };
            public static float[] passiveTech = { 0.25f, 0.3f, 0.4f, 0.55f, 0.7f, 0.9f, 1.2f, 1.6f, 2.0f, 2.5f, 3.0f, 3.75f };
            public static float[] activeTech = { 0.5f, 0.6f, 0.8f, 1.05f, 1.4f, 1.6f, 2.4f, 3.0f, 4.0f, 5.0f, 6.75f, 9.0f };
            public static float[] geoTech = { 0.01f, 0.02f, 0.03f, 0.05f };
            public static float[] reactorTech = { 0.1f, 0.15f, 0.225f, 0.3f, 0.4f, 0.5f, 0.6f, 0.8f, 1.0f, 1.2f, 1.6f, 2.0f };
            public static int[] radTech = { 2, 3, 4, 5 };
            public static int[] laserTech = { 2, 4, 6, 10 };

            /// <summary>
            /// Launchers are capped at this value so no missile greater than 100 makes any sense to be designed.
            /// </summary>
            public const double MaxSize = 100.0;

            /// <summary>
            /// Maximum missile speed which they may not exceed in km
            /// </summary>
            public const int MaximumSpeed = 299000;

            /// <summary>
            /// No missile will be smaller than size 6 for sensor purposes. This works out to 0.33 HS, not 0.3 HS however. This is subtracted from 6(6-6=0) for the activeSensor LookUpMT array.
            /// </summary>
            public const int MissileResolutionMinimum = 0;

            /// <summary>
            /// 1 HS, or 20 MSP is the maximum size for missile resolution. This is subtracted from 6(20-6=14) for the activeSensor LookUpMT array.
            /// Missiles can be larger than this, but they will just use LookUpST.
            /// </summary>
            public const int MissileResolutionMaximum = 14;
        }

        public static class JumpEngineTN
        {
            /// <summary>
            /// How many HS of ship does 1 HS of JumpEngine support.
            /// </summary>
            public static int[] JumpEfficiency = { 4,5,6,8,10,12,15,18,21,25 };

            /// <summary>
            /// How many ships can use this single jump engine.
            /// </summary>
            public static int[] SquadSize = { 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };

            /// <summary>
            /// How much bigger is this jump engine for allowing increased squadSize? size is JESize * this.
            /// </summary>
            public static float[] SquadSizeModifier = { 1.0f, 1.1f, 1.2f, 1.3f, 1.4f, 1.5f, 1.6f, 2.4f, 2.6f, 3.0f };

            /// <summary>
            /// How far from the jumppoint can a squadron transit go? multiply this by 10k km.
            /// </summary>
            public static int[] JumpRadius = { 5,10,25,50,75,100,150,200,250,300,400 };

            /// <summary>
            /// How much larger will this jumpengine be due to its jump radius tech?
            /// </summary>
            public static float[] JumpRadiusModifier = { 1.0f, 1.05f, 1.1f, 1.15f, 1.2f, 1.25f, 1.3f, 1.4f, 1.5f, 1.6f, 1.8f};

            /// <summary>
            /// How long does it take a jump engine to recharge?
            /// </summary>
            public const int JumpRechargeTime = (int)Constants.TimeInSeconds.FiveMinutes;

            /// <summary>
            /// Standard transit takes a while for jump effects to wear off.
            /// </summary>
            public const int StandardTransitPenalty = (int)Constants.TimeInSeconds.TwoMinutes;

            /// <summary>
            /// Squadrons recover very quickly.
            /// </summary>
            public const int SquadronTransitPenalty = (int)Constants.TimeInSeconds.ThirtySeconds;

        }

        /// <summary>
        /// List of game-specific settings.
        /// Since we don't have save/load yet, I'm just sticking this here.
        /// TODO: Move to correct place in the code.
        /// </summary>
        public static class GameSettings
        {
            // If true, Allows a faction from using a non-friendly faction's JumpGate. (True = default aurora)
            // TODO: Not currently functional as false. Factions have no relationships with each other yet.
            // Also Jump Construction modules and jump drives need to be implemented.
            // Should gates be phsyical things that can be destroyed or captured with marines? or should they be capturable/destroyable with a jump construction module only?
            public static bool AllowHostileGateJump = true;

            // Starting Build Points used for FastOOB.
            public static decimal FactionStartingShipBP = 8000m;
            public static decimal FactionStartingPDCBP = 4000m;

            // Base tracking speed factions start writh.
            public static int FactionBaseTrackingSpeed = 1250;

            // Base chance for each planet to generate a JP.
            public static int JumpPointGenerationChance = 10;

            // Base chance for a JP connection to loop to an already-generated system in it's local group.
            public static int JumpPointLocalGroupConnectionChance = 15;

            // Size of a LocalGroup.
            public static int JumpPointLocalGroupSize = 10;
        }
    }
}
