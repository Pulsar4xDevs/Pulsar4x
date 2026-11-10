using System.Collections.Generic;

namespace Pulsar4X.Modding
{
    public class ScenarioManifest
    {
        /// <summary>Filled by the loader from the manifest path.</summary>
        public string ScenarioDirectory { get; set; }
        public string Author { get; set; }
        public string ScenarioName { get; set; }
        public string Version { get; set; }
        public string Namespace { get; set; }
        public string Description { get; set; }
        public bool DefaultEnabled { get; set; } = false;

        /// <summary>Namespaces of the mods this scenario needs. They must already be loaded.</summary>
        public List<string> RequiredMods { get; set; }

        /// <summary>Data files relative to the scenario folder.</summary>
        public List<string> DataFiles { get; set; }

        /// <summary>Default species, colony, system, and body for a new game.</summary>
        public ScenarioStart DefaultStart { get; set; }

        public class ScenarioStart
        {
            public string Species { get; set; }
            public string Colony { get; set; }
            public string System { get; set; }
            public string Body { get; set; }
        }
    }
}
