using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Modding;
using Pulsar4X.Technology;

namespace Pulsar4X.Tests;

/// <summary>
/// A renamed tech id must not strand an old save: the save holds each faction's tech tree (keyed by id, with unlock lists) and
/// every lab's research queue, so loading rewrites the old ids to the new ones.
/// </summary>
[TestFixture]
public class TechIdMigrationTests
{
    static readonly string[] OldIds =
    {
        "tech-jumpoint-stabilization", "tech-jumpoint-stabilization-size-reduction", "tech-jumpoint-stabilization-time-reduction",
    };

    static (Game game, Entity faction, ModDataStore store) NewGame()
    {
        var loader = new ModLoader();
        var store = new ModDataStore();
        loader.LoadModManifest("Data/basemod/modInfo.json", store);
        loader.LoadScenario("Data/scenarios/warp-tech-start/scenarioInfo.json", store);
        var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false, MasterSeed = 4242 }, store);
        game.Settings.EnforceSingleThread = true;
        return (game, FactionFactory.CreateFaction(game, "Tech Typos"), store);
    }

    // Rewrites a faction's tech tree the way a save from before the rename held it.
    static void MakeOld(FactionDataStore data)
    {
        foreach (var dictionary in new[] { data.Techs, data.LockedTechs })
        {
            foreach (var oldId in OldIds)
            {
                var newId = TechIdMigration.Renamed[oldId];
                if (!dictionary.TryGetValue(newId, out var tech))
                    continue;
                dictionary.Remove(newId);
                tech.UniqueID = oldId;
                dictionary.Add(oldId, tech);
            }
            foreach (var tech in dictionary.Values)
                foreach (var ids in tech.Unlocks.Values)
                    for (int i = 0; i < ids.Count; i++)
                        if (TechIdMigration.Renamed.Values.Contains(ids[i]))
                            ids[i] = TechIdMigration.Renamed.First(r => r.Value == ids[i]).Key;
        }
    }

    [Test]
    public void TheShippedTechIds_AreSpelledRight_AndTheOldOnesAreGone()
    {
        var (_, _, store) = NewGame();
        foreach (var oldId in OldIds)
            Assert.That(store.Techs.ContainsKey(oldId), Is.False, oldId);
        foreach (var newId in TechIdMigration.Renamed.Values)
            Assert.That(store.Techs.ContainsKey(newId), Is.True, newId);
        Assert.That(store.Techs.Keys.Where(id => id.Contains("jumpoint")), Is.Empty);
        Assert.That(store.Techs.Values.Select(t => t.Name), Has.None.Contain("Efficency"));
        // No unlock list names a tech that does not exist.
        foreach (var tech in store.Techs.Values)
            foreach (var id in tech.Unlocks.Values.SelectMany(l => l))
                if (id.StartsWith("tech-"))
                    Assert.That(store.Techs.ContainsKey(id), Is.True, $"{tech.UniqueID} unlocks {id}");
    }

    [Test]
    public void AnOldSave_GetsTheNewIds_InTheTechTreeAndTheUnlockLists()
    {
        var (game, faction, _) = NewGame();
        var data = faction.GetDataBlob<FactionInfoDB>().Data;
        data.Unlock("tech-jumppoint-stabilization"); // one researched, the others still locked
        data.Techs["tech-jumppoint-stabilization"].Level = 1;
        MakeOld(data);
        Assert.That(data.Techs.ContainsKey("tech-jumpoint-stabilization"), "set up as an old save");
        Assert.That(data.LockedTechs.ContainsKey("tech-jumpoint-stabilization-size-reduction"));

        var json = Game.Save(game);
        int next = EntityIDGenerator.NextId;
        var loaded = Game.Load(json);
        EntityIDGenerator.NextId = Math.Max(next, EntityIDGenerator.NextId);

        var after = loaded.Factions.Values.Single(f => f.Id == faction.Id).GetDataBlob<FactionInfoDB>().Data;
        Assert.That(after.Techs.ContainsKey("tech-jumppoint-stabilization"), Is.True);
        Assert.That(after.Techs["tech-jumppoint-stabilization"].UniqueID, Is.EqualTo("tech-jumppoint-stabilization"));
        Assert.That(after.Techs["tech-jumppoint-stabilization"].Level, Is.EqualTo(1), "progress is kept");
        Assert.That(after.LockedTechs.ContainsKey("tech-jumppoint-stabilization-size-reduction"), Is.True);
        Assert.That(after.LockedTechs["tech-jumppoint-stabilization-time-reduction"].UniqueID, Is.EqualTo("tech-jumppoint-stabilization-time-reduction"));
        var allIds = after.Techs.Keys.Concat(after.LockedTechs.Keys)
            .Concat(after.Techs.Values.Concat(after.LockedTechs.Values).SelectMany(t => t.Unlocks.Values.SelectMany(l => l)));
        Assert.That(allIds.Where(id => id.Contains("jumpoint")), Is.Empty);
        Assert.That(after.LockedTechs.Values.Concat(after.Techs.Values)
            .SelectMany(t => t.Unlocks.Values.SelectMany(l => l)), Does.Contain("tech-jumppoint-stabilization"),
            "the parent tech still unlocks it, under the new id");
    }

    [Test]
    public void AnOldSave_HoldingARemovedGateNode_LoadsWithoutIt()
    {
        var (game, faction, _) = NewGame();
        var data = faction.GetDataBlob<FactionInfoDB>().Data;
        var anyTech = data.LockedTechs.Values.First();
        foreach (var removed in TechIdMigration.Removed)
        {
            // As an old save held it: a researched node that unlocked others, and another tech's list naming it.
            var node = new Tech(new Pulsar4X.Blueprints.TechBlueprint
            {
                UniqueID = removed, Name = removed, MaxLevel = 1, CostFormula = "0", DataFormula = "1", Category = "x",
                Unlocks = new Dictionary<int, List<string>> { [1] = new List<string> { anyTech.UniqueID } },
            }) { Level = 1 };
            data.Techs.Add(removed, node);
            anyTech.Unlocks[1] = new List<string> { removed };
        }

        var json = Game.Save(game);
        int next = EntityIDGenerator.NextId;
        var loaded = Game.Load(json);
        EntityIDGenerator.NextId = Math.Max(next, EntityIDGenerator.NextId);

        var after = loaded.Factions.Values.Single(f => f.Id == faction.Id).GetDataBlob<FactionInfoDB>().Data;
        foreach (var removed in TechIdMigration.Removed)
        {
            Assert.That(after.Techs.ContainsKey(removed), Is.False, removed);
            Assert.That(after.LockedTechs.ContainsKey(removed), Is.False, removed);
        }
        Assert.That(after.LockedTechs.Values.Concat(after.Techs.Values).SelectMany(t => t.Unlocks.Values.SelectMany(l => l)),
            Has.None.EqualTo("tech-modern-technology").And.None.EqualTo("tech-early-technology"));
        Assert.That(after.LockedTechs.ContainsKey(anyTech.UniqueID) || after.Techs.ContainsKey(anyTech.UniqueID), Is.True);
    }

    [Test]
    public void ARemovedGateNode_InAResearchQueue_IsDropped()
    {
        var lab = new ResearcherDB(null!);
        lab.TechQueue.Enqueue("tech-modern-technology");
        lab.TechQueue.Enqueue("tech-ion-engine");
        TechIdMigration.MigrateQueue(lab);
        Assert.That(lab.TechQueue.ToList(), Is.EqualTo(new[] { "tech-ion-engine" }));
    }

    [Test]
    public void ARenamedTech_InAResearchQueue_IsRenamedInPlace_KeepingTheOrder()
    {
        var lab = new ResearcherDB(null!);
        lab.TechQueue.Enqueue("tech-ion-drive");
        lab.TechQueue.Enqueue("tech-jumpoint-stabilization");
        lab.TechQueue.Enqueue("tech-jumpoint-stabilization-time-reduction");

        TechIdMigration.MigrateQueue(lab);
        Assert.That(lab.TechQueue.ToList(), Is.EqualTo(new[]
            { "tech-ion-drive", "tech-jumppoint-stabilization", "tech-jumppoint-stabilization-time-reduction" }));

        TechIdMigration.MigrateQueue(lab); // idempotent
        Assert.That(lab.TechQueue.Count, Is.EqualTo(3));
    }

    [Test]
    public void Current_MapsOldIds_AndLeavesTheRestAlone()
    {
        Assert.That(TechIdMigration.Current("tech-jumpoint-stabilization"), Is.EqualTo("tech-jumppoint-stabilization"));
        Assert.That(TechIdMigration.Current("tech-ion-drive"), Is.EqualTo("tech-ion-drive"));
    }
}
