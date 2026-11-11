using System.Collections.Generic;
using Newtonsoft.Json;

namespace Pulsar4X.Modding
{
    public class SystemManifest
    {
        /// <summary>Settings used to load and to serialize a system manifest so a round-trip
        /// keeps the same fields.</summary>
        public static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings();

        /// <summary>Filled by the loader from the manifest path.</summary>
        [JsonIgnore]
        public string SystemDirectory { get; set; }
        public string Author { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string Namespace { get; set; }
        public string Description { get; set; }

        /// <summary>Data files relative to the system folder.</summary>
        public List<string> DataFiles { get; set; }
    }
}
