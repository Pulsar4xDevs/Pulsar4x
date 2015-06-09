using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

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
        [JsonIgnore]
        private readonly Dictionary<string, Type> StringsToTypes;

        /// <summary>
        /// Reverse dictionary of the above.
        /// </summary>
        [JsonIgnore]
        private readonly Dictionary<Type, string> TypesToStrings; 

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
        /// stored in a dictionary to allow fast lookup of a specifc Technology based on its guid.
        /// </summary>
        public JDictionary<Guid, TechSD> Techs = new JDictionary<Guid, TechSD>();

        /// <summary>
        /// List which stores all of the installations
        /// </summary>
        public JDictionary<Guid, InstallationSD> Installations = new JDictionary<Guid, InstallationSD>();

        /// <summary>
        /// Dictionary which stores all the Recipes.
        /// </summary>
        public JDictionary<Guid, ConstructableObjSD> ConstructableObjects = new JDictionary<Guid, ConstructableObjSD>();

        public JDictionary<Guid, ComponentSD> Components = new JDictionary<Guid, ComponentSD>(); 

        ///< @todo add a whole bunch more static data.

        /// <summary>
        /// Default Constructor.
        /// </summary>
        public StaticDataStore()
        {
            StringsToTypes = new Dictionary<string, Type>()
            {
                {"AtmosphericGases", AtmosphericGases.GetType()},
                {"CommanderNameThemes", CommanderNameThemes.GetType()},
                {"Minerals", Minerals.GetType()},
                {"Techs", Techs.GetType()},
                {"Installations", Installations.GetType()},
                {"ConstrutableObj", ConstructableObjects.GetType()},
                {"Components", Components.GetType()}
            };
            TypesToStrings = StringsToTypes.ToDictionary(x => x.Value, x => x.Key);
        }

        /// <summary>
        /// Stores Atmospheric Gas Static Data.
        /// </summary>
        public void Store(WeightedList<AtmosphericGasSD> atmosphericGases)
        {
            if (atmosphericGases != null)
                AtmosphericGases.AddRange(atmosphericGases);
        }

        /// <summary>
        /// Stores Commander Name Themes.
        /// </summary>
        public void Store(List<CommanderNameThemeSD> commanderNameThemes)
        {
            if (commanderNameThemes != null)
                CommanderNameThemes.AddRange(commanderNameThemes);
        }

        /// <summary>
        /// Stores Mineral Static Data. Will overwrite an existing mineral if the IDs match.
        /// </summary>
        public void Store(List<MineralSD> minerals)
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
        public void Store(JDictionary<Guid, TechSD> techs)
        {
            if (techs != null)
            {
                foreach (var tech in techs)
                    Techs[tech.Key] = tech.Value;  // replace existing value or insert a new one as required.
            }
        }

        /// <summary>
        /// Stores Installation Static Data. Will overwrite any existing Installations with the same ID.
        /// </summary>
        public void Store(JDictionary<Guid, InstallationSD> installations)
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
        public void Store(JDictionary<Guid, ConstructableObjSD> recipies)
        {
            if (recipies != null)
            {
                foreach (var recipe in recipies)
                    ConstructableObjects[recipe.Key] = recipe.Value;
            }
        }

        /// <summary>
        /// Stores Component Static Data. Will overwrite any existing Component with the same ID.
        /// </summary>
        public void Store(JDictionary<Guid, ComponentSD> components)
        {
            if (components != null)
            {
                foreach (var component in components)
                    Components[component.Key] = component.Value;
            }
        }

        /// <summary>
        /// Returns a type custom string for a type of static data. This string is used to tell 
        /// what type of static data is being imported (and is thus exported as well). 
        /// </summary>
        public string GetTypeString(Type type)
        {
            string s;
            TypesToStrings.TryGetValue(type, out s);
            return s;
        }

        /// <summary>
        /// Gets the matching type for a type string. Used when importing previously exported 
        /// static data to know what type to import it as.
        /// </summary>
        public Type GetType(string typeString)
        {
            return StringsToTypes[typeString];
        }

        /// <summary>
        /// This functin goes through each Static Data type in the store looking for one that has an ID that
        /// matches the one provided.
        /// Returns null if the id is not found.
        /// </summary>
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

            if (ConstructableObjects.ContainsKey(id))
                return ConstructableObjects[id];

            if (Components.ContainsKey(id))
                return Components[id];

            return null;
        }
    }
}