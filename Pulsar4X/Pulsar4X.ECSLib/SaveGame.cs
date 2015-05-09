using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.IO;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// This cals is responsible for saving a game to/from disk.
    /// </summary>
    // use: http://www.newtonsoft.com/json/help/html/SerializationAttributes.htm
    public class SaveGame
    {
        public string File
        {
            get; set; 
        }

        private struct SaveData
        {
            public VersionInfo Version;
            public DateTime GameDateTime;
            public EntityManager GlobalEntityManager;
            public List<StarSystem> StarSystems;
            public StaticDataStore StaticData;
        }

        private SaveData _data;
        private JsonSerializer _serializer;

        public SaveGame(string file = null)
        {
            File = file;
            _serializer = new JsonSerializer { NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented, ContractResolver = new ForceUseISerializable() };
        }

        public void Save(string file = null)
        {
            CheckAndUpdateFile(file);

            // first collect the data:
            CollectGameData();

            using (StreamWriter sw = new StreamWriter(File))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                _serializer.Serialize(writer, _data);
            }
        }

        public void Load(string file = null)
        {
            CheckAndUpdateFile(file);

            using (StreamReader sr = new StreamReader(File))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                _data = (SaveData)_serializer.Deserialize(reader, typeof(SaveData));
            }

            // check the version info:
            if (_data.Version.IsCompatibleWith(VersionInfo.PulsarVersionInfo) == false)
            {
                string e = String.Format("The save file is not supported. the save is from version {0}, the game only supports versions: {1}", _data.Version.VersionString, VersionInfo.PulsarVersionInfo.CompatibileVersions);

                throw new System.NotSupportedException(e);
            }

            // set the static data:
            StaticDataManager.StaticDataStore = _data.StaticData;

            // get the game to do its post load stuff
            Game.Instance.PostGameLoad(_data.GameDateTime, _data.GlobalEntityManager, _data.StarSystems);
        }

        /// <summary>
        /// Check if we have a valid file, if we do it updates the cached file record.
        /// </summary>
        /// <param name="file">file to check.</param>
        private void CheckAndUpdateFile(string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                // check if cached file valid: 
                if (string.IsNullOrEmpty(File))
                {
                    throw new ArgumentNullException("No valid file path provided.");
                }
            }
            else
            {
                // @todo add more validity checks here.

                // if we have a problem with the file we should throw before this...
                File = file;
            }
        }

        private void CollectGameData()
        {
            _data.Version = VersionInfo.PulsarVersionInfo;
            _data.GlobalEntityManager = Game.Instance.GlobalManager;
            _data.StarSystems = Game.Instance.StarSystems;
            _data.GameDateTime = Game.Instance.CurrentDateTime;
            _data.StaticData = StaticDataManager.StaticDataStore;
        }
    }

    /// <summary>
    /// A small interface that defines the PostLaod function for datablobs and other classes to use for post de-serialization work.
    /// It also defines the RegisterPostLoad function that should be called in all an implimenters constructors.
    /// </summary>
    public interface IPostLoad
    {
        /// <summary>
        /// This function should be added to all of an implimenters constructors.
        /// To implement this function simply add the lines
        /// <code>
        /// if(!Game.Instance.IsLoaded)
        ///     Game.Instance.PostLoad += new EventHandler(PostLoad);
        /// </code>
        /// to the method body.
        /// </summary>
        void RegisterPostLoad();

        /// <summary>
        /// This function is called after the game has been loaded/Deserialized.
        /// Make sure you unsubscribe from the post Load event at the end of this function by using the following:
        /// <code>Game.Instance.PostLoad -= PostLoad;</code>
        /// </summary>
        void PostLoad(object sender, EventArgs e);
    }
}
