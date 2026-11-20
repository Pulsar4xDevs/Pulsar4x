using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.People;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.DataStructures;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Factions;
using Pulsar4X.Industry;
using Pulsar4X.People;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

/// <summary>
/// A refit waiting for parts picks up parts that are stored (now or later), never holds the same stored part twice,
/// and with an industry head seated the missing parts are queued for production once.
/// </summary>
[TestFixture]
[NonParallelizable]
public class RefitPartsTests
{
    const string Scenario = "Pulsar4x.Scenario.WarpTechStart";

    [Test]
    public void TwoOfTheSamePart_ClaimTwoDistinctStoredParts_AndComplete()
    {
        var s = Scene(seatHead: false);
        var a = Store(s, s.Part);
        var b = Store(s, s.Part);

        var job = QueueRefit(s, s.Part.UniqueID, s.Part.UniqueID);

        Assert.That(job.Refit!.Mount.Select(p => p.ID), Is.EquivalentTo(new[] { a.ID, b.ID }), "one claim per stored part");
        RunDays(s.Yard, 60, () => ShipRefitJob.JobsOf(s.Yard).Any());
        Assert.That(ShipRefitJob.JobsOf(s.Yard), Is.Empty, "the refit completed");
        Assert.That(PartsOnShip(s), Is.EqualTo(s.PartsBefore + 2));
    }

    [Test]
    public void ARefitHoldingOnePartTwice_ReleasesTheDuplicate_WaitsForIt_ThenCompletesWhenAnotherIsStored()
    {
        var s = Scene(seatHead: false);
        var a = Store(s, s.Part);
        var job = QueueRefit(s, s.Part.UniqueID, s.Part.UniqueID);
        // Older games claimed the same stored part for both slots and the refit waited for ever ("Waiting for parts").
        job.Refit!.Mount.Add(a);
        job.Refit.MountDesignIds.Clear();

        RunDays(s.Yard, 3, () => true);
        Assert.That(job.Status, Is.EqualTo(Pulsar4X.DataStructures.IndustryJobStatus.MissingResources));
        Assert.That(job.WaitReason, Is.EqualTo("Waiting for " + s.Part.Name), "the duplicate went back to wanted");
        Assert.That(job.Refit.Mount.Count(p => p.ID == a.ID), Is.EqualTo(1));

        Store(s, s.Part); // the second part arrives later
        RunDays(s.Yard, 60, () => ShipRefitJob.JobsOf(s.Yard).Any());
        Assert.That(ShipRefitJob.JobsOf(s.Yard), Is.Empty, "the refit picked up the arriving part and completed");
        Assert.That(PartsOnShip(s), Is.EqualTo(s.PartsBefore + 2));
    }

    [Test]
    public void AClaimedPartThatLeftTheWarehouse_IsWantedAgain_AndAnotherStoredPartIsUsed()
    {
        var s = Scene(seatHead: false);
        var a = Store(s, s.Part);
        var job = QueueRefit(s, s.Part.UniqueID);
        Assert.That(job.Refit!.Mount.Single().ID, Is.EqualTo(a.ID));
        Assert.That(s.Storage.RemoveCargoByUnit(a, 1), Is.EqualTo(1)); // used or moved elsewhere
        Store(s, s.Part);

        RunDays(s.Yard, 60, () => ShipRefitJob.JobsOf(s.Yard).Any());
        Assert.That(ShipRefitJob.JobsOf(s.Yard), Is.Empty);
        Assert.That(PartsOnShip(s), Is.EqualTo(s.PartsBefore + 1));
    }

    [Test]
    public void WithoutAHead_MissingPartsAreNotQueued()
    {
        var s = Scene(seatHead: false);
        var job = QueueRefit(s, s.Part.UniqueID);
        RunDays(s.Yard, 3, () => true);
        Assert.That(job.WaitReason, Is.EqualTo("Waiting for " + s.Part.Name));
        Assert.That(PartJobs(s), Is.Empty);
    }

    [Test]
    public void WithAHead_MissingPartsAreQueuedOnce_AndTheRefitCompletes()
    {
        var s = Scene(seatHead: true);
        QueueRefit(s, s.Part.UniqueID, s.Part.UniqueID);

        var queued = PartJobs(s);
        Assert.That(queued, Has.Count.EqualTo(1), "queued with the refit");
        Assert.That(queued[0].NumberOrdered * IndustryTools.OutputPerItem(s.Part), Is.GreaterThanOrEqualTo(2));
        Assert.That(IndustryTools.LineAccepts(s.Industry.ProductionLines.Single(l => l.Value.Jobs.Contains(queued[0])).Value, s.Part), Is.True);

        for (int day = 0; day < 365 && ShipRefitJob.JobsOf(s.Yard).Any(); day++)
        {
            StockInputs(s);
            new IndustryProcessor().ProcessEntity(s.Yard, 0);
            Assert.That(PartJobs(s).Sum(j => j.NumberOrdered - j.NumberCompleted) * IndustryTools.OutputPerItem(s.Part),
                Is.LessThanOrEqualTo(2), $"day {day}: never queued twice");
        }
        Assert.That(ShipRefitJob.JobsOf(s.Yard), Is.Empty, "the parts were built, claimed and mounted");
        Assert.That(PartsOnShip(s), Is.EqualTo(s.PartsBefore + 2));
    }

    [Test]
    public void WithAHead_PartsAlreadyQueuedByHand_AreNotQueuedAgain()
    {
        var s = Scene(seatHead: true);
        var maker = s.Industry.ProductionLines.First(l => !IndustryTools.IsShipyardLine(l.Value) && IndustryTools.LineAccepts(l.Value, s.Part)).Key;
        var byHand = new CommandTranslator(s.Game).Translate(s.Faction, s.Yard,
            new QueueIndustryJobCommand(s.Yard.Id, maker, s.Part.UniqueID, 1, false, false));
        Assert.That(byHand.Accepted, Is.True, byHand.RejectionReason);

        QueueRefit(s, s.Part.UniqueID);
        Assert.That(PartJobs(s).Sum(j => j.NumberOrdered), Is.EqualTo(1), "the hand-queued job covers the refit");

        for (int day = 0; day < 365 && ShipRefitJob.JobsOf(s.Yard).Any(); day++)
        {
            StockInputs(s);
            new IndustryProcessor().ProcessEntity(s.Yard, 0);
            Assert.That(PartJobs(s).Sum(j => j.NumberOrdered - j.NumberCompleted), Is.LessThanOrEqualTo(1), $"day {day}");
        }
        Assert.That(ShipRefitJob.JobsOf(s.Yard), Is.Empty);
        Assert.That(PartsOnShip(s), Is.EqualTo(s.PartsBefore + 1));
    }

    [Test]
    public void AGameWithAQueuedRefit_SavesAndLoads_WithTheRefitsParts()
    {
        var s = Scene(seatHead: false);
        var removed = s.Ship.GetDataBlob<ComponentInstancesDB>().AllComponents.Values.First(c => ShipRefitJob.IsShipPart(c.Design));
        var result = new CommandTranslator(s.Game).Translate(s.Faction, s.Yard, new QueueShipRefitCommand(
            s.Yard.Id, s.LineId, s.Ship.Id, new[] { removed.ID }, Array.Empty<int>(), null, new[] { s.Part.UniqueID }));
        Assert.That(result.Accepted, Is.True, result.RejectionReason);

        string json = Game.Save(s.Game);
        int next = EntityIDGenerator.NextId;
        // The faction's shipyard design used to carry the live yard line (and this refit) and the load failed.
        Assert.That(json, Does.Not.Contain("_productionLine"));
        Game? loaded = null;
        Assert.DoesNotThrow(() => loaded = Game.Load(json));
        EntityIDGenerator.NextId = Math.Max(next, EntityIDGenerator.NextId);

        var yard = loaded!.Systems.SelectMany(sys => sys.GetAllEntitiesWithDataBlob<IndustryAbilityDB>()).Single(e => e.Id == s.Yard.Id);
        var refit = ShipRefitJob.JobsOf(yard).Single().Item2.Refit!;
        Assert.That(refit.Remove.Single().ID, Is.EqualTo(removed.ID));
        Assert.That(refit.Remove.Single().Design.UniqueID, Is.EqualTo(removed.Design.UniqueID));
        Assert.That(refit.MountDesignIds, Is.EqualTo(new[] { s.Part.UniqueID }));
    }

    sealed record RefitScene(Game Game, Entity Faction, Entity Yard, string LineId, IndustryAbilityDB Industry,
        CargoStorageDB Storage, ComponentDesign Part, Entity Ship, int PartsBefore);

    static RefitScene Scene(bool seatHead)
    {
        ModsState.RefreshModsList("Data");
        var mods = ModsState.AvailableMods;
        var paths = new[] { mods.Single(m => m.Mod.ModName == "Pulsar4x").Path }.ToList();
        var store = GameLifecycle.LoadModStore(paths, mods.Select(m => m.Path), Scenario);
        var created = GameLifecycle.CreateGameCore(store, GameLifecycle.BuildQuickstartRequest(paths, GameLifecycle.BuildCatalog(store), 12345));
        Assert.That(created, Is.Not.Null);
        var (game, faction) = (created!.Value.game, created.Value.faction);
        game.Settings.EnforceSingleThread = true;
        var info = faction.GetDataBlob<FactionInfoDB>();

        Entity? yard = null;
        string? lineId = null;
        foreach (var system in game.Systems)
            foreach (var entity in system.GetAllEntitiesWithDataBlob<IndustryAbilityDB>())
                if (yard == null && entity.FactionOwnerID == faction.Id && entity.HasDataBlob<LocalConstructionDB>())
                    foreach (var (id, line) in entity.GetDataBlob<IndustryAbilityDB>().ProductionLines)
                        if (lineId == null && IndustryTools.IsShipyardLine(line))
                            (yard, lineId) = (entity, id);
        Assert.That(yard, Is.Not.Null, "no player shipyard");
        var industry = yard!.GetDataBlob<IndustryAbilityDB>();
        var part = info.ComponentDesigns.Values.First(d => d.Name.StartsWith("Passenger Quarters")
            && d.ComponentMountType.HasFlag(Pulsar4X.DataStructures.ComponentMountType.ShipComponent));
        Assert.That(industry.ProductionLines.Values.Any(l => !IndustryTools.IsShipyardLine(l) && IndustryTools.LineAccepts(l, part)), Is.True,
            "a factory line builds the part");

        var slip = industry.ProductionLines[lineId!];
        var design = info.ShipDesigns.Values.First(d => d.MassPerUnit <= slip.MaxVolume);
        var ship = ShipFactory.CreateShip(design, faction, yard.GetDataBlob<ColonyInfoDB>().PlanetEntity, "Refit Subject");
        if (!ship.HasDataBlob<CargoStorageDB>())
            ship.SetDataBlob(new CargoStorageDB());
        ship.GetDataBlob<CargoStorageDB>().TransferRangeDv_mps = 1e12;
        var storage = yard.GetDataBlob<CargoStorageDB>();
        storage.TransferRangeDv_mps = 1e12;

        // Only the jobs these tests queue run.
        foreach (var line in industry.ProductionLines.Values)
            line.Jobs.Clear();

        if (seatHead && !IndustrySeat.HasHead(yard))
        {
            var person = CommanderFactory.Create(yard.Manager, faction.Id, new CommanderDB("Lee", 1, CommanderTypes.Civilian));
            Assert.That(IndustrySeat.Seat(yard, person, yard.StarSysDateTime), Is.Null);
        }
        if (!seatHead && IndustrySeat.HasHead(yard))
            IndustrySeat.Unseat(yard, yard.StarSysDateTime);
        Assert.That(IndustrySeat.HasHead(yard), Is.EqualTo(seatHead));

        var scene = new RefitScene(game, faction, yard, lineId!, industry, storage, part, ship, 0);
        return scene with { PartsBefore = PartsOnShip(scene) };
    }

    static ComponentInstance Store(RefitScene s, ComponentDesign design)
    {
        var part = new ComponentInstance(design);
        Assert.That(s.Storage.AddCargoByUnit(part, 1), Is.EqualTo(1));
        return part;
    }

    static IndustryJob QueueRefit(RefitScene s, params string[] mountDesignIds)
    {
        var result = new CommandTranslator(s.Game).Translate(s.Faction, s.Yard, new QueueShipRefitCommand(
            s.Yard.Id, s.LineId, s.Ship.Id, Array.Empty<int>(), Array.Empty<int>(), null, mountDesignIds));
        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        return s.Industry.ProductionLines[s.LineId].Jobs.Single(j => j.Refit != null);
    }

    static List<IndustryJob> PartJobs(RefitScene s)
        => s.Industry.ProductionLines.Values.SelectMany(l => l.Jobs)
            .Where(j => j.Refit == null && j.ItemGuid == s.Part.UniqueID).ToList();

    static int PartsOnShip(RefitScene s)
        => s.Ship.GetDataBlob<ComponentInstancesDB>().AllComponents.Values.Count(c => c.Design.UniqueID == s.Part.UniqueID);

    static void StockInputs(RefitScene s)
    {
        var info = s.Faction.GetDataBlob<FactionInfoDB>();
        foreach (var job in s.Industry.ProductionLines.Values.SelectMany(l => l.Jobs).Where(j => j.Refit == null).ToList())
            foreach (var (id, amount) in job.ResourcesCosts)
            {
                var item = info.Data.CargoGoods.GetAny(id);
                if (item != null && s.Storage.GetUnitsStored(item, false) < amount)
                    s.Storage.AddCargoByUnit(item, amount);
            }
    }

    static void RunDays(Entity yard, int max, Func<bool> keepGoing)
    {
        for (int day = 0; day < max && keepGoing(); day++)
            new IndustryProcessor().ProcessEntity(yard, 0);
    }
}
