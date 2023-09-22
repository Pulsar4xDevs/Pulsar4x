using System.Collections.Generic;
using System.Linq;
using Pulsar4X.DataStructures;
using Pulsar4X.Interfaces;
using Pulsar4X.Blueprints;

namespace Pulsar4X.Modding
{
    /// <summary>
    /// Stores all the game data per faction, since the factions will unlock things at their own pace
    /// </summary>
    public class FactionDataStore
    {
        /// <summary>
        /// Armor types that not been unlocked by the faction
        /// </summary>
        public SafeDictionary<string, ArmorBlueprint> LockedArmor { get; private set; }
        public SafeDictionary<string, CargoTypeBlueprint> LockedCargoTypes { get; private set; }
        public SafeDictionary<string, ComponentTemplateBlueprint> LockedComponentTemplates { get; private set; }
        public SafeDictionary<string, IndustryTypeBlueprint> LockedIndustryTypes { get; private set; }
        public SafeDictionary<string, TechBlueprint> LockedTechs { get; private set; }

        public SafeDictionary<string, ArmorBlueprint> Armor { get; private set; }
        public SafeDictionary<string, CargoTypeBlueprint> CargoTypes { get; private set; }
        public SafeDictionary<string, ComponentTemplateBlueprint> ComponentTemplates { get; private set; }
        public SafeDictionary<string, IndustryTypeBlueprint> IndustryTypes { get; private set; }
        public SafeDictionary<string, TechBlueprint> Techs { get; private set; }

        public CargoDefinitionsLibrary LockedCargoGoods { get; private set; }
        public CargoDefinitionsLibrary CargoGoods { get; private set; }

        public FactionDataStore()
        {
            LockedArmor = new ();
            LockedCargoTypes = new ();
            LockedComponentTemplates = new ();
            LockedIndustryTypes = new ();
            LockedTechs = new ();

            Armor = new ();
            CargoTypes = new ();
            ComponentTemplates = new ();
            IndustryTypes = new ();
            Techs = new ();

            LockedCargoGoods = new ();
            CargoGoods = new ();
        }

        public FactionDataStore(ModDataStore modDataStore)
        {
            // By default all data is locked
            LockedArmor = new SafeDictionary<string, ArmorBlueprint>(modDataStore.Armor);
            LockedCargoTypes = new SafeDictionary<string, CargoTypeBlueprint>(modDataStore.CargoTypes);
            LockedComponentTemplates = new SafeDictionary<string, ComponentTemplateBlueprint>(modDataStore.ComponentTemplates);
            LockedIndustryTypes = new SafeDictionary<string, IndustryTypeBlueprint>(modDataStore.IndustryTypes);
            LockedTechs = new SafeDictionary<string, TechBlueprint>(modDataStore.Techs);

            LockedCargoGoods = new CargoDefinitionsLibrary(modDataStore.Minerals.Values.ToList(), modDataStore.ProcessedMaterials.Values.ToList(), new List<ICargoable>());
            CargoGoods = new CargoDefinitionsLibrary();
        }
        public FactionDataStore(FactionDataStore other)
        {
            LockedArmor = new SafeDictionary<string, ArmorBlueprint>(other.LockedArmor);
            LockedCargoTypes = new SafeDictionary<string, CargoTypeBlueprint>(other.LockedCargoTypes);
            LockedComponentTemplates = new SafeDictionary<string, ComponentTemplateBlueprint>(other.LockedComponentTemplates);
            LockedIndustryTypes = new SafeDictionary<string, IndustryTypeBlueprint>(other.LockedIndustryTypes);
            LockedTechs = new SafeDictionary<string, TechBlueprint>(other.LockedTechs);

            Armor = new SafeDictionary<string, ArmorBlueprint>(other.Armor);
            CargoTypes = new SafeDictionary<string, CargoTypeBlueprint>(other.CargoTypes);
            ComponentTemplates = new SafeDictionary<string, ComponentTemplateBlueprint>(other.ComponentTemplates);
            IndustryTypes = new SafeDictionary<string, IndustryTypeBlueprint>(other.IndustryTypes);
            Techs = new SafeDictionary<string, TechBlueprint>(other.Techs);

            LockedCargoGoods = new CargoDefinitionsLibrary(other.LockedCargoGoods);
            CargoGoods = new CargoDefinitionsLibrary(other.CargoGoods);
        }

        public void Unlock(string id)
        {
            if(LockedArmor.ContainsKey(id))
            {
                var thing = LockedArmor[id];
                LockedArmor.Remove(id);
                Armor.Add(id, thing);
            }
            else if(LockedCargoTypes.ContainsKey(id))
            {
                var thing = LockedCargoTypes[id];
                LockedCargoTypes.Remove(id);
                CargoTypes.Add(id, thing);
            }
            else if(LockedComponentTemplates.ContainsKey(id))
            {
                var thing = LockedComponentTemplates[id];
                LockedComponentTemplates.Remove(id);
                ComponentTemplates.Add(id, thing);
            }
            else if(LockedIndustryTypes.ContainsKey(id))
            {
                var thing = LockedIndustryTypes[id];
                LockedIndustryTypes.Remove(id);
                IndustryTypes.Add(id, thing);
            }
            else if(LockedTechs.ContainsKey(id))
            {
                var thing = LockedTechs[id];
                LockedTechs.Remove(id);
                Techs.Add(id, thing);
            }
            else if(LockedCargoGoods.Contains(id))
            {
                if(LockedCargoGoods.IsMaterial(id))
                {
                    var thing = LockedCargoGoods.GetMaterial(id);
                    LockedCargoGoods.Remove(thing);
                    CargoGoods.Add(thing);
                }
                else if(LockedCargoGoods.IsMineral(id))
                {
                    var thing = LockedCargoGoods.GetMineral(id);
                    LockedCargoGoods.Remove(thing);
                    CargoGoods.Add(thing);
                }
                else
                {
                    var thing = LockedCargoGoods.GetOther(id);
                    LockedCargoGoods.Remove(thing);
                    CargoGoods.Add(thing);
                }
            }
        }
    }
}