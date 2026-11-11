using System.Collections.Generic;
using Newtonsoft.Json;

namespace Pulsar4X.Modding
{
    public class ScenarioManifest
    {
        /// <summary>Settings used to load and to serialize a scenario manifest so a round-trip
        /// keeps the same fields. Null values are left out, so a field omitted from the file
        /// stays null.</summary>
        public static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        /// <summary>Filled by the loader from the manifest path.</summary>
        [JsonIgnore]
        public string ScenarioDirectory { get; set; }
        public string Author { get; set; }
        public string ScenarioName { get; set; }
        public string Version { get; set; }
        public string Namespace { get; set; }
        public string Description { get; set; }
        public bool DefaultEnabled { get; set; } = false;

        /// <summary>Namespaces of the mods this scenario needs. They must already be loaded.</summary>
        public List<string> RequiredMods { get; set; }

        /// <summary>Namespaces of scenarios applied first, in order.</summary>
        public List<string> RequiredScenarios { get; set; }

        /// <summary>Namespaces of the system packs this scenario includes, in order.
        /// Applied before this scenario's own data files.</summary>
        public List<string> IncludedSystems { get; set; }

        /// <summary>Data files relative to the scenario folder.</summary>
        public List<string> DataFiles { get; set; }

        /// <summary>Values this scenario contributes to a new game. A later scenario in the
        /// RequiredScenarios chain overwrites only the fields it sets.</summary>
        public ScenarioStart DefaultStart { get; set; }

        public class ScenarioStart
        {
            public string Species { get; set; }
            public string Colony { get; set; }
            public string System { get; set; }
            public string Body { get; set; }
            public string Theme { get; set; }
            public int? StartingFunds { get; set; }
            public int? MaxSystems { get; set; }
            public bool? EleStart { get; set; }
            public string FactionName { get; set; }
            public string FactionAbbreviation { get; set; }

            /// <summary>Merges starts in order, base first. A later start overwrites a field
            /// only when it sets one: non-null, and for strings non-empty. Null when every
            /// start is null.</summary>
            public static ScenarioStart? Merge(IEnumerable<ScenarioStart?> starts)
            {
                ScenarioStart? merged = null;
                if (starts == null)
                    return null;

                foreach (var start in starts)
                {
                    if (start == null)
                        continue;
                    if (merged == null)
                        merged = new ScenarioStart();

                    if (!string.IsNullOrEmpty(start.Species))
                        merged.Species = start.Species;
                    if (!string.IsNullOrEmpty(start.Colony))
                        merged.Colony = start.Colony;
                    if (!string.IsNullOrEmpty(start.System))
                        merged.System = start.System;
                    if (!string.IsNullOrEmpty(start.Body))
                        merged.Body = start.Body;
                    if (!string.IsNullOrEmpty(start.Theme))
                        merged.Theme = start.Theme;
                    if (start.StartingFunds != null)
                        merged.StartingFunds = start.StartingFunds;
                    if (start.MaxSystems != null)
                        merged.MaxSystems = start.MaxSystems;
                    if (start.EleStart != null)
                        merged.EleStart = start.EleStart;
                    if (!string.IsNullOrEmpty(start.FactionName))
                        merged.FactionName = start.FactionName;
                    if (!string.IsNullOrEmpty(start.FactionAbbreviation))
                        merged.FactionAbbreviation = start.FactionAbbreviation;
                }

                return merged;
            }
        }
    }
}
