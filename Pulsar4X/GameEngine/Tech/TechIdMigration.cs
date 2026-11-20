using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Engine;
using Pulsar4X.Factions;

namespace Pulsar4X.Technology;

/// <summary>
/// Tech ids that were renamed or removed. A save stores each faction's whole tech tree (keyed by id, with each tech's unlock lists) and every
/// lab's research queue, so a renamed id would leave an old save pointing at nothing. <see cref="AfterLoad"/> rewrites the old ids.
/// </summary>
public static class TechIdMigration
{
    /// <summary>Old id → new id. Add a row here whenever a tech id is renamed.</summary>
    public static readonly IReadOnlyDictionary<string, string> Renamed = new Dictionary<string, string>
    {
        ["tech-jumpoint-stabilization"] = "tech-jumppoint-stabilization",
        ["tech-jumpoint-stabilization-size-reduction"] = "tech-jumppoint-stabilization-size-reduction",
        ["tech-jumpoint-stabilization-time-reduction"] = "tech-jumppoint-stabilization-time-reduction",
    };

    /// <summary>
    /// Tech nodes that were removed (they only gated scenarios: nothing was ever researched through them). A save that holds one
    /// has it dropped on load, from the tech tree, from other techs' unlock lists and from every research queue.
    /// </summary>
    public static readonly IReadOnlySet<string> Removed = new HashSet<string>
    {
        "tech-modern-technology",
        "tech-early-technology",
    };

    /// <summary>The current id for an id from any version of the data.</summary>
    public static string Current(string id) => Renamed.TryGetValue(id, out var renamed) ? renamed : id;

    internal static void AfterLoad(Game game)
    {
        foreach (var faction in game.Factions.Values.Append(game.GameMasterFaction).Distinct())
        {
            if (faction != null && faction.TryGetDataBlob<FactionInfoDB>(out var info) && info.Data != null)
                Migrate(info.Data);
        }
        foreach (var manager in game.GlobalManagerDictionary.Values)
        {
            foreach (var lab in manager.GetAllDataBlobsOfType<ResearcherDB>())
                MigrateQueue(lab);
        }
        foreach (var system in game.Systems)
        {
            foreach (var lab in system.GetAllDataBlobsOfType<ResearcherDB>())
                MigrateQueue(lab);
        }
    }

    /// <summary>Rekeys renamed techs in a faction's data and rewrites every unlock list that names one.</summary>
    internal static void Migrate(FactionDataStore data)
    {
        foreach (var dictionary in new[] { data.Techs, data.LockedTechs })
        {
            foreach (var (oldId, newId) in Renamed)
            {
                if (!dictionary.TryGetValue(oldId, out var tech))
                    continue;
                dictionary.Remove(oldId);
                tech.UniqueID = newId;
                if (!dictionary.ContainsKey(newId))
                    dictionary.Add(newId, tech);
            }
            foreach (var removed in Removed)
                dictionary.Remove(removed);
            foreach (var tech in dictionary.Values)
            {
                if (tech.Unlocks == null)
                    continue;
                foreach (var ids in tech.Unlocks.Values)
                {
                    for (int i = 0; i < ids.Count; i++)
                        ids[i] = Current(ids[i]);
                    ids.RemoveAll(Removed.Contains);
                }
            }
        }
    }

    internal static void MigrateQueue(ResearcherDB lab)
    {
        var queued = lab.TechQueue.ToList().ToList();
        if (!queued.Any(id => Renamed.ContainsKey(id) || Removed.Contains(id)))
            return;
        while (lab.TechQueue.TryDequeue(out _)) { }
        foreach (var id in queued.Where(id => !Removed.Contains(id)))
            lab.TechQueue.Enqueue(Current(id));
    }
}
