using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        /// <summary>
        /// List which stores all the Commander Name themes.
        /// </summary>
        [JsonIgnore]
        public List<CommanderNameThemeSD> CommanderNameThemes = new List<CommanderNameThemeSD>();

        /// <summary>
        /// List which stores all the Minerals.
        /// </summary>
        [JsonIgnore]
        public List<MineralSD> Minerals = new List<MineralSD>();

        /// <summary>
        /// Dictionary which stores all the Technologies.
        /// stored in a dictionary to allow fast lookup of a specific Technology based on its guid.
        /// </summary>
        [JsonIgnore]
        public Dictionary<Guid, TechSD> Techs = new Dictionary<Guid, TechSD>();

        /// <summary>
        /// List which stores all of the installations
        /// </summary>
        [JsonIgnore]
        public Dictionary<Guid, InstallationSD> Installations = new Dictionary<Guid, InstallationSD>();

        /// <summary>
        /// Dictionary which stores all the Recipes.
        /// </summary>
        [JsonIgnore]
        public Dictionary<Guid, RefinedMaterialSD> RefinedMaterials = new Dictionary<Guid, RefinedMaterialSD>();

        /// <summary>
        /// Dictionary which stores all Components.
        /// </summary>
        [JsonIgnore]
        public Dictionary<Guid, ComponentSD> Components = new Dictionary<Guid, ComponentSD>();

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
                    "Minerals", typeof(List<MineralSD>)
                },
                {
                    "Techs", typeof(Dictionary<Guid, TechSD>)
                },
                {
                    "Installations", typeof(Dictionary<Guid, InstallationSD>)
                },
                {
                    "RefinedMaterials", typeof(Dictionary<Guid, RefinedMaterialSD>)
                },
                {
                    "Components", typeof(Dictionary<Guid, ComponentSD>)
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
                    typeof(Dictionary<Guid, InstallationSD>), "Installations"
                },
                {
                    typeof(Dictionary<Guid, RefinedMaterialSD>), "RefinedMaterials"
                },
                {
                    typeof(Dictionary<Guid, ComponentSD>), "Components"
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
            foreach (var m in Minerals)
            {
                if (m.ID == id)
                    return m;
            }

            if (Techs.ContainsKey(id))
                return Techs[id];

            if (Installations.ContainsKey(id))
                return Installations[id];

            if (RefinedMaterials.ContainsKey(id))
                return RefinedMaterials[id];

            if (Components.ContainsKey(id))
                return Components[id];

            return null;
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
                AtmosphericGases.AddRange(atmosphericGases);
        }

        /// <summary>
        /// Stores Commander Name Themes.
        /// </summary>
        internal void Store(List<CommanderNameThemeSD> commanderNameThemes)
        {
            if (commanderNameThemes != null)
                CommanderNameThemes.AddRange(commanderNameThemes);
        }

        /// <summary>
        /// Stores Mineral Static Data. Will overwrite an existing mineral if the IDs match.
        /// </summary>
        internal void Store(List<MineralSD> minerals)
        {
            if (minerals != null)
            {
                foreach (MineralSD min in minerals)
                {
                    int i = Minerals.FindIndex(x => x.ID == min.ID);
                    if (i >= 0) // found existing element!
                        Minerals[i] = min;
                    else
                        Minerals.Add(min);
                }
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
        /// Stores Installation Static Data. Will overwrite any existing Installations with the same ID.
        /// </summary>
        internal void Store(Dictionary<Guid, InstallationSD> installations)
        {
            if (installations != null)
            {
                foreach (KeyValuePair<Guid, InstallationSD> facility in installations)
                    Installations[facility.Key] = facility.Value;
            }
        }

        /// <summary>
        /// Stores ConstructableObj Static Data. Will overwrite any existing ConstructableObjs with the same ID.
        /// </summary>
        internal void Store(Dictionary<Guid, RefinedMaterialSD> recipes)
        {
            if (recipes != null)
            {
                foreach (KeyValuePair<Guid, RefinedMaterialSD> recipe in recipes)
                    RefinedMaterials[recipe.Key] = recipe.Value;
            }
        }

        /// <summary>
        /// Stores Component Static Data. Will overwrite any existing Component with the same ID.
        /// </summary>
        internal void Store(Dictionary<Guid, ComponentSD> components)
        {
            if (components != null)
            {
                foreach (KeyValuePair<Guid, ComponentSD> component in components)
                    Components[component.Key] = component.Value;
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
                StaticDataManager.LoadData(dataSet, SerializationManager.CurrentGame);
            }
        }

        internal StaticDataStore Clone()
        {
            StaticDataStore clone = new StaticDataStore
            {
                AtmosphericGases = new WeightedList<AtmosphericGasSD>(AtmosphericGases),
                CommanderNameThemes = new List<CommanderNameThemeSD>(CommanderNameThemes),
                Components = new Dictionary<Guid, ComponentSD>(Components),
                Installations = new Dictionary<Guid, InstallationSD>(Installations),
                _loadedDataSets = new List<DataVersionInfo>(LoadedDataSets),
                Minerals = new List<MineralSD>(Minerals),
                RefinedMaterials = new Dictionary<Guid, RefinedMaterialSD>(RefinedMaterials),
                SystemGenSettings = SystemGenSettings, // Todo: Make this cloneable
                Techs = new Dictionary<Guid, TechSD>(Techs)
            };

            return clone;
        }
        #endregion

    }

   
}