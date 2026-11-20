using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.Engine.Orders;
using GameEngine.People;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Factions;
using Pulsar4X.Industry;
using Pulsar4X.Interfaces;
using Pulsar4X.Logistics;
using Pulsar4X.Names;
using Pulsar4X.People;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

/// <summary>
/// Clearing a warehouse rule withdraws its auto-priced market row and cancels the not-started jobs its Min queued.
/// Market stock of a part design counts loose stored instances, but never parts a refit has claimed.
/// </summary>
[TestFixture]
[NonParallelizable]
public class StockRuleClearAndPartStockTests
{
    const string WarpTechStart = "Pulsar4x.Scenario.WarpTechStart";

    Game _game = null!;
    Entity _player = null!;
    Entity _earth = null!;

    [SetUp]
    public void SetUp()
    {
        ModsState.RefreshModsList("Data");
        var paths = ModsState.AvailableMods.Where(m => m.Mod.ModName == "Pulsar4x").Select(m => m.Path).ToList();
        var store = GameLifecycle.LoadModStore(paths, ModsState.AvailableMods.Select(m => m.Path), WarpTechStart);
        var result = GameLifecycle.CreateGameCore(store, GameLifecycle.BuildQuickstartRequest(paths, GameLifecycle.BuildCatalog(store), 12345678));
        Assert.That(result, Is.Not.Null);
        (_game, _player) = (result!.Value.game, result.Value.faction);
        _game.Settings.EnforceSingleThread = true;
        _earth = _game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ColonyInfoDB>())
            .Single(c => c.FactionOwnerID == _player.Id && c.GetDataBlob<ColonyInfoDB>().PlanetEntity.GetDataBlob<NameDB>().DefaultName == "Earth");
    }

    FactionInfoDB Info => _player.GetDataBlob<FactionInfoDB>();
    CargoStorageDB Storage => _earth.GetDataBlob<CargoStorageDB>();
    IndustryAbilityDB Industry => _earth.GetDataBlob<IndustryAbilityDB>();
    ColonyMarketPolicyDB Policy => _earth.GetDataBlob<ColonyMarketPolicyDB>();
    ICargoable Good(string id) => Info.Data.CargoGoods.GetAny(id)!;

    void MarketPass() => AgentProcessor.AssignGoal(_earth, new Goal(GoalType.OfferStock));

    void SeatMarketHead()
    {
        var head = CommanderFactory.Create(_earth.Manager!, _player.Id, new CommanderDB("Mara", 1, CommanderTypes.Civilian));
        Assert.That(MarketSeat.Seat(_earth, head, _earth.StarSysDateTime), Is.Null);
    }

    CommandResult Send(GameCommand command) => new CommandTranslator(_game).Translate(_player, _earth, command);

    (string id, IConstructableDesign design) BuildableGood()
    {
        foreach (var (id, design) in Info.IndustryDesigns.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            if (design == null || !Info.Data.CargoGoods.Contains(id) || string.IsNullOrEmpty(design.IndustryTypeID))
                continue;
            if (IndustryTools.FastestLineId(Industry, design.IndustryTypeID) != null)
                return (id, design);
        }
        Assert.Fail("Earth builds no cargo good");
        throw new InvalidOperationException();
    }

    List<IndustryJob> Jobs(string id) => Industry.ProductionLines.Values.SelectMany(l => l.Jobs).Where(j => j.Refit == null && j.ItemGuid == id).ToList();

    /// <summary>Queues the Min job for a buildable good and returns it (the only not-started job tagged for that rule).</summary>
    (string id, IndustryJob job) QueueMinJob()
    {
        Assert.That(IndustrySeat.HasHead(_earth), Is.True);
        var (id, design) = BuildableGood();
        long min = Storage.GetUnitsStoredByDesign(Good(id), false) + IndustryTools.QueuedOutput(_earth, design) + 5 * IndustryTools.OutputPerItem(design);
        Assert.That(StockRules.Set(_earth, id, min, null), Is.Null);
        Assert.That(StockRules.QueueTowardMin(_earth), Is.EqualTo(1));
        var job = Jobs(id).Single(j => j.StockRuleCargoId == id && StockRules.NotStarted(j));
        return (id, job);
    }

    [Test]
    public void ClearingARule_WithdrawsItsAutoPricedRowAndListing()
    {
        SeatMarketHead();
        Storage.AddCargoByUnit(Good("iron"), 1_000_000);
        Policy.Rows.Remove("iron");
        MarketBook.RemoveListing(_earth, "iron");
        Assert.That(StockRules.Set(_earth, "iron", null, 10), Is.Null);
        MarketPass();
        Assert.That(Policy.Rows["iron"].AutoPriced, Is.True);
        Assert.That(MarketBook.TryGet(_earth, "iron", out _), Is.True);

        Assert.That(Send(new ClearStockRuleCommand(_earth.Id, "iron")).Accepted, Is.True);
        Assert.That(Policy.Rows.ContainsKey("iron"), Is.False);
        Assert.That(MarketBook.TryGet(_earth, "iron", out _), Is.False);
    }

    [Test]
    public void SetWithBothBoundsNull_OrDroppingMax_WithdrawsTheAutoRow_ButNotThePlayersRow()
    {
        SeatMarketHead();
        Storage.AddCargoByUnit(Good("iron"), 1_000_000);
        Policy.Rows.Remove("iron");
        Assert.That(StockRules.Set(_earth, "iron", null, 10), Is.Null);
        MarketPass();
        Assert.That(Policy.Rows["iron"].AutoPriced, Is.True);
        Assert.That(StockRules.Set(_earth, "iron", 5, null), Is.Null, "Max dropped, Min kept");
        Assert.That(Policy.Rows.ContainsKey("iron"), Is.False);
        Assert.That(MarketBook.TryGet(_earth, "iron", out _), Is.False);

        // A player-priced row is the player's: clearing the rule leaves it.
        Assert.That(Send(new SetMarketPolicyCommand(_earth.Id, "iron", 0, 1_000, false, 7m, 0m)).Accepted, Is.True);
        Assert.That(StockRules.Set(_earth, "iron", null, 10), Is.Null);
        Assert.That(Send(new SetStockRuleCommand(_earth.Id, "iron", null, null)).Accepted, Is.True);
        Assert.That(Policy.Rows["iron"].AutoPriced, Is.False);
        Assert.That(Policy.Rows["iron"].Ask, Is.EqualTo(7m));
    }

    [Test]
    public void ClearingARule_CancelsItsNotStartedMinJob_AndLeavesThePlayersJob()
    {
        var (id, job) = QueueMinJob();
        var line = Industry.ProductionLines.First(l => l.Value.Jobs.Contains(job)).Key;
        Assert.That(Send(new QueueIndustryJobCommand(_earth.Id, line, id, 2, false, false)).Accepted, Is.True);
        var playerJob = Jobs(id).Single(j => j.StockRuleCargoId == null && j.NumberOrdered == 2);

        Assert.That(StockRules.Clear(_earth, id), Is.EqualTo(1));
        Assert.That(Jobs(id), Does.Not.Contain(job));
        Assert.That(Jobs(id), Does.Contain(playerJob));
    }

    [Test]
    public void ClearingARule_LeavesAStartedOrEditedMinJob()
    {
        var (id, job) = QueueMinJob();
        job.ProductionPointsLeft = job.ProductionPointsCost - 1; // work spent
        Assert.That(StockRules.Clear(_earth, id), Is.EqualTo(0));
        Assert.That(Jobs(id), Does.Contain(job));

        var (id2, job2) = QueueMinJob();
        var line = Industry.ProductionLines.First(l => l.Value.Jobs.Contains(job2)).Key;
        Assert.That(Send(new EditIndustryJobCommand(_earth.Id, line, job2.JobID, 3, false)).Accepted, Is.True);
        Assert.That(job2.StockRuleCargoId, Is.Null, "the player edited it: it is theirs now");
        Assert.That(StockRules.Clear(_earth, id2), Is.EqualTo(0));
        Assert.That(Jobs(id2), Does.Contain(job2));
    }

    [Test]
    public void TheMinJobTag_SurvivesSaveAndLoad_AndClearStillCancelsIt()
    {
        var (id, job) = QueueMinJob();
        string json = Game.Save(_game);
        int next = EntityIDGenerator.NextId;
        var loaded = Game.Load(json);
        EntityIDGenerator.NextId = Math.Max(next, EntityIDGenerator.NextId);
        var earth = loaded.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ColonyInfoDB>()).Single(c => c.Id == _earth.Id);
        var jobs = earth.GetDataBlob<IndustryAbilityDB>().ProductionLines.Values.SelectMany(l => l.Jobs);
        Assert.That(jobs.Single(j => j.JobID == job.JobID).StockRuleCargoId, Is.EqualTo(id));

        Assert.That(StockRules.Clear(earth, id), Is.EqualTo(1));
        Assert.That(earth.GetDataBlob<IndustryAbilityDB>().ProductionLines.Values.SelectMany(l => l.Jobs).Any(j => j.JobID == job.JobID), Is.False);
    }

    /// <summary>A part design Earth's warehouse can hold, with two stacked units and two loose instances, one claimed by a refit.</summary>
    (ComponentDesign design, ComponentInstance free, ComponentInstance claimed) PartsInStock()
    {
        var design = Info.ComponentDesigns.Values.OrderBy(d => d.UniqueID, StringComparer.Ordinal)
            .First(d => Info.Data.CargoGoods.Contains(d.UniqueID) && Storage.TypeStores.ContainsKey(d.CargoTypeID));
        Storage.RemoveUnitsByDesign(design, long.MaxValue);
        Assert.That(Storage.AddCargoByUnitUnbounded(design) + Storage.AddCargoByUnitUnbounded(design), Is.EqualTo(2));
        var free = new ComponentInstance(design);
        var claimed = new ComponentInstance(design);
        Assert.That(Storage.AddCargoByUnitUnbounded(free), Is.EqualTo(1));
        Assert.That(Storage.AddCargoByUnitUnbounded(claimed), Is.EqualTo(1));
        var refit = new ShipRefitJob { ShipEntityId = -1 };
        refit.Mount.Add(claimed);
        Industry.ProductionLines.Values.First().Jobs.Add(IndustryJob.ForRefit(refit, "Test refit"));
        Assert.That(ShipRefitJob.Reserved(_earth, claimed.ID), Is.True);
        Assert.That(Storage.GetUnitsStoredByDesign(design, false), Is.EqualTo(4));
        return (design, free, claimed);
    }

    [Test]
    public void PartStock_CountsLooseInstances_ButNotRefitClaimedOnes()
    {
        var (design, _, _) = PartsInStock();
        Assert.That(MarketBook.Stock(_earth, design), Is.EqualTo(3), "2 stacked + 1 loose; the claimed one is not stock");

        // The market lists what it counts: with Max 0 the whole sellable stock is offered.
        SeatMarketHead();
        Assert.That(StockRules.Set(_earth, design.UniqueID, null, 0), Is.Null);
        MarketPass();
        Assert.That(MarketBook.TryGet(_earth, design.UniqueID, out var listing), Is.True);
        Assert.That(listing.SellQuantity, Is.EqualTo(3));
    }

    [Test]
    public void TakingPartStock_UsesTheStackThenLooseInstances_AndNeverTheClaimedPart()
    {
        var (design, free, claimed) = PartsInStock();
        Assert.That(MarketBook.Take(_earth, Storage, design, 10), Is.EqualTo(3));
        Assert.That(Storage.GetUnitsStored(design, false), Is.EqualTo(0));
        Assert.That(Storage.GetUnitsStored(free, false), Is.EqualTo(0));
        Assert.That(Storage.GetUnitsStored(claimed, false), Is.EqualTo(1), "the refit keeps its part");
        Assert.That(MarketBook.Stock(_earth, design), Is.EqualTo(0));
    }
}
