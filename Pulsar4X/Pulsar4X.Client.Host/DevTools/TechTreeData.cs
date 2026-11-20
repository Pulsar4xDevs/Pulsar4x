using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Blueprints;
using Pulsar4X.Client.Host;
using Pulsar4X.Modding;

namespace Pulsar4X.Client;

/// <summary>Maps the engine's loaded <see cref="ModDataStore"/> tech blueprints into a <see cref="TechTreeInput"/>.</summary>
public static class TechTreeData
{
    public static TechTreeInput From(ModDataStore store)
    {
        var categories = store.TechCategories ?? new Dictionary<string, TechCategoryBlueprint>();
        var techs = new List<TechTreeSpec>();
        foreach (var tech in (store.Techs ?? new Dictionary<string, TechBlueprint>()).Values)
        {
            if (tech == null || string.IsNullOrEmpty(tech.UniqueID))
                continue;
            string categoryId = tech.Category ?? "";
            string categoryName = categories.TryGetValue(categoryId, out var category) && !string.IsNullOrEmpty(category.Name)
                ? category.Name
                : categoryId;
            techs.Add(new TechTreeSpec(
                tech.UniqueID,
                string.IsNullOrEmpty(tech.Name) ? tech.UniqueID : tech.Name,
                tech.Description ?? "",
                categoryId,
                categoryName,
                tech.CostFormula ?? "",
                tech.MaxLevel,
                Flatten(tech.Unlocks)));
        }

        var unlockables = new List<TechTreeUnlockable>();
        Add(unlockables, store.ComponentTemplates, "component", t => t.Name);
        Add(unlockables, store.Armor, "armor", a => string.IsNullOrEmpty(a.ResourceID) ? a.UniqueID : a.ResourceID);
        Add(unlockables, store.CargoTypes, "cargo-type", c => c.Name);
        Add(unlockables, store.IndustryTypes, "industry-type", i => i.Name);
        Add(unlockables, store.Minerals, "mineral", m => m.Name);
        Add(unlockables, store.ProcessedMaterials, "material", m => m.Name);
        return new TechTreeInput(techs, unlockables);
    }

    /// <summary>
    /// Prefer the current game's loaded data. If no game is running, load the enabled mods
    /// (or every available mod) so the visualiser still works as a tuning tool.
    /// </summary>
    public static (TechTreeInput? Input, string Source) Load()
    {
        var store = GameLifecycle.Instance?.Game?.StartingGameData;
        if (store != null)
            return (From(store), "current game");

        if (ModsState.AvailableMods.Count == 0)
            return (null, "no mods");

        var enabled = ModsState.AvailableMods
            .Where(m => ModsState.IsModEnabled.TryGetValue(m.Mod.Namespace, out bool on) && on)
            .Select(m => m.Path)
            .ToList();
        if (enabled.Count == 0)
            enabled = ModsState.AvailableMods.Select(m => m.Path).ToList();

        store = GameLifecycle.LoadModStore(enabled, ModsState.AvailableMods.Select(m => m.Path));
        return (From(store), "enabled mods");
    }

    static IReadOnlyList<TechTreeUnlock> Flatten(Dictionary<int, List<string>>? unlocks)
    {
        var list = new List<TechTreeUnlock>();
        if (unlocks == null)
            return list;
        foreach (var kv in unlocks.OrderBy(k => k.Key))
        {
            if (kv.Value == null)
                continue;
            foreach (var id in kv.Value)
            {
                if (!string.IsNullOrEmpty(id))
                    list.Add(new TechTreeUnlock(kv.Key, id));
            }
        }
        return list;
    }

    static void Add<T>(List<TechTreeUnlockable> list, Dictionary<string, T>? source, string kind, Func<T, string?> name)
        where T : Blueprint
    {
        if (source == null)
            return;
        foreach (var kv in source)
        {
            if (string.IsNullOrEmpty(kv.Key) || kv.Value == null)
                continue;
            string label = name(kv.Value) ?? kv.Key;
            list.Add(new TechTreeUnlockable(kv.Key, string.IsNullOrEmpty(label) ? kv.Key : label, kind));
        }
    }
}
