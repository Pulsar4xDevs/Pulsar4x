using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json;

namespace Pulsar4X.Storage
{
    public class Store
    {
        /// <summary>
        /// Default Save game location
        /// </summary>
        private const string DefaultSaveGamePath = "Save";

        private readonly string _saveGameFileName;
        private string _saveGameDirectoryPath;
        private string _fullSaveGameFileNamePath;
        

        public Store(string saveGameFileName, string saveGameDirectoryPath)
        {
            if (string.IsNullOrEmpty(saveGameFileName)) throw  new ArgumentNullException("saveGameFileName");

            _saveGameDirectoryPath = saveGameDirectoryPath;
            _saveGameFileName = saveGameFileName;
        }

        /// <summary>
        /// Save the gamestate to the path given in the gamestate.SaveDirectoryPath.
        /// </summary>
        /// <param name="gameState"></param>
        public void SaveGame(GameState gameState)
        {
            if (string.IsNullOrEmpty(_saveGameDirectoryPath))
            {
                if (string.IsNullOrEmpty(gameState.SaveDirectoryPath))
                {
                    //using default saved game location
                    gameState.SaveDirectoryPath = Path.Combine(GetApplicationPath(), DefaultSaveGamePath, gameState.Name);
                }
                else
                {
                    _saveGameDirectoryPath = gameState.SaveDirectoryPath;
                }
            }
            _fullSaveGameFileNamePath = Path.Combine(_saveGameDirectoryPath, _saveGameFileName);

            //make sure the directory we are saving to exists
            if (!Directory.Exists(_saveGameDirectoryPath))
            {
                Directory.CreateDirectory(_saveGameDirectoryPath);
            }

            var serializer = new JsonSerializer();
            serializer.Formatting = Formatting.Indented;
            serializer.NullValueHandling = NullValueHandling.Include;
            serializer.PreserveReferencesHandling = PreserveReferencesHandling.All;

            using (StreamWriter sw = new StreamWriter(_fullSaveGameFileNamePath))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, gameState);
            }
        }

        private string GetApplicationPath()
        {
            UriBuilder uri = new UriBuilder(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
            return Path.GetDirectoryName(Uri.UnescapeDataString(uri.Path));
        }

        public GameState LoadGame(string fullFilePathToLoad)
        {
            var serializer = new JsonSerializer();
            serializer.Formatting = Formatting.Indented;
            serializer.NullValueHandling = NullValueHandling.Include;
            serializer.PreserveReferencesHandling = PreserveReferencesHandling.All;

            using (StreamReader rdr = new StreamReader(fullFilePathToLoad))
            using (JsonReader reader = new JsonTextReader(rdr))
            {
                return serializer.Deserialize<GameState>(reader);
            }
        }
    }
}
