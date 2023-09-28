using System.Collections.Generic;
using Pulsar4X.Blueprints;

namespace Pulsar4X.Modding
{
    public class ModDataStore
    {
        public Dictionary<string, ArmorBlueprint> Armor { get; set; } = new ();
        public Dictionary<string, GasBlueprint> AtmosphericGas { get; set; } = new ();
        public Dictionary<string, CargoTypeBlueprint> CargoTypes { get; set; } = new ();
        public Dictionary<string, ComponentTemplateBlueprint> ComponentTemplates { get; set; } = new ();
        public Dictionary<string, IndustryTypeBlueprint> IndustryTypes { get; set; } = new();
        public Dictionary<string, MineralBlueprint> Minerals { get; set; } = new ();
        public Dictionary<string, ProcessedMaterialBlueprint> ProcessedMaterials { get; set; } = new ();
        public Dictionary<string, SystemGenSettingsBlueprint> SystemGenSettings { get; set; } = new ();
        public Dictionary<string, TechBlueprint> Techs { get; set; } = new ();
        public Dictionary<string, TechCategoryBlueprint> TechCategories { get; set; } = new ();
        public Dictionary<string, ThemeBlueprint> Themes { get; set; } = new ();
        public Dictionary<string, DefaultItemsBlueprint> DefaultItems { get; set; } = new ();
    }
}