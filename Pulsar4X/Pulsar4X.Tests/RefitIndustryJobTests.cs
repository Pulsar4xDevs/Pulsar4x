using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Factions;
using Pulsar4X.Industry;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

/// <summary>
/// A shipyard refit is a normal industry job on the yard's line. It spends the yard's ship-refit points,
/// shows in the line's job list with progress, and completes like any job, beside a new-ship build.
/// </summary>
[TestFixture]
[NonParallelizable]
public class RefitIndustryJobTests
{
    const string WarpTech = "Pulsar4x.Scenario.WarpTechStart";
    const string EarlyTech = "Pulsar4x.Scenario.EarlyTech";

    [TestCase(WarpTech, false)]
    [TestCase(EarlyTech, false)]
    [TestCase(WarpTech, true)]
    [TestCase(EarlyTech, true)]
    public void RefitAndNewShipBuild_BothComplete_InTheSameYard(string scenario, bool buildQueuedFirst)
    {
        var (game, faction) = Start(scenario);
        var (yard, lineId) = FindYard(game, faction);
        var info = faction.GetDataBlob<FactionInfoDB>();
        var line = yard.GetDataBlob<IndustryAbilityDB>().ProductionLines[lineId];
        Assert.That(yard.GetDataBlob<LocalConstructionDB>().RefitPointsPerDay, Is.GreaterThan(0), "the start's yard has a refit rate");

        var (from, to) = PickPair(yard, info, line);
        var ship = NewShip(yard, faction, from);
        var (remove, mount) = Diff(yard, ship, to);
        Assert.That(remove.Count + mount.Count, Is.GreaterThan(0), "the two designs differ");

        var translator = new CommandTranslator(game);
        void QueueBuild()
        {
            var queuedBuild = translator.Translate(faction, yard, new QueueIndustryJobCommand(yard.Id, lineId, from.UniqueID, 1, false, false));
            Assert.That(queuedBuild.Accepted, Is.True, queuedBuild.RejectionReason);
        }
        void QueueRefit()
        {
            var queuedRefit = translator.Translate(faction, yard, new QueueShipRefitCommand(
                yard.Id, lineId, ship.Id, remove.Select(p => p.ID).ToList(), mount.Select(p => p.ID).ToList(), to.UniqueID));
            Assert.That(queuedRefit.Accepted, Is.True, queuedRefit.RejectionReason);
        }
        if (buildQueuedFirst) { QueueBuild(); QueueRefit(); } else { QueueRefit(); QueueBuild(); }
        StockResources(yard, line, info);
        int hullsBefore = line.Hulls.Count;

        var view = new GameProjector(game).ProjectEntity(yard, faction.Id).GetView<IndustryView>()!
            .ProductionLines.Single(l => l.Id == lineId);
        var refitView = view.Jobs.Single(j => j.IsRefit);
        Assert.That(view.Jobs, Has.Count.EqualTo(2));
        Assert.That(refitView.Refit!.ShipEntityId, Is.EqualTo(ship.Id));
        Assert.That(refitView.Refit.FromDesign, Is.EqualTo(from.Name));
        Assert.That(refitView.Refit.ToDesign, Is.EqualTo(to.Name));
        Assert.That(refitView.NumberOrdered, Is.EqualTo(1));
        Assert.That(refitView.ProductionPointsLeft, Is.GreaterThan(0));

        int days = 0;
        while (days++ < 600 && (ShipRefitJob.JobsOf(yard).Any() || line.Hulls.Count == hullsBefore || line.Jobs.Count > 0))
            new IndustryProcessor().ProcessEntity(yard, 0);

        Assert.That(ShipRefitJob.JobsOf(yard), Is.Empty, "the refit finished");
        Assert.That(ship.GetDataBlob<ShipInfoDB>().Design.UniqueID, Is.EqualTo(to.UniqueID), "the ship wears the new design");
        var counts = ship.GetDataBlob<ComponentInstancesDB>().AllComponents.Values
            .GroupBy(c => c.Design.UniqueID).ToDictionary(g => g.Key, g => g.Count());
        foreach (var (part, count) in to.Components)
            Assert.That(counts.GetValueOrDefault(part.UniqueID), Is.EqualTo(count), part.Name);
        Assert.That(line.Hulls, Has.Count.EqualTo(hullsBefore + 1), "the new-ship build finished too");
        Assert.That(line.Jobs, Is.Empty);
        TestContext.Out.WriteLine($"{scenario}: both done in {days - 1} days");
    }

    [TestCase(WarpTech)]
    [TestCase(EarlyTech)]
    public void Shipyard_StillRefusesComponentJobs_ButAcceptsRefits(string scenario)
    {
        var (game, faction) = Start(scenario);
        var (yard, lineId) = FindYard(game, faction);
        var info = faction.GetDataBlob<FactionInfoDB>();
        var component = info.IndustryDesigns.Values.OfType<ComponentDesign>().First(c => c.IndustryTypeID == "component-construction");

        var refused = new CommandTranslator(game).Translate(faction, yard,
            new QueueIndustryJobCommand(yard.Id, lineId, component.UniqueID, 1, false, false));

        Assert.That(refused.Accepted, Is.False);
        Assert.That(refused.RejectionReason, Does.Contain("Shipyards build only ships"));
        var line = yard.GetDataBlob<IndustryAbilityDB>().ProductionLines[lineId];
        Assert.That(IndustryTools.LineAccepts(line, component), Is.False);
        Assert.That(IndustryTools.LineAccepts(line, new ShipRefitDesign("Refit", 1)), Is.True);
        Assert.That(line.Jobs, Is.Empty);
    }

    [Test]
    public void RefitJob_CanBeCancelled_AndAShipOutOfRangeWaitsWithAReason()
    {
        var (game, faction) = Start(WarpTech);
        var (yard, lineId) = FindYard(game, faction);
        var info = faction.GetDataBlob<FactionInfoDB>();
        var line = yard.GetDataBlob<IndustryAbilityDB>().ProductionLines[lineId];
        var (from, to) = PickPair(yard, info, line);
        var ship = NewShip(yard, faction, from);
        var (remove, mount) = Diff(yard, ship, to);
        var translator = new CommandTranslator(game);
        var queued = translator.Translate(faction, yard, new QueueShipRefitCommand(
            yard.Id, lineId, ship.Id, remove.Select(p => p.ID).ToList(), mount.Select(p => p.ID).ToList()));
        Assert.That(queued.Accepted, Is.True, queued.RejectionReason);

        // Take a part off the ship behind the job's back: the job waits and says why.
        var gone = remove.FirstOrDefault() ?? mount.First();
        if (remove.Count > 0)
            ship.RemoveComponent(gone);
        else
            yard.GetDataBlob<CargoStorageDB>().RemoveCargoByUnit(gone, 1);
        new IndustryProcessor().ProcessEntity(yard, 0);
        var job = line.Jobs.Single();
        Assert.That(job.Status, Is.EqualTo(Pulsar4X.DataStructures.IndustryJobStatus.MissingResources));
        Assert.That(job.ProductionPointsLeft, Is.EqualTo(job.ProductionPointsCost), "no points spent while it waits");
        var view = new GameProjector(game).ProjectEntity(yard, faction.Id).GetView<IndustryView>()!
            .ProductionLines.Single(l => l.Id == lineId).Jobs.Single();
        Assert.That(view.Refit!.WaitReason, Is.Not.Empty);

        var cancel = translator.Translate(faction, yard, new CancelIndustryJobCommand(yard.Id, lineId, job.JobID));
        Assert.That(cancel.Accepted, Is.True, cancel.RejectionReason);
        Assert.That(line.Jobs, Is.Empty);
        Assert.That(ShipRefitJob.TryFind(ship, out _, out _), Is.False);
    }

    [TestCase(WarpTech)]
    [TestCase(EarlyTech)]
    public void RemovingABridge_DropsItsSeat_AndDoesNotThrow(string scenario)
    {
        var (game, faction) = Start(scenario);
        var (yard, _) = FindYard(game, faction);
        var info = faction.GetDataBlob<FactionInfoDB>();
        var design = info.ShipDesigns.Values.First(d => d.Components.Any(c => c.Item1.HasAttribute<GameEngine.People.AdminSpaceAtb>()));
        var ship = NewShip(yard, faction, design);
        var seats = ship.GetDataBlob<GameEngine.People.AdminSpaceDB>().CommanderSeats;
        int before = seats.Count;
        Assert.That(before, Is.GreaterThan(0));
        var bridge = ship.GetDataBlob<ComponentInstancesDB>().AllComponents.Values
            .First(c => c.Design.HasAttribute<GameEngine.People.AdminSpaceAtb>());

        Assert.DoesNotThrow(() => ship.RemoveComponent(bridge));

        Assert.That(ship.GetDataBlob<GameEngine.People.AdminSpaceDB>().CommanderSeats, Has.Count.EqualTo(before - 1));
    }

    static (Game game, Entity faction) Start(string scenario)
    {
        ModsState.RefreshModsList("Data");
        var mods = ModsState.AvailableMods;
        var paths = new[] { mods.Single(m => m.Mod.ModName == "Pulsar4x").Path }.ToList();
        var store = GameLifecycle.LoadModStore(paths, mods.Select(m => m.Path), scenario);
        var created = GameLifecycle.CreateGameCore(store, GameLifecycle.BuildQuickstartRequest(paths, GameLifecycle.BuildCatalog(store), 12345));
        Assert.That(created, Is.Not.Null);
        var game = created!.Value.game;
        game.Settings.EnforceSingleThread = true;
        return (game, created.Value.faction);
    }

    static (Entity yard, string lineId) FindYard(Game game, Entity faction)
    {
        foreach (var system in game.Systems)
        {
            foreach (var entity in system.GetAllEntitiesWithDataBlob<IndustryAbilityDB>())
            {
                if (entity.FactionOwnerID != faction.Id || !entity.HasDataBlob<LocalConstructionDB>())
                    continue;
                foreach (var (id, line) in entity.GetDataBlob<IndustryAbilityDB>().ProductionLines)
                {
                    if (IndustryTools.IsShipyardLine(line))
                        return (entity, id);
                }
            }
        }
        Assert.Fail("no player shipyard");
        throw new InvalidOperationException();
    }

    /// <summary>Two ship designs that fit the slip and differ by parts the yard's warehouse can hold.</summary>
    static (ShipDesign from, ShipDesign to) PickPair(Entity yard, FactionInfoDB info, IndustryAbilityDB.ProductionLine line)
    {
        var stores = yard.GetDataBlob<CargoStorageDB>().TypeStores.Keys.ToHashSet();
        var designs = info.ShipDesigns.Values.Where(d => d.MassPerUnit <= line.MaxVolume).ToList();
        foreach (var from in designs)
        {
            foreach (var to in designs)
            {
                if (from.UniqueID == to.UniqueID)
                    continue;
                var a = from.Components.GroupBy(c => c.Item1.UniqueID).ToDictionary(g => g.Key, g => g.Sum(c => c.Item2));
                var b = to.Components.GroupBy(c => c.Item1.UniqueID).ToDictionary(g => g.Key, g => g.Sum(c => c.Item2));
                if (a.Count == b.Count && a.All(p => b.GetValueOrDefault(p.Key) == p.Value))
                    continue;
                var moved = from.Components.Concat(to.Components)
                    .Where(c => a.GetValueOrDefault(c.Item1.UniqueID) != b.GetValueOrDefault(c.Item1.UniqueID))
                    .Select(c => c.Item1);
                if (moved.All(p => ShipRefitJob.IsShipPart(p) && stores.Contains(p.CargoTypeID)))
                    return (from, to);
            }
        }
        Assert.Fail("no two designs the yard's warehouse can refit between");
        throw new InvalidOperationException();
    }

    static Entity NewShip(Entity yard, Entity faction, ShipDesign design)
    {
        var planet = yard.GetDataBlob<ColonyInfoDB>().PlanetEntity;
        var ship = ShipFactory.CreateShip(design, faction, planet, "Refit Subject");
        if (!ship.HasDataBlob<CargoStorageDB>())
            ship.SetDataBlob(new CargoStorageDB());
        ship.GetDataBlob<CargoStorageDB>().TransferRangeDv_mps = 1e12;
        yard.GetDataBlob<CargoStorageDB>().TransferRangeDv_mps = 1e12;
        return ship;
    }

    /// <summary>Parts to take off the ship and fresh parts (put in the warehouse) to mount, to turn it into <paramref name="to"/>.</summary>
    static (List<ComponentInstance> remove, List<ComponentInstance> mount) Diff(Entity yard, Entity ship, ShipDesign to)
    {
        var onShip = ship.GetDataBlob<ComponentInstancesDB>().AllComponents.Values.ToList();
        var remove = new List<ComponentInstance>();
        var mount = new List<ComponentInstance>();
        var wanted = new Dictionary<string, (ComponentDesign Design, int Count)>();
        foreach (var (part, count) in to.Components)
            wanted[part.UniqueID] = wanted.TryGetValue(part.UniqueID, out var have) ? (part, have.Count + count) : (part, count);
        foreach (var group in onShip.GroupBy(c => c.Design.UniqueID))
        {
            int need = wanted.TryGetValue(group.Key, out var w) ? w.Count : 0;
            remove.AddRange(group.Skip(need));
        }
        var storage = yard.GetDataBlob<CargoStorageDB>();
        foreach (var (id, (design, count)) in wanted)
        {
            int have = onShip.Count(c => c.Design.UniqueID == id);
            for (int i = have; i < count; i++)
            {
                var fresh = new ComponentInstance(design);
                Assert.That(storage.AddCargoByUnit(fresh, 1), Is.EqualTo(1),
                    $"{design.Name} ({design.CargoTypeID}, vol {design.VolumePerUnit}); yard stores {string.Join(",", storage.TypeStores.Select(t => t.Key + ":" + t.Value.FreeVolume))}");
                mount.Add(fresh);
            }
        }
        return (remove, mount);
    }

    static void StockResources(Entity yard, IndustryAbilityDB.ProductionLine line, FactionInfoDB info)
    {
        var storage = yard.GetDataBlob<CargoStorageDB>();
        foreach (var job in line.Jobs.Where(j => j.Refit == null))
        {
            foreach (var (id, amount) in job.ResourcesCosts)
            {
                var item = info.Data.CargoGoods.GetAny(id);
                Assert.That(item, Is.Not.Null, id);
                storage.AddCargoByUnit(item!, amount * 2);
            }
        }
    }
}
