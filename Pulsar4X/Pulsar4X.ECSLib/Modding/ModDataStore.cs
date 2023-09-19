using System.Collections.Generic;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Modding
{
    public class ModDataStore
    {
        public Dictionary<string, ArmorSD> Armor { get; set; } = new Dictionary<string, ArmorSD>();
        public Dictionary<string, AtmosphericGasSD> AtmosphericGas { get; set; } = new Dictionary<string, AtmosphericGasSD>();
        public Dictionary<string, CargoTypeSD> CargoTypes { get; set; } = new Dictionary<string, CargoTypeSD>();
        public Dictionary<string, ComponentTemplateSD> ComponentTemplates { get; set; } = new Dictionary<string, ComponentTemplateSD>();
        public Dictionary<string, MineralSD> Minerals { get; set; } = new Dictionary<string, MineralSD>();
        public Dictionary<string, ProcessedMaterialSD> ProcessedMaterials { get; set; } = new Dictionary<string, ProcessedMaterialSD>();
        public Dictionary<string, SystemGenSettingsSD> SystemGenSettings { get; set; } = new Dictionary<string, SystemGenSettingsSD>();
        public Dictionary<string, TechSD> Techs { get; set; } = new Dictionary<string, TechSD>();
        public Dictionary<string, ThemeSD> Themes { get; set; } = new Dictionary<string, ThemeSD>();
    }
}