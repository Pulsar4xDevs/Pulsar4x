namespace Pulsar4X.Blueprints
{
    public class IndustryTypeBlueprint : Blueprint
    {
        public string Name { get; set; }

        /// <summary>
        /// Build energy in kWh per industry point of this type (energy economy phase 3):
        /// 10 for fabrication (component, installation, ordnance), 5 for ship assembly, and
        /// 7 for refining as the average a recipe without its own EnergyCost falls back to.
        /// 0 (the default) means the type costs no energy.
        /// </summary>
        public double EnergyPerPoint { get; set; }
    }
}
