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
        public WeightedList<AtmosphericGasSD> AtmosphericGases = new WeightedList<AtmosphericGasSD>();

        /// <summary>
        /// List which stores all the Commander Name themes.
        /// </summary>
        public List<CommanderNameThemeSD> CommanderNameThemes = new List<CommanderNameThemeSD>();

        /// <summary>
        /// List which stores all the Minerals.
        /// </summary>
        public List<MineralSD> Minerals = new List<MineralSD>();

        /// <summary>
        /// Dictionary which stores all the Technologies.
        /// stored in a dictionary to allow fast lookup of a specific Technology based on its guid.
        /// </summary>
        public JDictionary<Guid, TechSD> Techs = new JDictionary<Guid, TechSD>();

        /// <summary>
        /// List which stores all of the installations
        /// </summary>
        public JDictionary<Guid, InstallationSD> Installations = new JDictionary<Guid, InstallationSD>();

        /// <summary>
        /// Dictionary which stores all the Recipes.
        /// </summary>
        public JDictionary<Guid, RefinedMaterialSD> RefinedMaterials = new JDictionary<Guid, RefinedMaterialSD>();

        /// <summary>
        /// Dictionary which stores all Components.
        /// </summary>
        public JDictionary<Guid, ComponentSD> Components = new JDictionary<Guid, ComponentSD>();

        /// <summary>
        /// Settings used by system generation. 
        /// @todo make Galaxy gen use this instead of default data (DO NOT DELETE THE HARD CODED DATA THO, that should be a fall back).
        /// </summary>
        public SystemGenSettingsSD SystemGenSettings;

        /// <summary>
        /// This list holds the version info of all the loaded data sets.
        /// </summary>
        [PublicAPI]
        public List<VersionInfo> LoadedDataSets { get { return _loadedDataSets; } }
        [JsonProperty]
        private readonly List<VersionInfo> _loadedDataSets;

        public StaticDataStore()
        {
            _loadedDataSets = new List<VersionInfo>();

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
                    "Techs", typeof(JDictionary<Guid, TechSD>)
                },
                {
                    "Installations", typeof(JDictionary<Guid, InstallationSD>)
                },
                {
                    "RefinedMaterials", typeof(JDictionary<Guid, RefinedMaterialSD>)
                },
                {
                    "Components", typeof(JDictionary<Guid, ComponentSD>)
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
                    typeof(JDictionary<Guid, TechSD>), "Techs"
                },
                {
                    typeof(JDictionary<Guid, InstallationSD>), "Installations"
                },
                {
                    typeof(JDictionary<Guid, RefinedMaterialSD>), "RefinedMaterials"
                },
                {
                    typeof(JDictionary<Guid, ComponentSD>), "Components"
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
                foreach (var min in minerals)
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
        internal void Store(JDictionary<Guid, TechSD> techs)
        {
            if (techs != null)
            {
                foreach (var tech in techs)
                    Techs[tech.Key] = tech.Value; // replace existing value or insert a new one as required.
            }
        }

        /// <summary>
        /// Stores Installation Static Data. Will overwrite any existing Installations with the same ID.
        /// </summary>
        internal void Store(JDictionary<Guid, InstallationSD> installations)
        {
            if (installations != null)
            {
                foreach (var facility in installations)
                    Installations[facility.Key] = facility.Value;
            }
        }

        /// <summary>
        /// Stores ConstructableObj Static Data. Will overwrite any existing ConstructableObjs with the same ID.
        /// </summary>
        internal void Store(JDictionary<Guid, RefinedMaterialSD> recipes)
        {
            if (recipes != null)
            {
                foreach (var recipe in recipes)
                    RefinedMaterials[recipe.Key] = recipe.Value;
            }
        }

        /// <summary>
        /// Stores Component Static Data. Will overwrite any existing Component with the same ID.
        /// </summary>
        internal void Store(JDictionary<Guid, ComponentSD> components)
        {
            if (components != null)
            {
                foreach (var component in components)
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
            //foreach (string dataSet in _loadedDataSets)
            //{
            //    LoadDataSet(dataSet);
            //}
        }

        #endregion

    }

   
}