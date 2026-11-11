using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.Client;

/// <summary>
/// Scenario and mod-tick rules for the new-game wizard. No ImGui, so tests can call it directly.
/// A scenario is unselectable only when a required namespace has no installed mod. An installed
/// mod that is unticked does not block it; picking the scenario ticks what it requires.
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
    /// Required-mod namespaces that have no installed mod of that namespace.
    /// An installed mod that is unticked is still installed, so it is not listed.
    /// </summary>
    public static List<string> NotInstalledMods(
        ScenarioOption scenario,
        IReadOnlyList<ModOption> mods)
    {
        var missing = new List<string>();
        foreach (var ns in scenario.RequiredMods)
        {
            if (!mods.Any(m => m.Namespace == ns))
                missing.Add(ns);
        }
        return missing;
    }

    /// <summary>
    /// True when <paramref name="scenario"/> lists <paramref name="modNamespace"/> in
    /// <see cref="ScenarioOption.RequiredMods"/>. A null scenario requires nothing.
    /// </summary>
    public static bool IsRequired(ScenarioOption? scenario, string modNamespace)
    {
        if (scenario == null)
            return false;

        foreach (var ns in scenario.RequiredMods)
        {
            if (ns == modNamespace)
                return true;
        }
        return false;
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
