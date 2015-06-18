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
    /* Old Data Class
    public class Data
    {
        private static bool _loading;
        public static DataHolderAndEvents<TechSD> TechData = new DataHolderAndEvents<TechSD>("Techs");
        public static DataHolderAndEvents<InstallationSD> InstallationData = new DataHolderAndEvents<InstallationSD>("Installations");
        public static DataHolderAndEvents<MineralSD> MineralData = new DataHolderAndEvents<MineralSD>("Minerals");
        public static DataHolderAndEvents<ComponentSD> ComponentData = new DataHolderAndEvents<ComponentSD>("Components"); 

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
        private static void LoadData(JDictionary<Guid, ComponentSD> list, string filePath)
        {
            ComponentData.Load(list, filePath);
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
            /// returns True if guid exsists and passes out the dataHolder;
            /// see also GetDataHolder
            /// </summary>
            /// <param name="guid"></param>
            /// <param name="dataHolder"></param>
            /// <returns></returns>
            public bool TryGetDataHolder(Guid guid, out DataHolder dataHolder)
            {
                bool found = _allDataHolders.TryGetValue(guid, out dataHolder);
                return found;
            }

            /// <summary>
            /// Gets a dataholder from a given guid, throws an exception if not found.
            /// </summary>
            /// <param name="guid"></param>
            /// <param name="throwException">on guid not found will throw exception if true, if false will return null</param>
            /// <returns></returns>
            public DataHolder GetDataHolder(Guid guid, bool throwException = true)
            {
                DataHolder dataHolder;
                bool found = _allDataHolders.TryGetValue(guid, out dataHolder);
                if(!found && throwException)    
                    throw new Exception("Guid not found");
                else
                    return dataHolder;
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
            public IEnumerable<DataHolder> GetDataHolders(List<Guid> guids, bool throwException = true)
            {
                List<DataHolder> dataHolders = new List<DataHolder>();
                foreach (Guid guid in guids)
                {
                    dataHolders.Add(GetDataHolder(guid, throwException));
                }
                return dataHolders.AsEnumerable();
            }

            
            public IEnumerable<Guid> GetGuids(List<DataHolder> dataHolders)
            {
                List<Guid> guids = new List<Guid>();
                foreach (DataHolder dataHolder in dataHolders)
                {
                    guids.Add(dataHolder.Guid);
                }
                return guids;
            }

            public void Load(JDictionary<Guid, T> dict, string filePath)
            {
                foreach (dynamic sd in dict.Values)
                {
                    _allSDs[sd.ID] = sd;
                    _allDataHolders[sd.ID] = new DataHolder(sd, filePath);
                }
            }
            public void Load(List<T> list, string filePath)
            {
                foreach (dynamic sd in list)
                {
                    _allSDs[sd.ID] = sd;
                    _allDataHolders[sd.ID] = new DataHolder(sd, filePath);
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
                _allDataHolders[sd.ID] = new DataHolder(sd, _selectedFile); //new DataHolder(sd.Name, _selectedFile, sd.ID);
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
    */


    public class Data
    {
        public static Dictionary<Guid, DataHolder> InstallationData = new Dictionary<Guid, DataHolder>();
        public static Dictionary<Guid, DataHolder> ComponentData = new Dictionary<Guid, DataHolder>();
        public static Dictionary<Guid, DataHolder> MineralData = new Dictionary<Guid, DataHolder>();
        public static Dictionary<Guid, DataHolder> TechData = new Dictionary<Guid, DataHolder>();
        public static Dictionary<Guid, DataHolder> RefinedObjData = new Dictionary<Guid, DataHolder>();

        public static MainWindow MainWindow;

        public static void loadDatafromDirectory(string dir)
        {
            StaticDataManager.LoadFromDirectory(dir);

            foreach (var installationKVP in StaticDataManager.StaticDataStore.Installations)
            {
                InstallationData.Add(installationKVP.Key, new DataHolder(installationKVP.Value));
            }
            foreach (var componentKVP in StaticDataManager.StaticDataStore.Components)
            {
                ComponentData.Add(componentKVP.Key, new DataHolder(componentKVP.Value));
            }
            foreach (var mineralSD in StaticDataManager.StaticDataStore.Minerals)
            {
                MineralData.Add(mineralSD.ID, new DataHolder(mineralSD));
            }
            foreach (var techKVP in StaticDataManager.StaticDataStore.Techs)
            {
                TechData.Add(techKVP.Key, new DataHolder(techKVP.Value));
            }
            //foreach (var refinedData in StaticDataManager.StaticDataStore.RefinedMats)
            //{
            //    RefinedObjData.Add(refinedData.Key, new DataHolder(refinedData.Value));
            //}
        }

        public static void SaveDataToDirectory(string dir)
        {
            StaticDataManager.ExportStaticData(StaticDataManager.StaticDataStore.Installations, dir + ".InstallationData.json");
            StaticDataManager.ExportStaticData(StaticDataManager.StaticDataStore.Components, dir + ".ComponentData.json");
            StaticDataManager.ExportStaticData(StaticDataManager.StaticDataStore.Minerals, dir + ".MineralData.json");
            StaticDataManager.ExportStaticData(StaticDataManager.StaticDataStore.Techs, dir + ".TechnologyData.json");
        }

        /// <summary>
        /// saves the given staticData to the datastore, if the staticData.ID exsits it will update the exsisting with the new,
        /// if the staticData.ID does not exsist it will create a new entry. 
        /// *NOTE* this does not export(save) the json file to disk. 
        /// TODO: should this throw an exception, or return a bool if there is a problem? 
        /// </summary>
        /// <param name="staticData"></param>
        public static void SaveToDataStore(dynamic staticData)
        {
            if (staticData is InstallationSD)
            {
                if (!StaticDataManager.StaticDataStore.Installations.ContainsKey(staticData.ID))
                    StaticDataManager.StaticDataStore.Installations.Add(staticData.ID, staticData);
                else
                    StaticDataManager.StaticDataStore.Installations[staticData.ID] = staticData;
            }
            else if (staticData is ComponentSD)
            {
                if (!StaticDataManager.StaticDataStore.Components.ContainsKey(staticData.ID))
                    StaticDataManager.StaticDataStore.Components.Add(staticData.ID, staticData);
                else
                    StaticDataManager.StaticDataStore.Components[staticData.ID] = staticData;
            }
            else if (staticData is MineralSD)
            {
                int index = StaticDataManager.StaticDataStore.Minerals.IndexOf(staticData);
                if (index == -1) //if its not in the store
                    StaticDataManager.StaticDataStore.Minerals.Add(staticData);
                else
                    StaticDataManager.StaticDataStore.Minerals[index] = staticData;
            }
            else if (staticData is TechSD)
            {
                if (!StaticDataManager.StaticDataStore.Techs.ContainsKey(staticData.ID))
                    StaticDataManager.StaticDataStore.Techs.Add(staticData.ID, staticData);
                else
                    StaticDataManager.StaticDataStore.Techs[staticData.ID] = staticData;
            }
        }

        /// <summary>
        /// returns all the guid for a list of dataholders.
        /// </summary>
        /// <param name="listOfDataHolders"></param>
        /// <returns></returns>
        public static List<Guid> GetGuidList(List<DataHolder> listOfDataHolders)
        {
            List<Guid> list = new List<Guid>();
            foreach (var dataholder in listOfDataHolders)
            {
                list.Add(dataholder.Guid);
            }
            return list;
        }

        private Dictionary<Guid, DataHolder> GetTypeStore(DataHolder dataHolder)
        {
            if(dataHolder.StaticData is InstallationSD)
                return InstallationData;
            if(dataHolder.StaticData is ComponentSD)
                return ComponentData;
            if(dataHolder.StaticData is MineralSD)
                return MineralData;
            if (dataHolder.StaticData is TechSD)
                return TechData;
            return null;
        }
    }

    public class DataHolder
    {
        public dynamic StaticData { get; private set; }
        public string File { get; private set; }


        public string Name { get { return StaticData.Name; } }       
        public Guid Guid { get { return StaticData.ID; } }
        public string Description { get { return StaticData.Description; } }
        


        //public DataHolder(dynamic staticData, string file)
        //{   
        //    File = file;   
        //    StaticData = staticData;
        //}
        public DataHolder(dynamic staticData)
        {
            StaticData = staticData;
        }

        public DataHolder(ComponentAbilitySD staticData)
        {
            File = null;
            StaticData = staticData;
        }

        /// <summary>
        /// this is what will get displayed in listboxes etc. 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Name;
        }
    }
}
