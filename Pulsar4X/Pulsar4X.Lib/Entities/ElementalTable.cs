using System.Collections.Generic;

namespace Pulsar4X.Entities
{
    public sealed class ElementalTable : List<Elemental>
    {
        private static readonly ElementalTable _instance = new ElementalTable();
        public static ElementalTable Instance { get { return _instance; } }

        static ElementalTable() { }
        private ElementalTable()
        {
            //TODO: put this in file and load on start?
            //load elements into self 
            Add(new Elemental { Id = Constants.Gasses.AtomicNumbers.AN_H, Symbol = "H", Name = "Hydrogen", AtomicWeight = 1.0079, MeltingPoint = 14.06, BoilingPoint = 20.40, Density = 8.99e-05, AbundanceE = 0.00125893, AbundanceS = 27925.4, Reactivity = 1, MaximumInspiredPartialPressure = 0.0D });
            Add(new Elemental { Id = Constants.Gasses.AtomicNumbers.AN_HE, Symbol = "He", Name = "Helium", AtomicWeight = 4.0026, MeltingPoint = 3.46, BoilingPoint = 4.20, Density = 0.0001787, AbundanceE = 7.94328e-09, AbundanceS = 2722.7, Reactivity = 0, MaximumInspiredPartialPressure = Constants.Gasses.InspiredPartialPressure.MAX_HE_IPP });
            Add(new Elemental { Id = Constants.Gasses.AtomicNumbers.AN_N, Symbol = "N", Name = "Nitrogen", AtomicWeight = 14.0067, MeltingPoint = 63.34, BoilingPoint = 77.40, Density = 0.0012506, AbundanceE = 1.99526e-05, AbundanceS = 3.13329, Reactivity = 0, MaximumInspiredPartialPressure = Constants.Gasses.InspiredPartialPressure.MAX_N2_IPP });
            Add(new Elemental { Id = Constants.Gasses.AtomicNumbers.AN_O, Symbol = "O", Name = "Oxygen", AtomicWeight = 15.9994, MeltingPoint = 54.80, BoilingPoint = 90.20, Density = 0.001429, AbundanceE = 0.501187, AbundanceS = 23.8232, Reactivity = 10, MaximumInspiredPartialPressure = Constants.Gasses.InspiredPartialPressure.MAX_O2_IPP });
            Add(new Elemental { Id = Constants.Gasses.AtomicNumbers.AN_NE, Symbol = "Ne", Name = "Neon", AtomicWeight = 20.1700, MeltingPoint = 24.53, BoilingPoint = 27.10, Density = 0.0009, AbundanceE = 5.01187e-09, AbundanceS = 3.4435e-5, Reactivity = 0, MaximumInspiredPartialPressure = Constants.Gasses.InspiredPartialPressure.MAX_NE_IPP });
            Add(new Elemental { Id = Constants.Gasses.AtomicNumbers.AN_AR, Symbol = "Ar", Name = "Argon", AtomicWeight = 39.9480, MeltingPoint = 84.00, BoilingPoint = 87.30, Density = 0.0017824, AbundanceE = 3.16228e-06, AbundanceS = 0.100925, Reactivity = 0, MaximumInspiredPartialPressure = Constants.Gasses.InspiredPartialPressure.MAX_AR_IPP });
            Add(new Elemental { Id = Constants.Gasses.AtomicNumbers.AN_KR, Symbol = "Kr", Name = "Krypton", AtomicWeight = 83.8000, MeltingPoint = 116.60, BoilingPoint = 119.70, Density = 0.003708, AbundanceE = 1e-10, AbundanceS = 4.4978e-05, Reactivity = 0, MaximumInspiredPartialPressure = Constants.Gasses.InspiredPartialPressure.MAX_KR_IPP });
            Add(new Elemental { Id = Constants.Gasses.AtomicNumbers.AN_XE, Symbol = "Xe", Name = "Xenon", AtomicWeight = 131.3000, MeltingPoint = 161.30, BoilingPoint = 165.00, Density = 0.00588, AbundanceE = 3.16228e-11, AbundanceS = 4.69894e-06, Reactivity = 0, MaximumInspiredPartialPressure = Constants.Gasses.InspiredPartialPressure.MAX_XE_IPP });
            Add(new Elemental { Id = Constants.Gasses.AtomicNumbers.AN_NH3, Symbol = "NH3", Name = "Ammonia", AtomicWeight = 17.0000, MeltingPoint = 195.46, BoilingPoint = 239.66, Density = 0.001, AbundanceE = 0.002, AbundanceS = 0.0001, Reactivity = 1, MaximumInspiredPartialPressure = Constants.Gasses.InspiredPartialPressure.MAX_NH3_IPP });
            Add(new Elemental { Id = Constants.Gasses.AtomicNumbers.AN_H2O, Symbol = "H2O", Name = "Water", AtomicWeight = 18.0000, MeltingPoint = 273.16, BoilingPoint = 373.16, Density = 1.000, AbundanceE = 0.03, AbundanceS = 0.001, Reactivity = 0, MaximumInspiredPartialPressure = 0.0D });
            Add(new Elemental { Id = Constants.Gasses.AtomicNumbers.AN_CO2, Symbol = "CO2", Name = "CarbonDioxide", AtomicWeight = 44.0000, MeltingPoint = 194.66, BoilingPoint = 194.66, Density = 0.001, AbundanceE = 0.01, AbundanceS = 0.0005, Reactivity = 0, MaximumInspiredPartialPressure = Constants.Gasses.InspiredPartialPressure.MAX_CO2_IPP });
            Add(new Elemental { Id = Constants.Gasses.AtomicNumbers.AN_O3, Symbol = "O3", Name = "Ozone", AtomicWeight = 48.0000, MeltingPoint = 80.16, BoilingPoint = 161.16, Density = 0.001, AbundanceE = 0.001, AbundanceS = 0.000001, Reactivity = 2, MaximumInspiredPartialPressure = Constants.Gasses.InspiredPartialPressure.MAX_O3_IPP });
            Add(new Elemental { Id = Constants.Gasses.AtomicNumbers.AN_CH4, Symbol = "CH4", Name = "Methane", AtomicWeight = 16.0000, MeltingPoint = 90.16, BoilingPoint = 109.16, Density = 0.010, AbundanceE = 0.005, AbundanceS = 0.0001, Reactivity = 1, MaximumInspiredPartialPressure = Constants.Gasses.InspiredPartialPressure.MAX_CH4_IPP });
            Add(new Elemental { Id = Constants.Gasses.AtomicNumbers.AN_F, Symbol = "F", Name = "Fluorine", AtomicWeight = 18.9984, MeltingPoint = 53.58, BoilingPoint = 85.10, Density = 0.001696, AbundanceE = 0.000630957, AbundanceS = 0.000843335, Reactivity = 50, MaximumInspiredPartialPressure = Constants.Gasses.InspiredPartialPressure.MAX_F_IPP });
            Add(new Elemental { Id = Constants.Gasses.AtomicNumbers.AN_CL, Symbol = "Cl", Name = "Chlorine", AtomicWeight = 35.4530, MeltingPoint = 172.22, BoilingPoint = 239.20, Density = 0.003214, AbundanceE = 0.000125893, AbundanceS = 0.005236, Reactivity = 40, MaximumInspiredPartialPressure = Constants.Gasses.InspiredPartialPressure.MAX_CL_IPP });

            ForEach(x =>
            {
                if (x.MaximumInspiredPartialPressure == 0.0D)
                    x.MaximumInspiredPartialPressure = Constants.Units.INCREDIBLY_LARGE_NUMBER;
            });

            //TODO: sort by diminishing abundance (abunds * abunde)
        }

    }
}
