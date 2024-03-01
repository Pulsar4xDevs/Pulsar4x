using Pulsar4X.ECSLib;

namespace Pulsar4X.Modding
{
    /// <summary>
    /// Stores all the game data per faction, since the factions will unlock things at their own pace
    /// </summary>
    public class FactionDataStoreDB : BaseDataBlob
    {
        /// <summary>
        /// Armor types that not been unlocked by the faction
        /// </summary>
        public SafeDictionary<string, ArmorBlueprint> LockedArmor { get; private set; }
        public SafeDictionary<string, CargoTypeBlueprint> LockedCargoTypes { get; private set; }
        public SafeDictionary<string, ComponentTemplateBlueprint> LockedComponentTemplates { get; private set; }
        public SafeDictionary<string, IndustryTypeBlueprint> LockedIndustryTypes { get; private set; }
        public SafeDictionary<string, MineralBlueprint> LockedMinerals { get; private set; }
        public SafeDictionary<string, ProcessedMaterialBlueprint> LockedProcessedMaterials { get; private set; }
        public SafeDictionary<string, TechBlueprint> LockedTechs { get; private set; }

        public SafeDictionary<string, ArmorBlueprint> Armor { get; private set; }
        public SafeDictionary<string, CargoTypeBlueprint> CargoTypes { get; private set; }
        public SafeDictionary<string, ComponentTemplateBlueprint> ComponentTemplates { get; private set; }
        public SafeDictionary<string, IndustryTypeBlueprint> IndustryTypes { get; private set; }
        public SafeDictionary<string, MineralBlueprint> Minerals { get; private set; }
        public SafeDictionary<string, ProcessedMaterialBlueprint> ProcessedMaterials { get; private set; }
        public SafeDictionary<string, TechBlueprint> Techs { get; private set; }

        public FactionDataStoreDB() { }

        public FactionDataStoreDB(ModDataStore modDataStore)
        {
            // By default all data is locked
            LockedArmor = new SafeDictionary<string, ArmorBlueprint>(modDataStore.Armor);
            LockedCargoTypes = new SafeDictionary<string, CargoTypeBlueprint>(modDataStore.CargoTypes);
            LockedComponentTemplates = new SafeDictionary<string, ComponentTemplateBlueprint>(modDataStore.ComponentTemplates);
            LockedIndustryTypes = new SafeDictionary<string, IndustryTypeBlueprint>(modDataStore.IndustryTypes);
            LockedMinerals = new SafeDictionary<string, MineralBlueprint>(modDataStore.Minerals);
            LockedProcessedMaterials = new SafeDictionary<string, ProcessedMaterialBlueprint>(modDataStore.ProcessedMaterials);
            LockedTechs = new SafeDictionary<string, TechBlueprint>(modDataStore.Techs);
        }
        public FactionDataStoreDB(FactionDataStoreDB other)
        {
            LockedArmor = new SafeDictionary<string, ArmorBlueprint>(other.LockedArmor);
            LockedCargoTypes = new SafeDictionary<string, CargoTypeBlueprint>(other.LockedCargoTypes);
            LockedComponentTemplates = new SafeDictionary<string, ComponentTemplateBlueprint>(other.LockedComponentTemplates);
            LockedIndustryTypes = new SafeDictionary<string, IndustryTypeBlueprint>(other.LockedIndustryTypes);
            LockedMinerals = new SafeDictionary<string, MineralBlueprint>(other.LockedMinerals);
            LockedProcessedMaterials = new SafeDictionary<string, ProcessedMaterialBlueprint>(other.LockedProcessedMaterials);
            LockedTechs = new SafeDictionary<string, TechBlueprint>(other.LockedTechs);

            Armor = new SafeDictionary<string, ArmorBlueprint>(other.Armor);
            CargoTypes = new SafeDictionary<string, CargoTypeBlueprint>(other.CargoTypes);
            ComponentTemplates = new SafeDictionary<string, ComponentTemplateBlueprint>(other.ComponentTemplates);
            IndustryTypes = new SafeDictionary<string, IndustryTypeBlueprint>(other.IndustryTypes);
            Minerals = new SafeDictionary<string, MineralBlueprint>(other.Minerals);
            ProcessedMaterials = new SafeDictionary<string, ProcessedMaterialBlueprint>(other.ProcessedMaterials);
            Techs = new SafeDictionary<string, TechBlueprint>(other.Techs);
        }

        public override object Clone()
        {
            return new FactionDataStoreDB(this);
        }
    }
}