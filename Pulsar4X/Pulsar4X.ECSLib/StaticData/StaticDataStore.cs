using Newtonsoft.Json;
using Pulsar4X.ECSLib.ComponentFeatureSets.CargoStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// This class acts as a simple store of all the games static data.
    /// It is saved alongside the rest of the game data.
    /// This class is generaly managed by the StaticDataManager.
    /// </summary>
    public class StaticDataStore
    {
        /// <summary>
        /// Easily convert string to Type
        /// </summary>
        private static readonly Dictionary<string, Type> StringsToTypes = InitializeStringsToTypes();

        /// <summary>
        /// Reverse dictionary of the above.
        /// </summary>
        private static readonly Dictionary<Type, string> TypesToStrings = InitializeTypesToStrings();

        /// <summary>
        /// List which stores all the atmospheric gases.
        /// </summary>
        [JsonIgnore]
        public WeightedList<AtmosphericGasSD> AtmosphericGases = new WeightedList<AtmosphericGasSD>();

        public AtmosphericGasSD GetAtmosGasByName(string name)
        {
            foreach (var gas in AtmosphericGases)
            {
                if (gas.Value.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                {
                    return gas.Value;
                }
            }

            throw new Exception("Atmospheric Gas " + name + " Not Found.");
        }

        public AtmosphericGasSD GetAtmosGasBySymbol(string chemicalSymbol)
        {
            foreach (var gas in AtmosphericGases)
            {
                if (gas.Value.ChemicalSymbol.Equals(chemicalSymbol, StringComparison.InvariantCultureIgnoreCase))
                {
                    return gas.Value;
                }
            }

            throw new Exception("Atmospheric Gas with symbol " + chemicalSymbol + " Not Found.");
        }

        /// <summary>
        /// List which stores all the Commander Name themes.
        /// </summary>
        [JsonIgnore]
        public List<CommanderNameThemeSD> CommanderNameThemes = new List<CommanderNameThemeSD>();

        /// <summary>
        /// Dictionary which stores all the Minerals.
        /// </summary>
        [JsonIgnore]
        public ICargoDefinitionsLibrary CargoGoods = new CargoDefinitionsLibrary();

        /// <summary>
        /// Dictionary which stores all the Technologies.
        /// stored in a dictionary to allow fast lookup of a specific Technology based on its guid.
        /// </summary>
        [JsonIgnore]
        public Dictionary<Guid, TechSD> Techs = new Dictionary<Guid, TechSD>();
        
        /// <summary>
        /// Dictionary which stores all Components.
        /// </summary>
        [JsonIgnore]
        public Dictionary<Guid, ComponentTemplateSD> ComponentTemplates = new Dictionary<Guid, ComponentTemplateSD>();

        /// <summary>
        /// Dictionary to store CargoTypes
        /// </summary>
        [JsonIgnore]
        public Dictionary<Guid, CargoTypeSD> CargoTypes = new Dictionary<Guid, CargoTypeSD>();

        [JsonIgnore]
        public Dictionary<Guid, IndustryTypeSD> IndustryTypes = new Dictionary<Guid, IndustryTypeSD>();
        
        public Dictionary<Guid, ArmorSD> ArmorTypes = new Dictionary<Guid, ArmorSD>();
        
        /// <summary>
        /// Settings used by system generation. 
        /// @todo make Galaxy gen use this instead of default data (DO NOT DELETE THE HARD CODED DATA THO, that should be a fall back).
        /// </summary>
        public SystemGenSettingsSD SystemGenSettings;

        /// <summary>
        /// This list holds the version info of all the loaded data sets.
        /// </summary>
        [PublicAPI]
        [JsonIgnore]
        public List<DataVersionInfo> LoadedDataSets => _loadedDataSets;

        [JsonProperty]
        private List<DataVersionInfo> _loadedDataSets;

        public StaticDataStore()
        {
            _loadedDataSets = new List<DataVersionInfo>();
        }

        #region Static field initializers

        /// <summary>
        /// Initializes the StringsToTypes static dictionary.
        /// </summary>
        private static Dictionary<string, Type> InitializeStringsToTypes()
        {
            return new Dictionary<string, Type>
            {
                {
                    "AtmosphericGases", typeof(WeightedList<AtmosphericGasSD>)
                },
                {
                    "CommanderNameThemes", typeof(List<CommanderNameThemeSD>)
                },
                {
                    "Minerals", typeof(Dictionary<Guid, MineralSD>)
                },
                {
                    "Techs", typeof(Dictionary<Guid, TechSD>)
                },
                {
                    "ProcessedMaterials", typeof(Dictionary<Guid, ProcessedMaterialSD>)
                },
                {
                    "ComponentTemplates", typeof(Dictionary<Guid, ComponentTemplateSD>)
                },
                {
                    "CargoTypes", typeof(Dictionary<Guid, CargoTypeSD>)
                },
                {
                    "IndustryTypes", typeof(Dictionary<Guid, IndustryTypeSD>)
                },
                {
                    "ArmorTypes", typeof(Dictionary<Guid, ArmorSD>)
                },
                {
                    "SystemGenSettings", typeof(SystemGenSettingsSD)
                },
                {
                    "VersionInfo", typeof(VersionInfo)
                }
            };
        }

        /// <summary>
        /// Initializes the TypesToStrings static dictionary.
        /// //todo work out if this is safe? doe .Net gurentee that InitializeStringsToTypes() is called first somehow??
        /// </summary>
        /// <returns></returns>
        private static Dictionary<Type, string> InitializeTypesToStrings()
        {
            return new Dictionary<Type, string>
            {
                {
                    typeof(WeightedList<AtmosphericGasSD>), "AtmosphericGases"
                },
                {
                    typeof(List<CommanderNameThemeSD>), "CommanderNameThemes"
                },
                {
                    typeof(List<MineralSD>), "Minerals"
                },
                {
                    typeof(Dictionary<Guid, TechSD>), "Techs"
                },
                {
                    typeof(Dictionary<Guid, ProcessedMaterialSD>), "RefinedMaterials"
                },
                {
                    typeof(Dictionary<Guid, ComponentTemplateSD>), "Components"
                },
                {
                    typeof(Dictionary<Guid, CargoTypeSD>), "CargoTypes"
                },
                {
                    typeof(Dictionary<Guid, IndustryTypeSD>), "IndustryTypes"
                },
                {
                    typeof(Dictionary<Guid, ArmorSD>), "ArmorTypes"
                },
                {
                    typeof(SystemGenSettingsSD), "SystemGenSettings"
                },
                {
                    typeof(VersionInfo), "VersionInfo"
                }
            };
        }

        #endregion

        #region Public API

        /// <summary>
        /// This functin goes through each Static Data type in the store looking for one that has an ID that
        /// matches the one provided.
        /// Returns null if the id is not found.
        /// </summary>
        [PublicAPI]
        [CanBeNull]
        public object FindDataObjectUsingID(Guid id)
        {
            var cargoGood = CargoGoods.GetAny(id);
            if (cargoGood != null)
                return cargoGood;
            
            if (Techs.ContainsKey(id))
                return Techs[id];

            if (ComponentTemplates.ContainsKey(id))
                return ComponentTemplates[id];

            if (CargoTypes.ContainsKey(id))
                return CargoTypes[id];

            return null;
        }

        public Dictionary<Guid, Guid> StorageTypeMap = new Dictionary<Guid, Guid>();
        internal void SetStorageTypeMap()
        {
            StorageTypeMap.Clear();
            var allCargoDefs = CargoGoods.GetAll();
            foreach (var item in allCargoDefs)          
                StorageTypeMap.Add(item.Key, item.Value.CargoTypeID);
            foreach (var item in ComponentTemplates)
                StorageTypeMap.Add(item.Key, item.Value.CargoTypeID);
        }


        public ICargoable GetICargoable(Guid id)
        {
            return (ICargoable)CargoGoods.GetAny(id);
        }

        #endregion

        #region Private functions

        #region private void Store(dynamic object) overloads.

        /// <summary>
        /// Stores Atmospheric Gas Static Data.
        /// </summary>
        internal void Store(WeightedList<AtmosphericGasSD> atmosphericGases)
        {
            if (atmosphericGases != null)
            {
                foreach (WeightedValue<AtmosphericGasSD> atmosphericGas in atmosphericGases)
                {
                    if (AtmosphericGases.ContainsValue(atmosphericGas.Value))
                    {
                        // Update existing value
                        int index = AtmosphericGases.IndexOf(atmosphericGas.Value);
                        AtmosphericGases[index] = atmosphericGas;
                    }
                    else
                    {
                        // Add new value
                        AtmosphericGases.Add(atmosphericGas);
                    }
                }
            }
        }

        /// <summary>
        /// Stores Commander Name Themes.
        /// </summary>
        internal void Store(List<CommanderNameThemeSD> commanderNameThemes)
        {
            if (commanderNameThemes != null)
            {
                foreach (CommanderNameThemeSD commanderNameThemeSD in commanderNameThemes)
                {
                    if (CommanderNameThemes.Contains(commanderNameThemeSD))
                    {
                        // Update existing value.
                        int index = CommanderNameThemes.IndexOf(commanderNameThemeSD);
                        CommanderNameThemes[index] = commanderNameThemeSD;
                    }
                    else
                    {
                        // Add new value.
                        CommanderNameThemes.Add(commanderNameThemeSD);
                    }
                }
            }
        }

        /// <summary>
        /// Stores Mineral Static Data. Will overwrite an existing mineral if the IDs match.
        /// </summary>
        internal void Store(Dictionary<Guid, MineralSD> minerals)
        {
            if (minerals != null)
            {
                CargoGoods.LoadMineralDefinitions(minerals.Values.ToList());
            }
        }

        /// <summary>
        /// Stores Technology Static Data. Will overwrite any existing Techs with the same ID.
        /// </summary>
        internal void Store(Dictionary<Guid, TechSD> techs)
        {
            if (techs != null)
            {
                foreach (KeyValuePair<Guid, TechSD> tech in techs)
                    Techs[tech.Key] = tech.Value; // replace existing value or insert a new one as required.
            }
        }


        /// <summary>
        /// Stores ConstructableObj Static Data. Will overwrite any existing ConstructableObjs with the same ID.
        /// </summary>
        internal void Store(Dictionary<Guid, ProcessedMaterialSD> recipes)
        {
            if (recipes != null)
            {
                CargoGoods.LoadMaterialsDefinitions(recipes.Values.ToList());
            }
        }

        /// <summary>
        /// Stores Component Static Data. Will overwrite any existing Component with the same ID.
        /// </summary>
        internal void Store(Dictionary<Guid, ComponentTemplateSD> components)
        {
            if (components != null)
            {
                foreach (KeyValuePair<Guid, ComponentTemplateSD> component in components)
                {
                    ComponentTemplates[component.Key] = component.Value;

                }
            }
        }

        /// <summary>
        /// Stores cargoType Static Data. Will overwrite any existing Component with the same ID.
        /// </summary>
        internal void Store(Dictionary<Guid, CargoTypeSD> cargoTypes)
        {
            if (cargoTypes != null)
            {
                foreach (KeyValuePair<Guid, CargoTypeSD> typeKVP in cargoTypes)
                    CargoTypes[typeKVP.Key] = typeKVP.Value;
            }
        }

        internal void Store(Dictionary<Guid, IndustryTypeSD> industryTypes)
        {
            if (industryTypes != null)
            {
                foreach (KeyValuePair<Guid,IndustryTypeSD> kvp in industryTypes)
                {
                    IndustryTypes[kvp.Key] = kvp.Value;
                }
            }
        }
        
        internal void Store(Dictionary<Guid, ArmorSD> armorTypes)
        {
            if (armorTypes != null)
            {
                foreach (KeyValuePair<Guid,ArmorSD> kvp in armorTypes)
                {
                    ArmorTypes[kvp.Key] = kvp.Value;
                }
            }
        }

        internal void Store(SystemGenSettingsSD settings)
        {
            SystemGenSettings = settings;
        }

        #endregion

        /// <summary>
        /// Returns a type custom string for a type of static data. This string is used to tell 
        /// what type of static data is being imported (and is thus exported as well). 
        /// </summary>
        public static string GetTypeString(Type type)
        {
            string s;
            TypesToStrings.TryGetValue(type, out s);
            return s;
        }

        /// <summary>
        /// Gets the matching type for a type string. Used when importing previously exported 
        /// static data to know what type to import it as.
        /// </summary>
        public static Type GetType(string typeString)
        {
            return StringsToTypes[typeString];
        }

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            foreach (string dataSet in _loadedDataSets.Select(dataVersionInfo => dataVersionInfo.Directory).ToList())
            {
                StaticDataManager.LoadData(dataSet, (Game)context.Context);
            }
            SetStorageTypeMap();
        }
        #endregion

    }

   
}