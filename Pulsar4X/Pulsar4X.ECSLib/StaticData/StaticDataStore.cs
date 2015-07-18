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
    [JsonObject(MemberSerialization.OptIn)]
    public class StaticDataStore
    {
        /// <summary>
        /// Default data directory, static data is stored in subfolders.
        /// Todo: make this load from some sort of settings file.
        /// Todo: Make sure this can be multiplatform.
        /// </summary>
        [PublicAPI]
        [NotNull]
        public static string DataDirectory { get { return "Data"; } }

        /// <summary>
        /// The subdirectory of DataDirectory that contains the official game data.
        /// </summary>
        [PublicAPI]
        [NotNull]
        public static string DefaultDataSet { get { return "Pulsar4X"; } }

        // Serializer, specifically configured for static data.
        private static readonly JsonSerializer Serializer = new JsonSerializer
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented,
            ContractResolver = new ForceUseISerializable(),
            Converters = { new Newtonsoft.Json.Converters.StringEnumConverter() }
        };

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
        public readonly WeightedList<AtmosphericGasSD> AtmosphericGases = new WeightedList<AtmosphericGasSD>();

        /// <summary>
        /// List which stores all the Commander Name themes.
        /// </summary>
        public readonly List<CommanderNameThemeSD> CommanderNameThemes = new List<CommanderNameThemeSD>();

        /// <summary>
        /// List which stores all the Minerals.
        /// </summary>
        public readonly List<MineralSD> Minerals = new List<MineralSD>();

        /// <summary>
        /// Dictionary which stores all the Technologies.
        /// stored in a dictionary to allow fast lookup of a specific Technology based on its guid.
        /// </summary>
        public readonly JDictionary<Guid, TechSD> Techs = new JDictionary<Guid, TechSD>();

        /// <summary>
        /// List which stores all of the installations
        /// </summary>
        public readonly JDictionary<Guid, InstallationSD> Installations = new JDictionary<Guid, InstallationSD>();

        /// <summary>
        /// Dictionary which stores all the Recipes.
        /// </summary>
        public readonly JDictionary<Guid, ConstructableObjSD> ConstructableObjects = new JDictionary<Guid, ConstructableObjSD>();

        /// <summary>
        /// Dictionary which stores all Components.
        /// </summary>
        public readonly JDictionary<Guid, ComponentSD> Components = new JDictionary<Guid, ComponentSD>();

        [PublicAPI]
        public List<string> LoadedDataSets { get { return _loadedDataSets; } }
        [JsonProperty]
        private readonly List<string> _loadedDataSets;

        public StaticDataStore()
        {
            _loadedDataSets = new List<string>();
        }

        #region Static field initializers

        /// <summary>
        /// Initializes the StringsToTypes static dictionary.
        /// </summary>
        private static Dictionary<string, Type> InitializeStringsToTypes()
        {
            return new Dictionary<string, Type> {{"AtmosphericGases", typeof(WeightedList<AtmosphericGasSD>)}, {"CommanderNameThemes", typeof(List<CommanderNameThemeSD>)}, {"Minerals", typeof(List<MineralSD>)}, {"Techs", typeof(JDictionary<Guid, TechSD>)}, {"Installations", typeof(JDictionary<Guid, InstallationSD>)}, {"ConstructableObj", typeof(JDictionary<Guid, ConstructableObjSD>)}, {"Components", typeof(JDictionary<Guid, ComponentSD>)}};
        }

        /// <summary>
        /// Initializes the TypesToStrings static dictionary.
        /// </summary>
        /// <returns></returns>
        private static Dictionary<Type, string> InitializeTypesToStrings()
        {
            return StringsToTypes.ToDictionary(x => x.Value, x => x.Key);
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

            if (ConstructableObjects.ContainsKey(id))
                return ConstructableObjects[id];

            if (Components.ContainsKey(id))
                return Components[id];

            return null;
        }

        /// <summary>
        /// Loads a StaticData DataSet into this StaticDataStore, overwriting any previous values.
        /// </summary>
        /// <param name="dataSet">Directory to the DataSet to load.</param>
        /// <returns></returns>
        /// <exception cref="StaticDataLoadException">Thrown in a variety of situations when StaticData could not be loaded.</exception>
        [PublicAPI]
        public void LoadDataSet([NotNull] string dataSet)
        {
            if (!Directory.Exists(DataDirectory))
            {
                throw new StaticDataLoadException(string.Format("Data directory {0} not found. Reinstalling may fix this issue.", DataDirectory));
            }

            // Verify the specified DataSet exists.
            string dataSetDirectory = string.Format("{0}{1}{2}", DataDirectory, Path.DirectorySeparatorChar, dataSet);

            // Attempt to load the DataSet.
            try
            {
                // we start by looking for a version file, no version file, no load.
                if (CheckDataDirectoryVersion(dataSetDirectory, VersionInfo.PulsarVersionInfo) == false)
                    throw new StaticDataLoadException(string.Format("DataSet {0} is incompatible with this game version.", dataSet));

                // now we can move on to looking for json files:
                string[] files = Directory.GetFiles(dataSetDirectory, "*.json");

                if (files.GetLength(0) < 1)
                    return;

                foreach (var file in files)
                {
                    try
                    {
                        JObject obj = Load(file);
                        StoreObject(obj);
                    }
                    catch (JsonSerializationException e)
                    {
                        throw new StaticDataLoadException(string.Format("Bad Json provided in file: ", file), e);
                    }
                }
            }
// TODO: Review exceptions in this function. I followed the established exception pattern during implementation here, but I think we should throw more specific exceptions to allow the UI to handle them better.
            catch (JsonSerializationException e)
            {
                throw new StaticDataLoadException(string.Format("Bad Json provided in directory: {0}", dataSetDirectory), e);
            }
            catch (FileNotFoundException e)
            {
                throw new StaticDataLoadException(string.Format("Version info file could not be found for DataSet: {0}", dataSet), e);
            }
            catch (DirectoryNotFoundException e)
            {
                throw new StaticDataLoadException(string.Format("DataSet directory {0} not found.", dataSetDirectory), e);
            }
            catch (IOException e)
            {
                throw new StaticDataLoadException(string.Format("IO Exception while accessing DataSet: {0}", dataSet), e);
            }
            catch (UnauthorizedAccessException e)
            {
                throw new StaticDataLoadException(string.Format("Access denied while loading DataSet: {0}", dataSet), e);
            }
            if (!_loadedDataSets.Contains(dataSet))
            {
                _loadedDataSets.Add(dataSet);
            }
        }

        /// <summary>
        /// Exports the provided static data into the specified file.
        /// </summary>
        /// <exception cref="KeyNotFoundException">The provided staticData is not a valid static data type.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="staticData"/> is <see langword="null" />.</exception>
        /// <exception cref="UnauthorizedAccessException">Access is denied.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive). </exception>
        /// <exception cref="IOException"><paramref name="path" /> includes an incorrect or invalid syntax for file name, directory name, or volume label syntax. </exception>
        /// <exception cref="SecurityException">The caller does not have the required permission. </exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must not exceed 248 characters, and file names must not exceed 260 characters. </exception>
        /// <exception cref="ArgumentException"><paramref name="path" /> is an empty string (""). -or-<paramref name="path" /> contains the name of a system device (com1, com2, and so on).</exception>
        [PublicAPI]
        public static void ExportStaticData([NotNull] object staticData, string path)
        {
            if (staticData == null)
            {
                throw new ArgumentNullException("staticData");
            }
            var data = new DataExportContainer { Data = staticData, Type = TypesToStrings[staticData.GetType()] };

            using (StreamWriter sw = new StreamWriter(path))
            {
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    Serializer.Serialize(writer, data);
                }
            }
        }

        #endregion

        #region Private functions

        #region private void Store(dynamic object) overloads.

        /// <summary>
        /// Stores Atmospheric Gas Static Data.
        /// </summary>
        private void Store(WeightedList<AtmosphericGasSD> atmosphericGases)
        {
            if (atmosphericGases != null)
                AtmosphericGases.AddRange(atmosphericGases);
        }

        /// <summary>
        /// Stores Commander Name Themes.
        /// </summary>
        private void Store(List<CommanderNameThemeSD> commanderNameThemes)
        {
            if (commanderNameThemes != null)
                CommanderNameThemes.AddRange(commanderNameThemes);
        }

        /// <summary>
        /// Stores Mineral Static Data. Will overwrite an existing mineral if the IDs match.
        /// </summary>
        private void Store(List<MineralSD> minerals)
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
        private void Store(JDictionary<Guid, TechSD> techs)
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
        private void Store(JDictionary<Guid, InstallationSD> installations)
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
        public void Store(JDictionary<Guid, ConstructableObjSD> recipes)
        {
            if (recipes != null)
            {
                foreach (var recipe in recipes)
                    ConstructableObjects[recipe.Key] = recipe.Value;
            }
        }

        /// <summary>
        /// Stores Component Static Data. Will overwrite any existing Component with the same ID.
        /// </summary>
        private void Store(JDictionary<Guid, ComponentSD> components)
        {
            if (components != null)
            {
                foreach (var component in components)
                    Components[component.Key] = component.Value;
            }
        }

        #endregion

        /// <summary>
        /// Stores the data in the provided JObject if it is valid.
        /// </summary>
        private void StoreObject(JObject obj)
        {
            // we need to work out the type:
            Type type = StringsToTypes[obj["Type"].ToString()];

            // grab the data:
            // use dynamic here to avoid having to know/use the exact the types.
            // we are already checking the types via StaticDataStore.*Type, so we 
            // can rely on there being an overload of StaticDataStore.Store
            // that supports that type.
            dynamic data = obj["Data"].ToObject(type, Serializer);

            Store(data);
        }

        /// <summary>
        /// Loads the specified file into a JObject for further processing.
        /// </summary>
        private static JObject Load(string file)
        {
            JObject obj;
            using (StreamReader sr = new StreamReader(file))
                using (JsonReader reader = new JsonTextReader(sr))
                {
                    obj = (JObject)Serializer.Deserialize(reader);
                }

            return obj;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="FileNotFoundException">When VersionInfo.vinfo not found in this directory.</exception>
        private static bool CheckDataDirectoryVersion(string directory, VersionInfo vInfo)
        {
            string versionInfoFile = string.Format("{0}{1}VersionInfo.vinfo", directory, Path.DirectorySeparatorChar);
            VersionInfo loadedVersionInfo;
            if (!File.Exists(versionInfoFile))
            {
                throw new FileNotFoundException("Version information file not found.", versionInfoFile);
            }

            try
            {
                using (StreamReader sr = new StreamReader(versionInfoFile))
                {
                    using (JsonReader reader = new JsonTextReader(sr))
                    {
                        loadedVersionInfo = (VersionInfo)Serializer.Deserialize(reader, typeof(VersionInfo));
                    }
                }
            }
            catch (JsonSerializationException e)
            {
                throw new StaticDataLoadException(string.Format("{0}VersionInfo.vinfo contained invalid JSON.", directory + Path.DirectorySeparatorChar), e);
            }

            return loadedVersionInfo.IsCompatibleWith(vInfo);
        }

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            foreach (string dataSet in _loadedDataSets)
            {
                LoadDataSet(dataSet);
            }
        }

        #endregion

    }

    #region Helper classes

    /// <summary>
    /// Exception which is thrown when an error occurs during loading of static data.
    /// usually InnerException is set to the original exception which caused the error. 
    /// </summary>
    public class StaticDataLoadException : Exception
    {
        public StaticDataLoadException()
            : base("Unknown error occured during Static Data load.")
        { }

        public StaticDataLoadException(string message)
            : base("Error while loading static data: " + message)
        { }

        public StaticDataLoadException(string message, Exception inner)
            : base("Error while loading static data: " + message, inner)
        { }
    }

    /// <summary>
    /// This is a simple attribute that should be attached to Static Data structs. It assists reflection in finding 
    /// Static data and dealing with it. It has two properties, HasID and IDPropertyName, that are used to 
    /// signal that this piece of static data has a unique guid that represents it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct)]
    public class StaticDataAttribute : Attribute
    {
        public bool HasID { get; set; }

        public string IDPropertyName { get; set; }

        public StaticDataAttribute(bool hasID)
        {
            HasID = hasID;
        }
    }

    /// <summary>
    /// A small helper for exporting static data.
    /// </summary>
    public struct DataExportContainer
    {
        public string Type;
        public dynamic Data;
    }

    #endregion
}