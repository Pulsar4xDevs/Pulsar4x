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
using Pulsar4X.DataStructures;
using Pulsar4X.Energy;
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
/// The market seat gates the market's own pricing and the warehouse Max sale; the industry seat gates production
/// toward the warehouse Min. Stock rules are set and cleared by command and survive a save.
/// </summary>
[TestFixture]
[NonParallelizable]
public class MarketSeatStockRulesTests
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
        Assert.That(_game.LocalHumanFactionId, Is.EqualTo(_player.Id), "Earth is the human player's colony");
        Assert.That(_earth.HasDataBlob<LogiBaseDB>(), Is.True, "Earth has a logistics office");
        Assert.That(MarketSeat.HasHead(_earth), Is.False, "no market head at start");
    }

    FactionInfoDB Info => _player.GetDataBlob<FactionInfoDB>();
    ICargoable Good(string id) => Info.Data.CargoGoods.GetAny(id)!;
    ColonyMarketPolicyDB Policy => _earth.GetDataBlob<ColonyMarketPolicyDB>();

    void SetStock(string id, long units)
    {
        var db = _earth.GetDataBlob<CargoStorageDB>();
        var good = Good(id);
        long now = db.GetUnitsStored(good, false);
        if (now > units) db.RemoveCargoByUnit(good, now - units);
        else if (now < units) db.AddCargoByUnit(good, units - now);
        Assert.That(db.GetUnitsStored(good, false), Is.EqualTo(units), id);
    }

    void MarketPass() => AgentProcessor.AssignGoal(_earth, new Goal(GoalType.OfferStock));

    Entity Civilian(string name)
        => CommanderFactory.Create(_earth.Manager!, _player.Id, new CommanderDB(name, 1, CommanderTypes.Civilian));

    Entity SeatMarketHead()
    {
        var head = Civilian("Mara");
        Assert.That(MarketSeat.Seat(_earth, head, _earth.StarSysDateTime), Is.Null);
        Assert.That(MarketSeat.HasHead(_earth), Is.True);
        return head;
    }

    CommandResult Send(GameCommand command) => new CommandTranslator(_game).Translate(_player, _earth, command);

    /// <summary>A cargo good Earth can build on a line, and its design.</summary>
    (string id, IConstructableDesign design) BuildableGood()
    {
        var industry = _earth.GetDataBlob<IndustryAbilityDB>();
        foreach (var (id, design) in Info.IndustryDesigns.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            if (design == null || !Info.Data.CargoGoods.Contains(id) || string.IsNullOrEmpty(design.IndustryTypeID))
                continue;
            if (IndustryTools.FastestLineId(industry, design.IndustryTypeID) != null)
                return (id, design);
        }
        Assert.Fail("Earth builds no cargo good");
        throw new InvalidOperationException();
    }

    long PartJobsOrdered(string id) => _earth.GetDataBlob<IndustryAbilityDB>().ProductionLines.Values
        .SelectMany(l => l.Jobs).Where(j => j.Refit == null && j.ItemGuid == id).Sum(j => (long)j.NumberOrdered);

    [Test]
    public void TheMarket_PricesStockByItself_OnlyWithAMarketHead()
    {
        SetStock("iron", 5_000);
        Policy.Rows.Remove("iron");
        MarketBook.RemoveListing(_earth, "iron");

        MarketPass();
        Assert.That(Policy.Rows.ContainsKey("iron"), Is.False, "no head: stock is not priced");
        Assert.That(MarketBook.TryGet(_earth, "iron", out _), Is.False);

        var head = SeatMarketHead();
        MarketPass();
        Assert.That(Policy.Rows["iron"].AutoPriced, Is.True);
        Assert.That(Policy.Rows["iron"].Ask, Is.EqualTo(EnergyPricing.SeedAsk(_game, Info.Data, "iron")).And.GreaterThan(0));
        Assert.That(MarketBook.TryGet(_earth, "iron", out _), Is.True);

        Assert.That(MarketSeat.Unseat(_earth, _earth.StarSysDateTime), Is.Null);
        Assert.That(head.GetDataBlob<CommanderDB>().AssignedTo, Is.EqualTo(-1));
        MarketPass();
        Assert.That(Policy.Rows.ContainsKey("iron"), Is.False, "the head left: the market's own prices are withdrawn");
        Assert.That(MarketBook.TryGet(_earth, "iron", out _), Is.False);
    }

    [Test]
    public void WithoutAMarketHead_ThePlayersOwnPricesStayListed()
    {
        SetStock("iron", 5_000);
        Assert.That(Send(new SetMarketPolicyCommand(_earth.Id, "iron", 1_000, 1_000, false, 7m, 0m)).Accepted, Is.True);
        MarketPass();
        Assert.That(Policy.Rows["iron"].AutoPriced, Is.False);
        Assert.That(MarketBook.TryGet(_earth, "iron", out var listing), Is.True);
        Assert.That(listing.Ask, Is.EqualTo(7m));
    }

    [Test]
    public void Max_SellsTheSurplus_OnlyWithAMarketHead()
    {
        SetStock("iron", 1_000_000);
        Policy.Rows.Remove("iron");
        MarketBook.RemoveListing(_earth, "iron");
        Assert.That(StockRules.Set(_earth, "iron", null, 900_000), Is.Null);

        MarketPass();
        Assert.That(MarketBook.TryGet(_earth, "iron", out _), Is.False, "no head: nothing is sold");

        SeatMarketHead();
        MarketPass();
        Assert.That(MarketBook.TryGet(_earth, "iron", out var listing), Is.True);
        Assert.That(listing.SellQuantity, Is.EqualTo(100_000), "only the stock above Max");
        Assert.That(listing.Ask, Is.EqualTo(EnergyPricing.SeedAsk(_game, Info.Data, "iron")));
    }

    [Test]
    public void Min_IsNeverSold()
    {
        SeatMarketHead();
        SetStock("iron", 1_000_000);
        Assert.That(StockRules.Set(_earth, "iron", 950_000, null), Is.Null);
        MarketPass();
        Assert.That(MarketBook.TryGet(_earth, "iron", out var listing), Is.True);
        Assert.That(listing.SellQuantity, Is.LessThanOrEqualTo(50_000));
    }

    [Test]
    public void Min_QueuesProductionOnce_WithAnIndustryHead()
    {
        Assert.That(IndustrySeat.HasHead(_earth), Is.True, "Warp Tech Earth starts with an industry head");
        var (id, design) = BuildableGood();
        var storage = _earth.GetDataBlob<CargoStorageDB>();
        long stock = storage.GetUnitsStoredByDesign(Good(id), false);
        long queuedBefore = IndustryTools.QueuedOutput(_earth, design);
        long orderedBefore = PartJobsOrdered(id);
        long perBatch = IndustryTools.OutputPerItem(design);
        Assert.That(StockRules.Set(_earth, id, stock + queuedBefore + 10 * perBatch, null), Is.Null);

        Assert.That(StockRules.QueueTowardMin(_earth), Is.EqualTo(1));
        Assert.That(PartJobsOrdered(id) - orderedBefore, Is.EqualTo(10), "the gap, in batches");
        Assert.That(StockRules.QueueTowardMin(_earth), Is.EqualTo(0), "queued jobs count as supply: no second job");
        Assert.That(PartJobsOrdered(id) - orderedBefore, Is.EqualTo(10));
    }

    [Test]
    public void Min_QueuesNothing_WithoutAnIndustryHead()
    {
        Assert.That(IndustrySeat.Unseat(_earth, _earth.StarSysDateTime), Is.Null);
        var (id, design) = BuildableGood();
        long stock = _earth.GetDataBlob<CargoStorageDB>().GetUnitsStoredByDesign(Good(id), false);
        long orderedBefore = PartJobsOrdered(id);
        Assert.That(StockRules.Set(_earth, id, stock + IndustryTools.QueuedOutput(_earth, design) + 10, null), Is.Null);

        Assert.That(StockRules.QueueTowardMin(_earth), Is.EqualTo(0));
        new IndustryProcessor().ProcessEntity(_earth, 0);
        Assert.That(PartJobsOrdered(id), Is.LessThanOrEqualTo(orderedBefore), "no job queued");
    }

    [Test]
    public void TheDailyIndustryPass_QueuesTowardMin()
    {
        var (id, design) = BuildableGood();
        long stock = _earth.GetDataBlob<CargoStorageDB>().GetUnitsStoredByDesign(Good(id), false);
        long min = stock + IndustryTools.QueuedOutput(_earth, design) + 10;
        Assert.That(StockRules.Set(_earth, id, min, null), Is.Null);
        new IndustryProcessor().ProcessEntity(_earth, 0);
        long after = _earth.GetDataBlob<CargoStorageDB>().GetUnitsStoredByDesign(Good(id), false) + IndustryTools.QueuedOutput(_earth, design);
        Assert.That(after, Is.GreaterThanOrEqualTo(min), "stock plus queued output reaches the minimum");
    }

    [Test]
    public void StockRuleCommands_SetAndClear_AndShowInTheWarehouseView()
    {
        Assert.That(Send(new SetStockRuleCommand(_earth.Id, "iron", 10, 20)).Accepted, Is.True);
        Assert.That(Send(new SetStockRuleCommand(_earth.Id, "water", null, 50)).Accepted, Is.True);
        Assert.That(Send(new SetStockRuleCommand(_earth.Id, "iron", 30, 20)).Accepted, Is.False, "max below min");
        Assert.That(Send(new SetStockRuleCommand(_earth.Id, "iron", -1, null)).Accepted, Is.False, "negative");
        Assert.That(Send(new SetStockRuleCommand(_earth.Id, "no-such-item", 1, null)).Accepted, Is.False, "unknown item");

        var view = new GameProjector(_game).ProjectEntity(_earth, _player.Id).GetView<CargoStorageView>()!;
        var iron = view.StockRules.Single(r => r.CargoId == "iron");
        Assert.That((iron.Min, iron.Max), Is.EqualTo(((long?)10, (long?)20)));
        Assert.That(iron.Name, Is.EqualTo(Good("iron").Name));
        Assert.That(iron.Stock, Is.EqualTo(_earth.GetDataBlob<CargoStorageDB>().GetUnitsStored(Good("iron"), false)));
        Assert.That(view.StockRules.Single(r => r.CargoId == "water").Min, Is.Null);
        Assert.That(view.IndustrySeatFilled, Is.True);
        Assert.That(view.MarketSeatFilled, Is.False);
        Assert.That(view.Stores.SelectMany(s => s.Items).Any(i => i.CargoId == "iron"), Is.True, "items carry the id a rule takes");

        Assert.That(Send(new ClearStockRuleCommand(_earth.Id, "iron")).Accepted, Is.True);
        Assert.That(Send(new SetStockRuleCommand(_earth.Id, "water", null, null)).Accepted, Is.True, "both unset clears");
        Assert.That(StockRules.Get(_earth, "iron"), Is.Null);
        Assert.That(StockRules.Get(_earth, "water"), Is.Null);
        Assert.That(Send(new ClearStockRuleCommand(_earth.Id, "iron")).Accepted, Is.True, "clearing nothing is fine");
    }

    [Test]
    public void MarketHeadCommands_SeatAndUnseat_OnePersonOnePost()
    {
        var mara = Civilian("Mara");
        Assert.That(Send(new SeatMarketHeadCommand(_earth.Id, mara.Id)).Accepted, Is.True);
        var colony = new GameProjector(_game).ProjectEntity(_earth, _player.Id).GetView<ColonyView>()!;
        Assert.That((colony.MarketHeadId, colony.MarketHeadName), Is.EqualTo((mara.Id, "Mara")));

        Assert.That(IndustrySeat.TryGetHead(_earth, out var industryHead), Is.True);
        Assert.That(Send(new SeatMarketHeadCommand(_earth.Id, industryHead.Id)).Accepted, Is.False, "the industry head cannot also run the market");
        Assert.That(Send(new SeatIndustryHeadCommand(_earth.Id, mara.Id)).Accepted, Is.False, "the market head cannot also run industry");
        Assert.That(IndustrySeat.ChairRefusal(mara), Is.Not.Null, "nor take a city-hall chair");

        Assert.That(Send(new UnseatMarketHeadCommand(_earth.Id)).Accepted, Is.True);
        Assert.That(MarketSeat.HasHead(_earth), Is.False);
        Assert.That(mara.GetDataBlob<CommanderDB>().AssignedTo, Is.EqualTo(-1));
    }

    [Test]
    public void OrdersSetAndClearRules_AndSeatTheMarketHead()
    {
        var mara = Civilian("Mara");
        Assert.That(SetStockRuleOrder.Create(_earth, "iron", 5, 9).IsValidCommand(_game), Is.True);
        Assert.That(SetStockRuleOrder.Create(_earth, "iron", 9, 5).IsValidCommand(_game), Is.False);
        SetStockRuleOrder.Create(_earth, "iron", 5, 9).Execute(_earth.StarSysDateTime);
        Assert.That(StockRules.Get(_earth, "iron")!.Max, Is.EqualTo(9));
        SetStockRuleOrder.CreateClear(_earth, "iron").Execute(_earth.StarSysDateTime);
        Assert.That(StockRules.Get(_earth, "iron"), Is.Null);

        Pulsar4X.People.Orders.SeatMarketHeadOrder.Create(_earth, mara.Id).Execute(_earth.StarSysDateTime);
        Assert.That(MarketSeat.HeadId(_earth), Is.EqualTo(mara.Id));
        Pulsar4X.People.Orders.UnseatMarketHeadOrder.Create(_earth).Execute(_earth.StarSysDateTime);
        Assert.That(MarketSeat.HasHead(_earth), Is.False);
    }

    [Test]
    public void StockRules_MarketSeat_AndAutoPricedRows_SurviveSaveAndLoad()
    {
        var head = SeatMarketHead();
        Assert.That(StockRules.Set(_earth, "iron", 5, null), Is.Null);
        Assert.That(StockRules.Set(_earth, "water", null, 7), Is.Null);
        Assert.That(StockRules.Set(_earth, "methalox", 3, 8), Is.Null);
        SetStock("iron", 5_000);
        Policy.Rows.Remove("iron");
        MarketPass();
        Assert.That(Policy.Rows["iron"].AutoPriced, Is.True);

        string json = Game.Save(_game);
        int next = EntityIDGenerator.NextId;
        var loaded = Game.Load(json);
        EntityIDGenerator.NextId = Math.Max(next, EntityIDGenerator.NextId);

        var earth = loaded.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ColonyInfoDB>()).Single(c => c.Id == _earth.Id);
        var rules = earth.GetDataBlob<ColonyStockRulesDB>().Rules;
        Assert.That(rules.Keys, Is.EquivalentTo(new[] { "iron", "water", "methalox" }));
        Assert.That((rules["iron"].Min, rules["iron"].Max), Is.EqualTo(((long?)5, (long?)null)));
        Assert.That((rules["water"].Min, rules["water"].Max), Is.EqualTo(((long?)null, (long?)7)));
        Assert.That((rules["methalox"].Min, rules["methalox"].Max, rules["methalox"].CargoId), Is.EqualTo(((long?)3, (long?)8, "methalox")));
        Assert.That(MarketSeat.HeadId(earth), Is.EqualTo(head.Id));
        Assert.That(earth.GetDataBlob<ColonyMarketPolicyDB>().Rows["iron"].AutoPriced, Is.True);
    }
}
