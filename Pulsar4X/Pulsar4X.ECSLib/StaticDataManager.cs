using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// This class manages the games static data. This includes import/export of static data 
    /// for an existing game as well as the initial import of the static data for a new game.
    /// </summary>
    public class StaticDataManager
    {
        /// <summary>
        /// Default data directory, static data is stored in subfolders.
        /// </summary>
        private const string DataDirectory = "Data";

        // Serializer, specifically configured for static data.
        private static readonly JsonSerializer Serializer = new JsonSerializer
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented,
            ContractResolver = new ForceUseISerializable(),
            Converters = { new Newtonsoft.Json.Converters.StringEnumConverter() }
        };

        /// <summary>
        /// Returns a list of DataVersionInfo objects representing the datasets that the StaticDataManager
        /// could find and are available for loading.
        /// </summary>
        [PublicAPI]
        public static List<DataVersionInfo> AvailableData()
        {
            string dataDirectory = Path.Combine(SerializationManager.GetWorkingDirectory(), DataDirectory);
            var availableDirs = new List<string>(Directory.GetDirectories(dataDirectory));
            var availableData = new List<DataVersionInfo>();

            foreach (string possibleMod in availableDirs)
            {
                DataVersionInfo dvi;
                if (CheckDataDirectoryVersion(possibleMod, new StaticDataStore(), out dvi))
                {
                    if (availableData.Contains(dvi))
                    {
                        throw new AmbiguousMatchException($"Found two mods in different directories with the same version info: {dvi.FullVersion}");
                    }
                    availableData.Add(dvi);
                }
            }

            return availableData;
        }

        /// <summary>
        /// Loads the data from a specified data subdirectory into the provided game
        /// </summary>
        [PublicAPI]
        public static void LoadData(string dataDir, Game game)
        {
            StaticDataStore newStore = game.StaticData.Clone();
            try
            {
                string dataDirectory = Path.Combine(Path.Combine(SerializationManager.GetWorkingDirectory(), DataDirectory), dataDir);

                // we start by looking for a version file, no version file, no load.
                DataVersionInfo dataVInfo;
                if (CheckDataDirectoryVersion(dataDirectory, game.StaticData, out dataVInfo) == false)
                {
                    throw new StaticDataLoadException("Static Data is explicitly incompatible with currently loaded data.");
                }

                // now we can move on to looking for json files:
                string[] files = Directory.GetFiles(dataDirectory, "*.json");

                if (files.GetLength(0) < 1)
                    return;

                foreach (string file in files)
                {
                    JObject obj = Load(file);
                    StoreObject(obj, newStore);
                }

                if (!newStore.LoadedDataSets.Contains(dataVInfo))
                {
                    newStore.LoadedDataSets.Add(dataVInfo);
                }

                game.StaticData = newStore;
            }
            catch (Exception e)
            {
                if (e.GetType() == typeof(JsonSerializationException) || e.GetType() == typeof(JsonReaderException))
                    throw new StaticDataLoadException("Bad Json provided in directory: " + dataDir, e);
                

                throw;  // rethrow exception if not known ;)
            }
        }

        /// <summary>
        /// Checks for a valid vinfo file in the specified directory, if the file is found it loads it and 
        /// checks that it is compatible with previously loaded data and the library.
        /// </summary>
        /// <param name="directory">Directory to check.</param>
        /// <param name="staticDataStore">staticDataStore to check this dataVersionInfo against loaded dataVersionInfo for incompatibilities</param>
        /// <param name="dataVInfo">Static data version to check against.</param>
        /// <returns>true if a compatible vinfo file was found, false otherwise.</returns>
        private static bool CheckDataDirectoryVersion(string directory, StaticDataStore staticDataStore, out DataVersionInfo dataVInfo)
        {
            dataVInfo = null;

            string[] vInfoFile = Directory.GetFiles(directory, "*.vinfo");

            if (vInfoFile.GetLength(0) < 1 || string.IsNullOrEmpty(vInfoFile[0]))
                return false;

            dataVInfo = LoadVinfo(vInfoFile[0]);

            if (!dataVInfo.IsCompatibleWithLibrary())
            {
                return false;
            }
            foreach (DataVersionInfo dataVersionInfo in staticDataStore.LoadedDataSets)
            {
                if (!dataVersionInfo.IsCompatibleWithDataset(dataVInfo))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Stores the data in the provided JObject if it is valid.
        /// </summary>
        private static void StoreObject(JObject obj, StaticDataStore staticDataStore)
        {
            // we need to work out the type:
            Type type = StaticDataStore.GetType(obj["Type"].ToString());

            // grab the data:
            // use dynamic here to avoid having to know/use the exact the types.
            // we are alreading checking the types via StaticDataStore.*Type, so we 
            // can rely on there being an overload of StaticDataStore.Store
            // that supports that type.
            dynamic data = obj["Data"].ToObject(type, Serializer);

            staticDataStore.Store(data);
        }

        /// <summary>
        /// Loads the specified file into a JObject for further processing.
        /// </summary>
        private static JObject Load(string file)
        {
            JObject obj;
            using (var sr = new StreamReader(file))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                obj = (JObject)Serializer.Deserialize(reader);
            }

            return obj;
        }

        /// <summary>
        /// Loads the specified object into a VersionInfo struct.
        /// </summary>
        private static DataVersionInfo LoadVinfo(string file)
        {
            JObject obj = Load(file); // load into json data.
            var info = (DataVersionInfo)obj["Data"].ToObject(typeof(DataVersionInfo), Serializer);

            return info;
        }

        /// <summary>
        /// Exports the provided static data into the specified file.
        /// </summary>
        public static void ExportStaticData(object staticData, string file)
        {
            var data = new DataExportContainer {Data = staticData, Type = StaticDataStore.GetTypeString(staticData.GetType())};

            string workingDir = Path.Combine(SerializationManager.GetWorkingDirectory(), DataDirectory);

            file = Path.Combine(workingDir, file);

            if (string.IsNullOrEmpty(data.Type) == false)
            {
                using (var sw = new StreamWriter(file))
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    Serializer.Serialize(writer, data);
                }
            }
        }
    }

    #region Helper classes

    /// <summary>
    /// Exception which is thown when an error occurs during loading of atatic data.
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

        // This constructor is needed for serialization.
        public StaticDataLoadException(SerializationInfo info, StreamingContext context)
            : base(info, context)
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
