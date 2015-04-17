using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class StaticDataManager
    {
        public static StaticDataStore StaticDataStore = new StaticDataStore();

        private const string defaultDataDirectory = "./Data";

        /// <summary>
        /// the subdirectory of defaultDataDirectory that contains the offical game data.
        /// We will want to laod this first so that mods overwrite our game files.
        /// </summary>
        private const string officialDataDirectory = "/Pulsar4x";

        private static JsonSerializer serializer = new JsonSerializer
        {
            NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented, Converters = { new Newtonsoft.Json.Converters.StringEnumConverter() }
        };

        /// <summary>
        /// Loads all dtat from the default static data directory.
        /// Used when initilising a new game.
        /// </summary>
        public static void LoadFromDataDirectory()
        {
            // get list of default sub-directories:
            var dataDirs = Directory.GetDirectories(defaultDataDirectory);

            // safty check:
            if (dataDirs == null || dataDirs.GetLength(0) < 1)
            {
                return;
            }

            // Loop through dirs, looking for files to load:
            foreach (var dir in dataDirs)
            {
                // we start by looking for a version file, no verion file, no load.
                var vInfoFile = Directory.GetFiles(dir, "*.vinfo");

                if (vInfoFile == null || vInfoFile.GetLength(0) < 1)
                    continue;

                VersionInfo vinfo = (VersionInfo)Load(vInfoFile[0]);

                if (vinfo.IsCompatibleWith(VersionInfo.PulsarVersionInfo) == false)
                    continue; ///< @todo log failure to import due to incompatible version.

                // now we can move on to looking for json files:
                var files = Directory.GetFiles(dir, "*.json");
                         
                if (files == null || files.GetLength(0) < 1)
                    continue;

                foreach (var file in files)
                {
                    // use dynamic here to avoid having to know/use the exact the types.
                    // we are alreading checking the types via StaticDataStore.*Type, so we 
                    // can rely on there being an overload of StaticDataStore.Store
                    // that supports that type.
                    dynamic obj = Load(file);
                    Type type = obj.GetType();

                    if (type == StaticDataStore.AtmosphericGasesType)
                    {
                        StaticDataStore.Store(obj);
                    }
                    else if (type == StaticDataStore.CommanderNameThemesType)
                    {
                        StaticDataStore.Store(obj);                        
                    }
                    else if (type == StaticDataStore.MineralsType)
                    {
                        StaticDataStore.Store(obj);
                    }  
                    // ... more here.
                }
            }
        }


        static object Load(string file)
        {
            object obj = null;
            using (StreamReader sr = new StreamReader(file))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                obj = serializer.Deserialize(reader);
            }

            return obj;
        }


        public static void ImportStaticData(string file)
        {
            throw new NotImplementedException();
        }

        public static void ExportStaticData(object staticData, string file)
        {
            using (StreamWriter sw = new StreamWriter(file))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, staticData);
            }
        }
    }
}
