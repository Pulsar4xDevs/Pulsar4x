using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// A small helper for exporting static data.
    /// </summary>
    public struct DataExportContainer
    {
        public string Type;
        public dynamic Data;
    }

    /// <summary>
    /// This class manages the games static data. This includes import/export of static data 
    /// for an existing game as well as the initial import of the static data for a new game.
    /// </summary>
    public class StaticDataManager
    {
        /// <summary>
        /// The static data store.
        /// </summary>
        public static StaticDataStore StaticDataStore = new StaticDataStore();

        /// <summary>
        /// Default data directory, static data is stored in subfolders.
        /// @todo make this load from some sort of settings file.
        /// </summary>
        private const string DefaultDataDirectory = "./Data";

        /// <summary>
        /// the subdirectory of defaultDataDirectory that contains the offical game data.
        /// We will want to laod this first so that mods overwrite our game files.
        /// @todo make this load from some sort of settings file.
        /// @todo make this more cross platform (currently windows only).
        /// </summary>
        private const string OfficialDataDirectory = "\\Pulsar4x";

        // Serilizer, specifically configured for static data.
        private static JsonSerializer _serializer = new JsonSerializer
        {
            NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented, ContractResolver = new ForceUseISerializable(), Converters = { new Newtonsoft.Json.Converters.StringEnumConverter() }
        };

        /// <summary>
        /// Small function that clears any static data currently loaded. 
        /// It should be used before loading or starting a new game to make 
        /// sure the static data is starting from a blank slate.
        /// </summary>
        public static void ClearAllData()
        {
            StaticDataStore = new StaticDataStore();
        }

        /// <summary>
        /// Loads all dtat from the default static data directory.
        /// Used when initilising a new game.
        /// </summary>
        public static void LoadFromDefaultDataDirectory()
        {
            // get list of default sub-directories:
            var dataDirs = Directory.GetDirectories(DefaultDataDirectory);

            // safty check:
            if (dataDirs.GetLength(0) < 1)
            {
                return;
            }

            // lets make sure we load the official data first:
            int i = Array.FindIndex(dataDirs, x => x == (DefaultDataDirectory + OfficialDataDirectory));
            if (i < 0 || String.IsNullOrEmpty(dataDirs[i]))
                return; // bad.

            LoadFromDirectory(dataDirs[i]);
            dataDirs[i] = null; // prevent us from loading it again.
            
            // Loop through dirs, looking for files to load:
            foreach (var dir in dataDirs)
            {
                if (dir != null)
                    LoadFromDirectory(dir);
            }
        }

        /// <summary>
        /// Loads all data from the specified directory.
        /// </summary>
        public static void LoadFromDirectory(string directory)
        {
            try
            {
                // we start by looking for a version file, no verion file, no load.
                if (CheckDataDirectoryVersion(directory, VersionInfo.PulsarVersionInfo) == false)
                    return; ///< @todo log failure to import due to incompatible version.

                // now we can move on to looking for json files:
                var files = Directory.GetFiles(directory, "*.json");

                if (files.GetLength(0) < 1)
                    return;

                foreach (var file in files)
                {
                    JObject obj = Load(file);
                    StoreObject(obj);
                }
            }
            catch (Exception e)
            {
                if (e.GetType() == typeof(Newtonsoft.Json.JsonSerializationException))
                    throw new StaticDataLoadException("Bad Json provided in directory: " + directory, e);

                if (e.GetType() == typeof(DirectoryNotFoundException))
                    throw new StaticDataLoadException("Directory not found: " + directory, e);
            }
        }

        /// <summary>
        /// Checks for a valid vinfo file in the specified directory, if the file is found it loads it and 
        /// chacks that is is a compatible with version info provided.
        /// </summary>
        /// <param name="directory">Directory to check.</param>
        /// <param name="vinfo">Version to check against.</param>
        /// <returns>true if a compatible vinfo file was found, false otherwise.</returns>
        private static bool CheckDataDirectoryVersion(string directory, VersionInfo vinfo)
        {
            var vInfoFile = Directory.GetFiles(directory, "*.vinfo");

            if (vInfoFile.GetLength(0) < 1 || String.IsNullOrEmpty(vInfoFile[0]))
                return false;

            VersionInfo loadedVinfo = LoadVinfo(vInfoFile[0]);

            return loadedVinfo.IsCompatibleWith(vinfo);
        }

        /// <summary>
        /// Stores the data in the provided JObject if it is valid.
        /// </summary>
        private static void StoreObject(JObject obj)
        {
            // we need to work out the type:
            Type type = StaticDataStore.GetType(obj["Type"].ToString());

            // grab the data:
            // use dynamic here to avoid having to know/use the exact the types.
            // we are alreading checking the types via StaticDataStore.*Type, so we 
            // can rely on there being an overload of StaticDataStore.Store
            // that supports that type.
            dynamic data = obj["Data"].ToObject(type, _serializer);

            if (type == StaticDataStore.AtmosphericGasesType)
            {
                StaticDataStore.Store(data);
            }
            else if (type == StaticDataStore.CommanderNameThemesType)
            {
                StaticDataStore.Store(data);
            }
            else if (type == StaticDataStore.MineralsType)
            {
                StaticDataStore.Store(data);
            }
            else if (type == StaticDataStore.TechsType)
            {
                StaticDataStore.Store(data);
            }
            else if (type == StaticDataStore.InstallationsType)
            {
                StaticDataStore.Store(data);
            }

            // ... more here.
        }

        /// <summary>
        /// Loads the specified file into a JObject for further processing.
        /// </summary>
        static JObject Load(string file)
        {
            JObject obj = null;
            using (StreamReader sr = new StreamReader(file))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                obj = (JObject)_serializer.Deserialize(reader);
            }

            return obj;
        }

        /// <summary>
        /// Loads the specified object into a VersionInfo struct.
        /// </summary>
        static VersionInfo LoadVinfo(string file)
        {
            VersionInfo info;
            using (StreamReader sr = new StreamReader(file))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                info = (VersionInfo)_serializer.Deserialize(reader, typeof(VersionInfo));
            }

            return info;
        }


        public static void ImportStaticData(string file)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Exports the provided static data into the specified file.
        /// </summary>
        public static void ExportStaticData(object staticData, string file)
        {
            var data = new DataExportContainer();
            data.Data = staticData;
            data.Type = StaticDataStore.GetTypeString(staticData.GetType());

            if (String.IsNullOrEmpty(data.Type) == false)
            {
                using (StreamWriter sw = new StreamWriter(file))
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    _serializer.Serialize(writer, data);
                }
            }
        }
    }

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
}
