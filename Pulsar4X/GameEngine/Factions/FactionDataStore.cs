using System.Collections.Generic;
using System.Linq;
using Pulsar4X.DataStructures;
using Pulsar4X.Interfaces;
using Pulsar4X.Blueprints;
using Pulsar4X.Modding;

namespace Pulsar4X.Engine
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
        public SafeDictionary<string, Tech> LockedTechs { get; private set; }
        public SafeDictionary<string, ArmorBlueprint> Armor { get; private set; }
        public SafeDictionary<string, CargoTypeBlueprint> CargoTypes { get; private set; }
        public SafeDictionary<string, ComponentTemplateBlueprint> ComponentTemplates { get; private set; }
        public SafeDictionary<string, IndustryTypeBlueprint> IndustryTypes { get; private set; }
        public SafeDictionary<string, Tech> Techs { get; private set; }

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
            LockedTechs = new ();
            foreach(var (id, techBlueprint) in modDataStore.Techs)
            {
                LockedTechs.Add(id, new Tech(techBlueprint));
            }

            Armor = new ();
            CargoTypes = new ();
            ComponentTemplates = new ();
            IndustryTypes = new ();
            Techs = new ();

            LockedCargoGoods = new CargoDefinitionsLibrary(modDataStore.Minerals.Values.ToList(), modDataStore.ProcessedMaterials.Values.ToList(), new List<ICargoable>());
            CargoGoods = new CargoDefinitionsLibrary();
        }
        public FactionDataStore(FactionDataStore other)
        {
            LockedArmor = new SafeDictionary<string, ArmorBlueprint>(other.LockedArmor);
            LockedCargoTypes = new SafeDictionary<string, CargoTypeBlueprint>(other.LockedCargoTypes);
            LockedComponentTemplates = new SafeDictionary<string, ComponentTemplateBlueprint>(other.LockedComponentTemplates);
            LockedIndustryTypes = new SafeDictionary<string, IndustryTypeBlueprint>(other.LockedIndustryTypes);
            LockedTechs = new SafeDictionary<string, Tech>(other.LockedTechs);

            Armor = new SafeDictionary<string, ArmorBlueprint>(other.Armor);
            CargoTypes = new SafeDictionary<string, CargoTypeBlueprint>(other.CargoTypes);
            ComponentTemplates = new SafeDictionary<string, ComponentTemplateBlueprint>(other.ComponentTemplates);
            IndustryTypes = new SafeDictionary<string, IndustryTypeBlueprint>(other.IndustryTypes);
            Techs = new SafeDictionary<string, Tech>(other.Techs);

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

        public string GetName(string id)
        {
            if(LockedArmor.ContainsKey(id)) return CargoGoods[LockedArmor[id].ResourceID].Name;
            if(Armor.ContainsKey(id)) return CargoGoods[Armor[id].ResourceID].Name;
            if(LockedCargoTypes.ContainsKey(id)) return LockedCargoTypes[id].Name;
            if(CargoTypes.ContainsKey(id)) return CargoTypes[id].Name;
            if(LockedComponentTemplates.ContainsKey(id)) return LockedComponentTemplates[id].Name;
            if(ComponentTemplates.ContainsKey(id)) return ComponentTemplates[id].Name;
            if(LockedIndustryTypes.ContainsKey(id)) return LockedIndustryTypes[id].Name;
            if(IndustryTypes.ContainsKey(id)) return IndustryTypes[id].Name;
            if(LockedTechs.ContainsKey(id)) return LockedTechs[id].Name;
            if(Techs.ContainsKey(id)) return Techs[id].Name;
            if(LockedCargoGoods.Contains(id)) return LockedCargoGoods[id].Name;
            if(CargoGoods.Contains(id)) return CargoGoods[id].Name;

            return string.Empty;
        }

        public bool IsResearchable(string id)
        {
            return Techs.ContainsKey(id)
                    && Techs[id].Level < Techs[id].MaxLevel;
        }

        public void IncrementTechLevel(string id)
        {
            if(Techs.ContainsKey(id)) IncrementTechLevel(Techs[id]);
        }

        public void IncrementTechLevel(Tech tech)
        {
            if(!Techs.ContainsKey(tech.UniqueID)) return;

            tech.Level++;
            tech.ResearchProgress = 0;
            tech.ResearchCost = tech.TechCostFormula();

            if(tech.Unlocks.ContainsKey(tech.Level))
            {
                foreach(var item in tech.Unlocks[tech.Level])
                {
                    Unlock(item);

                    if(Techs.ContainsKey(item))
                    {
                        var unlockedTech = (Tech)Techs[item];
                        unlockedTech.ResearchCost = unlockedTech.TechCostFormula();
                    }
                }
            }
        }

        internal void AddTechPoints(Tech tech, int pointsToAdd)
        {
            if(!Techs.ContainsKey(tech.UniqueID)) return;

            var newPointsTotal = tech.ResearchProgress + pointsToAdd;
            if(newPointsTotal >= tech.ResearchCost)
            {
                int remainder = newPointsTotal - tech.ResearchCost;
                IncrementTechLevel(tech);
                tech.ResearchProgress = remainder;
            }
            else
            {
                tech.ResearchProgress = newPointsTotal;
            }
        }
    }
}