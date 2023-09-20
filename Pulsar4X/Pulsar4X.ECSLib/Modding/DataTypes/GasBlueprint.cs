namespace Pulsar4X.Modding
{
    public class GasBlueprint : SerializableGameData
    {
        public double Weight { get; set; }

        /// <summary>
        /// Common name of the gas.
        /// </summary>
        public string Name { get; set;}

        /// <summary>
        /// Chemical symbol of the gas. This is used like the ID of the gas.
        /// </summary>
        public string ChemicalSymbol { get; set;}

        /// <summary>
        /// Indicates weither or not the gas is toxic for the purpose of colony cost.
        /// </summary>
        public bool IsToxic { get; set;}

        /// <summary>
        /// Indicates at what percent of atmosphere this gas becomes toxic for the purpose of colony cost.
        /// </summary>
        public float? IsToxicAtPercentage { get; set;}

        /// <summary>
        /// Indicates weither or not the gas is highly toxic for the purpose of colony cost.
        /// </summary>
        public bool IsHighlyToxic { get; set;}

        /// <summary>
        /// Indicates at what percent of atmosphere this gas becomes highly toxic for the purpose of colony cost.
        /// </summary>
        public float? IsHighlyToxicAtPercentage { get; set;}

        /// <summary>
        /// The point at which the gas boils, i.e. goes from being a liqued to a gas.
        /// </summary>
        public double BoilingPoint { get; set;}

        /// <summary>
        /// The point at which the gas freezes, i.e. goes from being a liqued to a solid.
        /// </summary>
        public double MeltingPoint { get; set;}

        /// <summary>
        /// The minium surface gravity (in g) required for a world to hold on to this gas, heaver gases should have lower values, lighter gases higher values.
        /// This value is only used during system generation, if you want to exclude a gas from system generation, just make this value very high (like 1000).
        /// </summary>
        public double MinGravity { get; set;}

        /// <summary>
        /// A value representing the Greenhouse effect this gas has (if any).
        ///  0 = Inert/No Effect
        ///  A negative number would be an Anti-Greenhouse gas.
        ///  A positive Number would be a Greenhouse gas.
        ///  The Magnitude of the number could be used to have different gases have a greater or lesser greenhouse effect.
        /// </summary>
        public double GreenhouseEffect { get; set;}
    }
}