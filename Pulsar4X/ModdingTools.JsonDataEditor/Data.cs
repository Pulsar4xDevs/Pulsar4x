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
    public class Data
    {
        private static bool _loading;
        public static DataHolderAndEvents<TechSD> TechData = new DataHolderAndEvents<TechSD>("Techs");
        public static DataHolderAndEvents<InstallationSD> InstallationData = new DataHolderAndEvents<InstallationSD>("Installations");
        public static DataHolderAndEvents<MineralSD> MineralData = new DataHolderAndEvents<MineralSD>("Minerals"); 
        //stolen from StaticDataManager
        private static JsonSerializer serializer = new JsonSerializer
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented,
            ContractResolver = new ForceUseISerializable(),
            Converters = { new Newtonsoft.Json.Converters.StringEnumConverter() }
        };

        public static MainWindow MainWindow; // T_T

        #region Save/Load stuff

        //clear all data holders and events objects
        public static void Clear()
        {
            TechData.Clear();
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

            LoadData(data, filePath);

            _loading = false;
            
            //Send message to all subscribers
            TechData.OnListChangeEvent(filePath);
            InstallationData.OnListChangeEvent(filePath);
            MineralData.OnListChangeEvent(filePath);
        }

        private static void LoadData(JDictionary<Guid, TechSD> dict, string filePath)
        {
            TechData.Load(dict, filePath);
        }
        private static void LoadData(JDictionary<Guid, InstallationSD> dict, string filePath)
        {
            InstallationData.Load(dict, filePath);
        }
        private static void LoadData(List<MineralSD> list, string filePath)
        {
            MineralData.Load(list, filePath);
            
        }

        public static bool SaveData()
        {
            bool success = TechData.Save() && InstallationData.Save() && MineralData.Save();
            return success;          
        }

        #endregion

        public class DataHolderAndEvents<T>
        {
            public event Action ListChanged;
            public event Action LoadedFilesListChanged;

            private string _typeString;

            private JDictionary<Guid, T> _allSDs = new JDictionary<Guid, T>();
            private Dictionary<Guid, DataHolder> _allDataHolders = new Dictionary<Guid, DataHolder>();

            //New tech instances would be written in this file
            private string _selectedFile = "";
            private List<string> _loadedFiles = new List<string>();

            public DataHolderAndEvents(string type)
            {
                _typeString = type;
            } 

            /// <summary>
            /// Gets a dataholder from a given guid
            /// </summary>
            /// <param name="guid"></param>
            /// <returns></returns>
            public DataHolder GetDataHolder(Guid guid)
            {
                DataHolder dataHolder;
                if (_allDataHolders.TryGetValue(guid, out dataHolder))
                    return dataHolder;
                throw new Exception("Guid not found");
            }

            /// <summary>
            /// returns all dataholders
            /// </summary>
            /// <returns></returns>
            public IEnumerable<DataHolder> GetDataHolders()
            {
                return _allDataHolders.Values.AsEnumerable();
            }
            /// <summary>
            /// returns dataholders that match a list of given guid.
            /// </summary>
            /// <param name="guids">List of guid</param>
            /// <returns></returns>
            public IEnumerable<DataHolder> GetDataHolders(List<Guid> guids)
            {
                List<DataHolder> dataHolders = new List<DataHolder>();
                foreach (Guid guid in guids)
                {
                    dataHolders.Add(GetDataHolder(guid));
                }
                return dataHolders.AsEnumerable();
            }

            public void Load(JDictionary<Guid, T> dict, string filePath)
            {
                foreach (dynamic sd in dict.Values)
                {
                    _allSDs[sd.ID] = sd;
                    _allDataHolders[sd.ID] = new DataHolder(sd.Name, filePath, sd.ID);
                }
            }
            public void Load(List<T> list, string filePath)
            {
                foreach (dynamic sd in list)
                {
                    _allSDs[sd.ID] = sd;
                    _allDataHolders[sd.ID] = new DataHolder(sd.Name, filePath, sd.ID);
                }
            }

            public bool Save()
            {
                //Save tech data
                Dictionary<string, JDictionary<Guid, T>> sortedData = new Dictionary<string, JDictionary<Guid, T>>();
                foreach (DataHolder dataHolder in _allDataHolders.Values)
                {
                    if (!sortedData.ContainsKey(dataHolder.File))
                        sortedData[dataHolder.File] = new JDictionary<Guid, T>();
                    sortedData[dataHolder.File][dataHolder.Guid] = _allSDs[dataHolder.Guid];
                }

                foreach (string path in sortedData.Keys)
                {
                    DataExportContainer exportContainer = new DataExportContainer { Type = _typeString, Data = sortedData[path] };

                    using (StreamWriter streamWriter = new StreamWriter(path))
                    using (JsonWriter writer = new JsonTextWriter(streamWriter))
                    {
                        serializer.Serialize(writer, exportContainer);
                    }
                }

                return true;
            }

            public T Get(Guid guid)
            {
                T sd;
                if (_allSDs.TryGetValue(guid, out sd))
                    return sd;
                throw new Exception("Guid not found");
            }

            public void Update(dynamic sd)
            {
                if (!_allDataHolders.ContainsKey(sd.ID))
                {
                    if (string.IsNullOrWhiteSpace(_selectedFile))
                        return;

                    Create(sd);
                }
                else
                {
                    _allSDs[sd.ID] = sd;
                    _allDataHolders[sd.ID].Name = sd.Name;
                }
                
                if (!_loading && ListChanged != null)
                    ListChanged.Invoke();
            }

            private void Create(dynamic sd)
            {
                _allSDs[sd.ID] = sd;
                _allDataHolders[sd.ID] = new DataHolder(sd.Name, _selectedFile, sd.ID);
            }

            public void Remove(Guid guid)
            {
                _allSDs.Remove(guid);
                _allDataHolders.Remove(guid);

                if (!_loading && ListChanged != null)
                    ListChanged.Invoke();
            }

            public List<string> GetLoadedFiles()
            {
                return _loadedFiles.ToList();
            }

            public void SetSelectedFile(string path)
            {
                if (!_loadedFiles.Contains(path))
                    return;
                _selectedFile = path;

                if (LoadedFilesListChanged != null)
                    LoadedFilesListChanged.Invoke();
            }

            public void Clear()
            {
                _allSDs = new JDictionary<Guid, T>();
                _allDataHolders = new Dictionary<Guid, DataHolder>();
            }

            public void OnListChangeEvent(string filePath)
            {
                if (ListChanged != null)
                    ListChanged.Invoke();

                _loadedFiles.Add(filePath);
                if (String.IsNullOrWhiteSpace(_selectedFile))
                    _selectedFile = filePath;

                if (LoadedFilesListChanged != null)
                    LoadedFilesListChanged.Invoke();
            }
        }
    }

    public class DataHolder
    {
        public string Name { get; set; }
        public string File { get; private set; }
        public Guid Guid { get; private set; }

        public DataHolder(string name, string file, Guid guid)
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
