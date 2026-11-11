using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.Client;

/// <summary>
/// Scenario and mod-tick rules for the new-game wizard. No ImGui, so tests can call it directly.
/// </summary>
public static class ScenarioCompatibility
{
    /// <summary>
    /// Required-mod namespaces that are not an enabled installed mod. A namespace is installed
    /// only when it is a key of <paramref name="enabledByNamespace"/>.
    /// </summary>
    public static List<string> MissingMods(
        ScenarioOption scenario,
        IReadOnlyDictionary<string, bool> enabledByNamespace)
    {
        var missing = new List<string>();
        foreach (var ns in scenario.RequiredMods)
        {
            if (!enabledByNamespace.TryGetValue(ns, out bool enabled) || !enabled)
                missing.Add(ns);
        }
        return missing;
    }

    /// <summary>
    /// Ticks every installed mod <paramref name="next"/> requires. Required mods of
    /// <paramref name="previous"/> that <paramref name="next"/> does not list go back to that
    /// mod's <see cref="ModOption.EnabledByDefault"/>. A namespace that is not installed is left
    /// out of <paramref name="enabledByNamespace"/>, so <see cref="MissingMods"/> still lists it.
    /// </summary>
    public static void ApplySelection(
        ScenarioOption? previous,
        ScenarioOption next,
        Dictionary<string, bool> enabledByNamespace,
        IReadOnlyList<ModOption> mods)
    {
        var nextRequired = new HashSet<string>(next.RequiredMods);
        if (previous != null)
        {
            foreach (var ns in previous.RequiredMods)
            {
                if (nextRequired.Contains(ns))
                    continue;
                var mod = mods.FirstOrDefault(m => m.Namespace == ns);
                if (mod != null)
                    enabledByNamespace[ns] = mod.EnabledByDefault;
            }
        }

        foreach (var ns in next.RequiredMods)
        {
            if (mods.Any(m => m.Namespace == ns))
                enabledByNamespace[ns] = true;
        }
    }
}
