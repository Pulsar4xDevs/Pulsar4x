using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Pulsar4X.ECSLib;

namespace ModdingTools.JsonDataEditor
{
    static class Data
    {
        private static bool _loading;

        //stolen from StaticDataManager
        private static JsonSerializer serializer = new JsonSerializer
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented,
            ContractResolver = new ForceUseISerializable(),
            Converters = { new Newtonsoft.Json.Converters.StringEnumConverter() }
        };

        #region Technologies

        public static event Action TechListChanged;
        public static event Action TechLoadedFilesListChanged;

        private static JDictionary<Guid, TechSD> _allTechs = new JDictionary<Guid, TechSD>();
        private static Dictionary<Guid, TechDataHolder> _allTechDataHolders = new Dictionary<Guid, TechDataHolder>();

        //New tech instances would be written in this file
        private static string _selectedTechFile = ""; 
        private static List<string> _loadedTechFiles = new List<string>();

        public static TechDataHolder GetTechDataHolder(Guid guid)
        {
            TechDataHolder dataHolder;
            if(_allTechDataHolders.TryGetValue(guid, out dataHolder))
                return dataHolder;
            throw new Exception("Guid not found");
        }

        public static IEnumerable<TechDataHolder> GetTechDataHolders()
        {
            return _allTechDataHolders.Values.AsEnumerable();
        }

        public static TechSD GetTech(Guid guid)
        {
            TechSD techSD;
            if(_allTechs.TryGetValue(guid, out techSD))
                return techSD;
            throw new Exception("Guid not found");
        }

        public static void UpdateTech(TechSD tech)
        {
            if (!_allTechDataHolders.ContainsKey(tech.Id))
            {
                CreateTech(tech);
            }
            else
            {
                _allTechs[tech.Id] = tech;
                _allTechDataHolders[tech.Id].Name = tech.Name;
            }

            if(!_loading && TechListChanged != null)
                TechListChanged.Invoke();
        }

        private static void CreateTech(TechSD tech)
        {
            _allTechs[tech.Id] = tech;
            _allTechDataHolders[tech.Id] = new TechDataHolder(tech.Name, _selectedTechFile, tech.Id);
        }

        public static void RemoveTech(Guid guid)
        {
            _allTechs.Remove(guid);
            _allTechDataHolders.Remove(guid);

            if (!_loading && TechListChanged != null)
                TechListChanged.Invoke();
        }

        public static List<string> GetLoadedFiles()
        {
            return _loadedTechFiles.ToList();
        }

        public static void SetSelectedTechFile(string path)
        {
            if(!_loadedTechFiles.Contains(path))
                return;
            _selectedTechFile = path;

            if (TechLoadedFilesListChanged != null)
                TechLoadedFilesListChanged.Invoke();
        }
        #endregion

        #region Save/Load stuff

        public static void Clear()
        {
            _allTechs = new JDictionary<Guid, TechSD>();
            _allTechDataHolders = new Dictionary<Guid, TechDataHolder>();
            
        }

        public static void LoadFile(string filePath)
        {
            _loading = true;

            //Read file and get all data from it
            JObject obj;
            using (StreamReader streamReader = new StreamReader(filePath))
            using (JsonReader reader = new JsonTextReader(streamReader))
            {
                obj = (JObject)serializer.Deserialize(reader);
            }

            //Check which type of json data file is it
            Type type = StaticDataManager.StaticDataStore.GetType(obj["Type"].ToString());

            //Load data using dynamic
            dynamic data = obj["Data"].ToObject(type, serializer);

            if (type == StaticDataManager.StaticDataStore.TechsType)
            {
                LoadData(data, filePath);
            }

            _loading = false;
            
            //Send message to all subscribers
            if(TechListChanged != null)
                TechListChanged.Invoke();

            _loadedTechFiles.Add(filePath);
            if (String.IsNullOrWhiteSpace(_selectedTechFile))
                _selectedTechFile = filePath;

            if (TechLoadedFilesListChanged != null)
                TechLoadedFilesListChanged.Invoke();
            
        }

        private static void LoadData(JDictionary<Guid, TechSD> dict, string filePath)
        {
            foreach (TechSD techSD in dict.Values)
            {
                _allTechs[techSD.Id] = techSD;
                _allTechDataHolders[techSD.Id] = new TechDataHolder(techSD.Name, filePath, techSD.Id);
            }
        }

        public static bool SaveData()
        {
            //Save tech data
            Dictionary<string, JDictionary<Guid, TechSD>> sortedData = new Dictionary<string, JDictionary<Guid, TechSD>>();
            foreach(TechDataHolder techDataHolder in _allTechDataHolders.Values)
            {
                if (!sortedData.ContainsKey(techDataHolder.File))
                    sortedData[techDataHolder.File] = new JDictionary<Guid, TechSD>();
                sortedData[techDataHolder.File][techDataHolder.Guid] = _allTechs[techDataHolder.Guid];
            }

            foreach (string path in sortedData.Keys)
            {
                DataExportContainer exportContainer = new DataExportContainer { Type = "Techs", Data = sortedData[path] };

                using (StreamWriter streamWriter = new StreamWriter(path))
                using (JsonWriter writer = new JsonTextWriter(streamWriter))
                {
                    serializer.Serialize(writer, exportContainer);
                }
            }

            return true;
        }

        #endregion
    }
    
    public class TechDataHolder
    {
        public string Name { get; set; }
        public string File { get; private set; }
        public Guid Guid { get; private set; }

        public TechDataHolder(string name, string file, Guid guid)
        {
            Name = name;
            File = file;
            Guid = guid;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
