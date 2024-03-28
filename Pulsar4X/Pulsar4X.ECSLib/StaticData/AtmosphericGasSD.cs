
namespace Pulsar4X.ECSLib
{
    [StaticData(false)]
    public class AtmosphericGasSD
    {
        /// <summary>
        /// Common name of the gas.
        /// </summary>
        public string Name;

        /// <summary>
        /// Chemical symbol of the gas. This is used like the ID of the gas.
        /// </summary>
        public string ChemicalSymbol;

        /// <summary>
        /// Indicates weither or not the gas is toxic for the purpose of colony cost.
        /// </summary>
        public bool IsToxic;

        /// <summary>
        /// Indicates at what percent of atmosphere this gas becomes toxic for the purpose of colony cost.
        /// </summary>
        public float? IsToxicAtPercentage;

        /// <summary>
        /// Indicates weither or not the gas is highly toxic for the purpose of colony cost.
        /// </summary>
        public bool IsHighlyToxic;

        /// <summary>
        /// Indicates at what percent of atmosphere this gas becomes highly toxic for the purpose of colony cost.
        /// </summary>
        public float? IsHighlyToxicAtPercentage;

        /// <summary>
        /// The point at which the gas boils, i.e. goes from being a liqued to a gas.
        /// </summary>
        public double BoilingPoint;

        /// <summary>
        /// The point at which the gas freezes, i.e. goes from being a liqued to a solid.
        /// </summary>
        public double MeltingPoint;

        /// <summary>
        /// The minium surface gravity (in g) required for a world to hold on to this gas, heaver gases should have lower values, lighter gases higher values.
        /// This value is only used during system generation, if you want to exclude a gas from system generation, just make this value very high (like 1000).
        /// </summary>
        public double MinGravity;

        /// <summary>
        /// A value representing the Greenhouse effect this gas has (if any).
        ///  0 = Inert/No Effect
        ///  A negative number would be an Anti-Greenhouse gas.
        ///  A positive Number would be a Greenhouse gas.
        ///  The Magnitude of the number could be used to have different gases have a greater or lesser greenhouse effect.
        /// </summary>
        public double GreenhouseEffect;

        public bool Equals(AtmosphericGasSD other)
        {
            return string.Equals(ChemicalSymbol, other.ChemicalSymbol);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            return obj is AtmosphericGasSD && Equals((AtmosphericGasSD)obj);
        }

        public override int GetHashCode()
        {
            return ChemicalSymbol?.GetHashCode() ?? 0;
        }
    }
}
