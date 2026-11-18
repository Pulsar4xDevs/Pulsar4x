using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Colonies;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Industry;
using Pulsar4X.Logistics;
using Pulsar4X.Modding;
using Pulsar4X.Names;
using Pulsar4X.Energy;
using Pulsar4X.Storage;
using GameEngine.Engine.Orders;

namespace Pulsar4X.Tests;

/// <summary>
/// Multi-faction Earth phase 4: bloc markets that buy. Data-driven demand rows consume, bid 0.75 x seed ask
/// with a shortage premium, spend only above the reserve, and clear same-body sell listings in the market pass.
/// </summary>
[TestFixture]
public class MultiFactionEarthPhase4Tests
{
    const string WarpTechStart = "Pulsar4x.Scenario.WarpTechStart";

    Game _game = null!;
    Entity _player = null!;
    Entity _earthBody = null!;
    Entity _company = null!;

    [SetUp]
    public void SetUp()
    {
        ModsState.RefreshModsList("Data");
        var paths = ModsState.AvailableMods.Where(m => m.Mod.ModName == "Pulsar4x").Select(m => m.Path).ToList();
        var store = GameLifecycle.LoadModStore(paths, ModsState.AvailableMods.Select(m => m.Path), WarpTechStart);
        var request = GameLifecycle.BuildQuickstartRequest(paths, GameLifecycle.BuildCatalog(store), 12345678);
        var result = GameLifecycle.CreateGameCore(store, request);
        Assert.That(result, Is.Not.Null);
        (_game, _player) = (result!.Value.game, result.Value.faction);
        _game.Settings.EnforceSingleThread = true;
        _company = Colonies().Single(c => c.FactionOwnerID == _player.Id && BodyName(c) == "Earth");
        _earthBody = _company.GetDataBlob<ColonyInfoDB>().PlanetEntity;
    }

    List<Entity> Colonies() => _game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ColonyInfoDB>()).ToList();
    static string BodyName(Entity c) => c.GetDataBlob<ColonyInfoDB>().PlanetEntity.GetDataBlob<NameDB>().DefaultName;
    Entity Named(string name) => Colonies().Single(c => c.GetDataBlob<NameDB>().DefaultName == name);
    static FactionInfoDB Info(Entity colony) => colony.GetFactionOwner.GetDataBlob<FactionInfoDB>();
    static ICargoable Iron(Entity colony) => Info(colony).Data.CargoGoods["iron"];

    static long Stock(Entity colony, int mineralId) =>
        colony.GetDataBlob<CargoStorageDB>().TypeStores.Values
            .Sum(s => s.CurrentStoreInUnits.TryGetValue(mineralId, out var v) ? v : 0);

    static ICargoable Good(Entity colony, string id) => Info(colony).Data.CargoGoods.GetAny(id)!;
    decimal Ask(Entity colony, string id) => EnergyPricing.SeedAsk(_game, Info(colony).Data, id);
    static MarketPolicyRow Row(Entity colony, string id) => colony.GetDataBlob<ColonyMarketPolicyDB>().Rows[id];
    static long Units(Entity colony, string id) => MarketBook.Stock(colony, Good(colony, id));

    static void SetStock(Entity colony, string id, long units)
    {
        var db = colony.GetDataBlob<CargoStorageDB>();
        var good = Good(colony, id);
        long now = db.GetUnitsStored(good, false);
        if (now > units) db.RemoveCargoByUnit(good, now - units);
        else if (now < units) db.AddCargoByUnit(good, units - now);
        Assert.That(db.GetUnitsStored(good, false), Is.EqualTo(units), id);
    }

    static void SetFunds(Entity colony, decimal funds)
    {
        var money = Info(colony).Money;
        decimal delta = funds - money.GetCurrentFunds();
        if (delta > 0) money.AddIncome(DateTime.Now, TransactionCategory.InitialInvestment, "test", delta);
        else if (delta < 0) money.AddExpense(DateTime.Now, TransactionCategory.InitialInvestment, "test", -delta);
        Assert.That(money.GetCurrentFunds(), Is.EqualTo(funds));
    }

    /// <summary>Runs the colony's OfferStock market pass now and applies its listings (the agent path).</summary>
    static void Plan(Entity colony, DateTime at)
    {
        Assert.That(at, Is.EqualTo(colony.StarSysDateTime), "the agent pass runs at the colony's date");
        AgentProcessor.AssignGoal(colony, new Goal(GoalType.OfferStock));
    }

    [Test]
    public void BlocKits_AndDemandRows_AreSeededFromData()
    {
        var amc = Named("Americas Compact");
        var spc = Named("Sino-Pacific Coalition");
        var ecn = Named("Euro Consortium");
        foreach (var bloc in new[] { amc, spc, ecn })
            Assert.That(bloc.GetDataBlob<LogiBaseDB>().Capacity, Is.EqualTo(15), "three trade offices");

        var iron = Row(spc, "iron");
        Assert.That(iron.UsePerDay, Is.EqualTo(5000));
        Assert.That(iron.Min, Is.EqualTo(150_000), "30 hold days x 5,000/day");
        Assert.That(iron.BidFactor, Is.EqualTo(0.75));
        Assert.That(iron.Ask, Is.EqualTo(Ask(spc, "iron")));
        Assert.That(iron.Bid, Is.EqualTo(Math.Round(Ask(spc, "iron") * 0.75m, 2, MidpointRounding.AwayFromZero)));
        var steel = Row(spc, "stainless-steel");
        Assert.That((steel.BidFactor, steel.Min, steel.Bid, steel.Ask), Is.EqualTo((0.0, 0L, 0m, Ask(spc, "stainless-steel"))), "a Sell row");

        Assert.That(Row(amc, "methalox").UsePerDay, Is.EqualTo(2500));
        Assert.That(amc.GetDataBlob<ColonyMarketPolicyDB>().Rows.ContainsKey("electronics"), Is.False, "not unlocked at start");
        Assert.That(Row(ecn, "titanium").UsePerDay, Is.EqualTo(333));
        Assert.That(Units(amc, "water"), Is.EqualTo(100_000));
        Assert.That(Units(spc, "plastic"), Is.EqualTo(5_000));
    }

    [Test]
    public void NewOfferStockRow_AsksTheSeedAsk()
    {
        SetStock(_company, "iron", 5_000);
        _company.GetDataBlob<ColonyMarketPolicyDB>().Rows.Remove("iron");
        Plan(_company, _company.StarSysDateTime);
        Assert.That(Row(_company, "iron").Ask, Is.EqualTo(Ask(_company, "iron")).And.GreaterThan(0));
        Assert.That(MarketBook.TryGet(_company, "iron", out var listing), Is.True);
        Assert.That(listing.Ask, Is.EqualTo(Ask(_company, "iron")));
    }

    [Test]
    public void DemandRow_DrainsStock_AndPostsABuyAtThreeQuartersOfTheAsk()
    {
        var spc = Named("Sino-Pacific Coalition");
        var row = Row(spc, "iron");
        var t0 = spc.StarSysDateTime;
        SetStock(spc, "iron", 100_000);
        row.LastConsumption(spc, t0.AddDays(-4));

        Plan(spc, t0);
        Assert.That(Units(spc, "iron"), Is.EqualTo(80_000), "4 days x 5,000/day");
        Assert.That(row.ShortSince, Is.EqualTo(t0));
        Assert.That(MarketBook.TryGet(spc, "iron", out var listing), Is.True);
        decimal bid = Math.Round(Ask(spc, "iron") * 0.75m, 2, MidpointRounding.AwayFromZero);
        Assert.That(listing.Bid, Is.EqualTo(bid));
        Assert.That(listing.BuyQuantity, Is.EqualTo(150_000 - 80_000));
        Assert.That(listing.SellQuantity, Is.EqualTo(0));
    }

    [Test]
    public void Bid_ClimbsWhileShort_CapsAtDouble_AndResetsWhenFilled()
    {
        var spc = Named("Sino-Pacific Coalition");
        var row = Row(spc, "iron");
        var t0 = spc.StarSysDateTime;
        decimal ask = Ask(spc, "iron");
        decimal At(double premium) => Math.Round(ask * (decimal)(0.75 * premium), 2, MidpointRounding.AwayFromZero);
        SetStock(spc, "iron", 10_000);
        row.LastConsumption(spc, t0);

        BlocDemand.Run(spc, t0);
        Assert.That(row.Bid, Is.EqualTo(At(1)));
        BlocDemand.Run(spc, t0.AddDays(45));
        Assert.That(row.Bid, Is.EqualTo(At(1.5)));
        BlocDemand.Run(spc, t0.AddDays(200));
        Assert.That(row.Bid, Is.EqualTo(At(2)), "premium caps at 90 days");

        SetStock(spc, "iron", 1_000_000);
        BlocDemand.Run(spc, t0.AddDays(201));
        Assert.That(row.ShortSince, Is.Null);
        Assert.That(row.Bid, Is.EqualTo(At(1)));
    }

    [Test]
    public void CompanySellsIronToSpcOnEarth_AndIsPaidTheSameTick()
    {
        var spc = Named("Sino-Pacific Coalition");
        var t0 = spc.StarSysDateTime;
        // The company lists 5,000 iron below the bloc's base bid; SPC is short.
        SetStock(_company, "iron", 5_000);
        Plan(_company, t0);
        Assert.That(MarketBook.SetListing(_company, new MarketListing { CargoId = "iron", SellQuantity = 5_000, Ask = 1m }), Is.True);
        SetStock(spc, "iron", 0);
        var row = Row(spc, "iron");
        row.LastConsumption(spc, t0);
        decimal companyFunds = Info(_company).Money.GetCurrentFunds(), spcFunds = Info(spc).Money.GetCurrentFunds();
        var events = new List<Pulsar4X.Events.Event>();
        Action<Pulsar4X.Events.Event> onTrade = events.Add;
        Pulsar4X.Events.EventManager.Instance.Subscribe(Pulsar4X.Events.EventType.MarketTradeSettled, onTrade);
        try { Plan(spc, t0); }
        finally { Pulsar4X.Events.EventManager.Instance.Unsubscribe(Pulsar4X.Events.EventType.MarketTradeSettled, onTrade); }

        // Listing is consent, but every auto-cleared sale shows in the player's ledger and event log.
        var sale = events.Single(e => e.ConcernedFactions.Contains(_player.Id));
        Assert.That(sale.ConcernedFactions, Does.Contain(spc.FactionOwnerID));
        Assert.That(sale.Message, Does.Contain("5,000 iron"));
        Assert.That(Info(_company).Money.GetTransactionsByCategory(TransactionCategory.Trade)
            .Count(t => t.Amount == 5_000m && t.Description.StartsWith("same-body iron")), Is.EqualTo(1));

        Assert.That(Units(_company, "iron"), Is.EqualTo(0));
        Assert.That(Units(spc, "iron"), Is.EqualTo(5_000));
        Assert.That(Info(_company).Money.GetCurrentFunds(), Is.EqualTo(companyFunds + 5_000m), "paid the ask, fee 0");
        Assert.That(Info(spc).Money.GetCurrentFunds(), Is.EqualTo(spcFunds - 5_000m));
        Assert.That(MarketBook.TryGet(spc, "iron", out var listing), Is.True);
        Assert.That(listing.BuyQuantity, Is.EqualTo(150_000 - 5_000), "the rest stays bid");
    }

    [Test]
    public void ClearingSkipsAsksAboveTheBid()
    {
        var spc = Named("Sino-Pacific Coalition");
        var t0 = spc.StarSysDateTime;
        SetStock(_company, "iron", 5_000);
        Plan(_company, t0);
        Assert.That(MarketBook.SetListing(_company, new MarketListing { CargoId = "iron", SellQuantity = 5_000, Ask = 100m }), Is.True);
        SetStock(spc, "iron", 0);
        Row(spc, "iron").LastConsumption(spc, t0);

        Plan(spc, t0);
        Assert.That(Units(_company, "iron"), Is.EqualTo(5_000));
    }

    [Test]
    public void Bids_OnlySpendFundsAboveTheReserve()
    {
        var spc = Named("Sino-Pacific Coalition");
        var t0 = spc.StarSysDateTime;
        // Multi-faction phase 6: the reserve is three months of the bloc's running costs (was a flat 10M).
        decimal reserve = BlocDemand.ReserveFor(spc);
        Assert.That(reserve, Is.EqualTo(3 * FactionSpending.MonthlyRunningCosts(_game, spc.FactionOwnerID)).And.GreaterThan(0));
        SetStock(spc, "iron", 0);
        Row(spc, "iron").LastConsumption(spc, t0);
        SetFunds(spc, reserve + 1_000m);

        Plan(spc, t0);
        Assert.That(MarketBook.TryGet(spc, "iron", out var listing), Is.True);
        Assert.That(listing.BuyQuantity, Is.EqualTo((long)Math.Floor(1_000m / listing.Bid)));

        SetFunds(spc, reserve - 1m);
        Plan(spc, t0);
        Assert.That(!MarketBook.TryGet(spc, "iron", out listing) || listing.BuyQuantity == 0, Is.True, "nothing above the reserve");
    }

    // ---------------------------------------------------------------- Earth mining horizon

    /// <summary>
    /// Earth's mined minerals at start rates (company plus the three blocs), in both scenarios:
    /// the bloc kits are sized so AMC, the raw-goods bloc, mines and SPC and ECN do not.
    /// AMC starts with tech-mining-production (level 1: 2.5× mine output), so the 60M homeworld
    /// minimum lasts ~6.6 years for aluminium, regolith and water (hydrocarbons ~9.6). The phase-4
    /// 10-year floor no longer holds at that rate. Raising MinHomeworldMineralAmmount is a Jon
    /// decision — the plan says not to silently buff Earth's randomHW deposits.
    /// </summary>
    [TestCase("Pulsar4x.Scenario.WarpTechStart")]
    [TestCase("Pulsar4x.Scenario.EarlyTech")]
    public void EarthDeposits_LastAtLeastTenYears_AtStartRates(string scenario)
    {
        ModsState.RefreshModsList("Data");
        var paths = ModsState.AvailableMods.Where(m => m.Mod.ModName == "Pulsar4x").Select(m => m.Path).ToList();
        var store = GameLifecycle.LoadModStore(paths, ModsState.AvailableMods.Select(m => m.Path), scenario);
        var result = GameLifecycle.CreateGameCore(store, GameLifecycle.BuildQuickstartRequest(paths, GameLifecycle.BuildCatalog(store), 12345678));
        Assert.That(result, Is.Not.Null);
        var (game, player) = (result!.Value.game, result.Value.faction);
        var company = game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ColonyInfoDB>())
            .Single(c => c.FactionOwnerID == player.Id && BodyName(c) == "Earth");
        var earth = company.GetDataBlob<ColonyInfoDB>().PlanetEntity;
        var info = player.GetDataBlob<FactionInfoDB>();
        var names = info.Data.CargoGoods.GetMineralsList().ToDictionary(m => m.ID, m => m.Name);

        var report = new List<string>();
        double worst = double.MaxValue;
        var yearsByName = new Dictionary<string, double>();
        var minersByName = new Dictionary<string, string[]>();
        var annualByName = new Dictionary<string, long>();
        foreach (var (id, deposit) in earth.GetDataBlob<MineralsDB>().Minerals)
        {
            long annual = MiningHelper.BodyAnnualProduction(earth, id);
            if (annual <= 0)
                continue;
            double years = MiningHelper.YearsToDepletion(deposit.Amount.For(info.FactionMask), annual) ?? double.MaxValue;
            worst = Math.Min(worst, years);
            var by = earth.ColoniesOnBody().Where(c => c.GetDataBlob<MiningDB>().ActualMiningRate.TryGetValue(id, out var r) && r > 0)
                .Select(c => $"{c.GetDataBlob<NameDB>().DefaultName}={365 * c.GetDataBlob<MiningDB>().ActualMiningRate[id]}")
                .ToArray();
            var name = names.TryGetValue(id, out var n) ? n : id.ToString();
            yearsByName[name] = years;
            minersByName[name] = by;
            annualByName[name] = annual;
            report.Add($"{name}: {years:0.###} y ({deposit.Amount.For(info.FactionMask)} / {annual}; {string.Join(", ", by)})");
        }
        TestContext.Out.WriteLine(string.Join("\n", report));
        Assert.That(report, Is.Not.Empty, "something mines Earth");
        // Only the company (1 mine, no mining-production tech) and AMC (2 mines × 2.5) should mine.
        Assert.That(minersByName["Iron"], Is.EquivalentTo(new[] { "Earth HQ=1642500", "Americas Compact=8212500" }), string.Join("\n", report));
        Assert.That(annualByName["Iron"], Is.EqualTo(9_855_000L), string.Join("\n", report));
        // 60M min ÷ 9,855,000/y ≈ 6.6 years; don't let a further rate increase slip under that.
        Assert.That(worst, Is.GreaterThanOrEqualTo(6.5), string.Join("\n", report));
        foreach (var shortMineral in new[] { "Aluminium", "Regolith", "Water" })
            Assert.That(yearsByName[shortMineral], Is.GreaterThanOrEqualTo(6.5), string.Join("\n", report));
    }
}

static class Phase4TestExtensions
{
    /// <summary>Starts the consumption clock at <paramref name="at"/> so the next pass counts from there.</summary>
    public static void LastConsumption(this MarketPolicyRow _, Entity colony, DateTime at) =>
        colony.GetDataBlob<ColonyMarketPolicyDB>().LastConsumption = at;
}
