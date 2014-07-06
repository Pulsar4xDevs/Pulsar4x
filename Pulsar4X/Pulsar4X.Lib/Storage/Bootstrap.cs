using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Pulsar4X.Entities;

namespace Pulsar4X.Storage
{
    public class Bootstrap
    {
        private readonly string _gameDirectoryPath;
        private readonly string _dataDirectoryPath;
        private const string BaseDirectory = "Data";

        public Bootstrap()
        {
            var uri = new UriBuilder(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
            _gameDirectoryPath = Path.GetDirectoryName(Uri.UnescapeDataString(uri.Path));
            _dataDirectoryPath = Path.Combine(_gameDirectoryPath, BaseDirectory);

            if (!Directory.Exists(_dataDirectoryPath))
                Directory.CreateDirectory(_dataDirectoryPath);
        }


        private const string CommanderNameThemeDataFileName = "CommanderNameThemes.json";
        public List<CommanderNameTheme> LoadCommanderNameTheme()
        {
            var serializer = new JsonSerializer();
            serializer.Formatting = Formatting.Indented;
            serializer.NullValueHandling = NullValueHandling.Include;

            var file = Path.Combine(_dataDirectoryPath, CommanderNameThemeDataFileName);
            if (File.Exists(file) == false) return new List<CommanderNameTheme>();

            using (var rdr = new StreamReader(file))
            using (JsonReader reader = new JsonTextReader(rdr))
            {
                return serializer.Deserialize<List<CommanderNameTheme>>(reader);
            }
        }

        public void SaveCommanderNameTheme(List<CommanderNameTheme> themes)
        {
            var serializer = new JsonSerializer();
            serializer.Formatting = Formatting.Indented;
            serializer.NullValueHandling = NullValueHandling.Include;

            var file = Path.Combine(_dataDirectoryPath, CommanderNameThemeDataFileName);
            using (var sw = new StreamWriter(file))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, themes);
            }
        }

    }
}
