using System.Collections.Generic;

namespace Pulsar4X.Modding
{
    public class ModManifest
    {
        public string ModDirectory { get; set; }
        public string Author { get; set; }
        public string ModName { get; set; }
        public string Version { get; set; }
        public string Namespace { get; set; } // Unique mod identifier
        public List<string> DataFiles { get; set; } // List of paths to mod data files
        public string PlayerFactionStartingItems { get; set; }
        public string AIFactionStartingItems { get; set; }
        public bool DefaultEnabled { get; set; } = false;

        /// <summary>Namespaces of the mods this one builds on. They are always loaded before it.</summary>
        public List<string>? Dependencies { get; set; }
    }
}