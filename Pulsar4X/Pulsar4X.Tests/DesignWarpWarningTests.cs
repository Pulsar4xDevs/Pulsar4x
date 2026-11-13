using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Components;
using Pulsar4X.Energy;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Ships;

namespace Pulsar4X.Tests;

/// <summary>
/// Ship designer warp warnings (WarpMath.DesignWarpWarnings) on the Quickstart Tanker design and
/// variants of it. The design totals must match what the built Tanker I carries.
/// </summary>
[TestFixture]
[NonParallelizable]
public class DesignWarpWarningTests
{
    Entity _tanker = null!;
    List<(ComponentDesign design, int count)> _tankerParts = null!;

    [OneTimeSetUp]
    public void StartQuickstart()
    {
        ModsState.RefreshModsList("Data");
        var mods = ModsState.AvailableMods;
        var paths = new[] { mods.Single(m => m.Mod.ModName == "Pulsar4x").Path }.ToList();
        var store = GameLifecycle.LoadModStore(paths, mods.Select(m => m.Path), "Pulsar4x.Scenario.WarpTechStart");
        var created = GameLifecycle.CreateGameCore(store,
            GameLifecycle.BuildQuickstartRequest(paths, GameLifecycle.BuildCatalog(store), 12345));
        Assert.That(created, Is.Not.Null);
        var game = created!.Value.game;
        var player = created.Value.faction;
        _tanker = game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ShipInfoDB>())
            .Single(s => s.FactionOwnerID == player.Id && s.GetDataBlob<NameDB>().GetName(player.Id) == "Tanker I");
        _tankerParts = _tanker.GetDataBlob<ShipInfoDB>().Design.Components.ToList();

        Assert.That(Count<WarpDriveAtb>(_tankerParts), Is.EqualTo(1));
        Assert.That(Count<EnergyGenerationAtb>(_tankerParts), Is.EqualTo(2));
        Assert.That(Count<EnergyStoreAtb>(_tankerParts), Is.EqualTo(2));
    }

    static int Count<T>(List<(ComponentDesign design, int count)> parts) where T : IComponentDesignAttribute
        => parts.Where(p => p.design.HasAttribute<T>()).Sum(p => p.count);

    List<(ComponentDesign design, int count)> Tanker(int drives, int reactors, int batteries)
    {
        return _tankerParts.Select(p =>
            p.design.HasAttribute<WarpDriveAtb>() ? (p.design, drives)
            : p.design.HasAttribute<EnergyGenerationAtb>() ? (p.design, reactors)
            : p.design.HasAttribute<EnergyStoreAtb>() ? (p.design, batteries)
            : p).ToList();
    }

    [Test]
    public void Tanker_OneDriveTwoReactors_HasNoWarnings()
    {
        Assert.That(WarpMath.DesignWarpWarnings(_tankerParts), Is.Empty);
    }

    [Test]
    public void DesignTotals_MatchTheBuiltTanker()
    {
        // The sums DesignWarpWarnings makes, written out, equal what installation gave the built ship.
        var warp = _tanker.GetDataBlob<WarpAbilityDB>();
        var power = _tanker.GetDataBlob<EnergyGenAbilityDB>();
        double liveStore = power.EnergyStoreMax[warp.EnergyType];
        double liveOutput = power.MaxOutputFromReactor;

        double designSustain = 0, designCreation = 0, designOutput = 0, designStore = 0;
        foreach (var (design, count) in _tankerParts)
        {
            if (design.TryGetAttribute<WarpDriveAtb>(out var d)) { designSustain += d.BubbleSustainCost * count; designCreation += d.BubbleCreationCost * count; }
            if (design.TryGetAttribute<EnergyGenerationAtb>(out var g)) { designOutput += g.PowerOutputMax * count; designStore += g.PowerOutputMax * count; }
            if (design.TryGetAttribute<EnergyStoreAtb>(out var b)) designStore += b.MaxStore * count;
        }
        Assert.That(designSustain, Is.EqualTo(warp.BubbleSustainCost));
        Assert.That(designCreation, Is.EqualTo(warp.BubbleCreationCost));
        Assert.That(designOutput, Is.EqualTo(liveOutput));
        Assert.That(designStore, Is.EqualTo(liveStore), "batteries plus one second of each reactor");
    }

    [Test]
    public void TwoDrivesOneReactor_WarnsSustainOnly()
    {
        var warnings = WarpMath.DesignWarpWarnings(Tanker(drives: 2, reactors: 1, batteries: 4));
        Assert.That(warnings, Has.Count.EqualTo(1));
        Assert.That(warnings[0], Does.StartWith("Can't warp: bubble sustain needs"));
        Assert.That(warnings[0], Does.Contain("plant makes"));
    }

    [Test]
    public void TwoDrivesTwoBatteries_WarnsCreationOnly()
    {
        var warnings = WarpMath.DesignWarpWarnings(Tanker(drives: 2, reactors: 2, batteries: 2));
        Assert.That(warnings, Has.Count.EqualTo(1));
        Assert.That(warnings[0], Does.StartWith("Can't warp: bubble creation needs"));
        Assert.That(warnings[0], Does.Contain("batteries hold"));
    }

    [Test]
    public void NoDrive_HasNoWarnings()
    {
        Assert.That(WarpMath.DesignWarpWarnings(Tanker(drives: 0, reactors: 0, batteries: 0)
            .Where(p => !p.design.HasAttribute<WarpDriveAtb>()).ToList()), Is.Empty);
    }
}
