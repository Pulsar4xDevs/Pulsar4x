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
        // Summed in a different order from installation, so allow float noise (the 3 t reactors are not round numbers).
        Assert.That(designStore, Is.EqualTo(liveStore).Within(1e-6), "batteries plus one second of each reactor");
    }

    [Test]
    public void TwoDrivesOneReactor_WarnsSustainOnly()
    {
        var warnings = WarpMath.DesignWarpWarnings(Tanker(drives: 2, reactors: 1, batteries: 4));
        Assert.That(warnings, Has.Count.EqualTo(1));
        // Crew plan phase 4: the sustain line counts the crew's life support (1 kW per crew at tech 0).
        Assert.That(warnings[0], Does.StartWith("Can't warp: bubble sustain"));
        Assert.That(warnings[0], Does.Contain("plus life support"));
        Assert.That(warnings[0], Does.Contain("plant makes"));
    }

    [Test]
    public void TwoDrivesTwoBatteries_WarnsCreationOnly()
    {
        // Three reactors: two drives' sustain plus the crew's life support fits the plant (crew plan phase 4).
        var warnings = WarpMath.DesignWarpWarnings(Tanker(drives: 2, reactors: 3, batteries: 2));
        Assert.That(warnings, Has.Count.EqualTo(1));
        Assert.That(warnings[0], Does.StartWith("Can't warp: bubble creation needs"));
        Assert.That(warnings[0], Does.Contain("batteries hold"));
    }

    [Test]
    public void LifeSupport_CountsInTheSustainCheck()
    {
        // Crew plan phase 4: one drive and one 3 t reactor (25.4 kW) holds the 15 kW bubble alone,
        // but not with the crew's life support on top.
        var parts = Tanker(drives: 1, reactors: 1, batteries: 4);
        double output = parts.Where(p => p.design.HasAttribute<EnergyGenerationAtb>()).Sum(p => p.design.GetAttribute<EnergyGenerationAtb>().PowerOutputMax * p.count);
        double sustain = parts.Where(p => p.design.HasAttribute<WarpDriveAtb>()).Sum(p => p.design.GetAttribute<WarpDriveAtb>().BubbleSustainCost * p.count);
        int crew = ShipCrew.Split(parts).Capacity;
        Assert.That(output, Is.GreaterThan(sustain));
        Assert.That(output, Is.LessThan(sustain + crew * 1.0));
        var warnings = WarpMath.DesignWarpWarnings(parts);
        Assert.That(warnings, Has.Count.EqualTo(1));
        Assert.That(warnings[0], Does.Contain("plus life support"));
    }

    [Test]
    public void NoDrive_HasNoWarnings()
    {
        Assert.That(WarpMath.DesignWarpWarnings(Tanker(drives: 0, reactors: 0, batteries: 0)
            .Where(p => !p.design.HasAttribute<WarpDriveAtb>()).ToList()), Is.Empty);
    }
}
