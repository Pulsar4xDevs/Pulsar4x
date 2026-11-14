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
/// Multi-faction Earth phase 6: staff wages billed with crew wages on the 1st, NPC blocs pay wages and collect
/// tax, commuter wages go to the bloc, blocs stop bidding below three months of running costs, Luna and Ceres
/// are exempt; plus a log-only solvency run.
/// </summary>
[TestFixture]
public class MultiFactionEarthPhase6Tests
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

    static ColonyWorkforceDB Wf(Entity c) => c.GetDataBlob<ColonyWorkforceDB>();
    static List<Transaction> Lines(Entity c, TransactionCategory cat) => Info(c).Money.GetTransactionsByCategory(cat);
    static DateTime NextFirst(DateTime d) => new DateTime(d.Year, d.Month, 1, 0, 0, 0, d.Kind).AddMonths(1);

    /// <summary>Runs the pay processor once a day from <paramref name="from"/> through <paramref name="to"/>.</summary>
    void PayDaily(DateTime from, DateTime to)
    {
        for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
            FactionSpending.PayDay(_game, d);
    }

    [Test]
    public void Payday_BooksCrewAndStaffLines_AndALayOffCutsTheNextBill()
    {
        var t0 = _company.StarSysDateTime.Date;
        var first = NextFirst(t0);
        // Own staff across the company's colonies (Earth HQ and Mars HQ).
        long staff = FactionSpending.StaffOnPayroll(Info(_company));
        Assert.That(staff, Is.EqualTo(Colonies().Where(c => c.FactionOwnerID == _player.Id).Sum(c => Wf(c).Employed)));
        PayDaily(t0, first);
        var line = Lines(_company, TransactionCategory.StaffWages).Single();
        int days = (first - t0).Days;
        Assert.That(line.Date, Is.EqualTo(first));
        Assert.That(line.Amount, Is.EqualTo(-(staff * 2m * days)), "Employed x 2 cr x days");
        Assert.That(line.Description, Does.StartWith("Staff wages for"));
        Assert.That(Lines(_company, TransactionCategory.Wages).Count(t => t.Date == first), Is.EqualTo(1), "crew wages on the same day");

        Workforce.SetTarget(_company, Wf(_company).Employed - 1_000);
        var second = NextFirst(first);
        PayDaily(first.AddDays(1), second);
        var next = Lines(_company, TransactionCategory.StaffWages).Single(t => t.Date == second);
        // The 1st itself was accrued at full staff before the lay-off; every later day is 1,000 fewer.
        Assert.That(next.Amount, Is.EqualTo(-((staff - 1_000) * 2m * (second - first).Days + 1_000 * 2m)), "laid-off staff are not paid");
    }

    [Test]
    public void Bloc_CollectsTax_AndPaysCrewAndStaffWages()
    {
        var amc = Named("Americas Compact");
        Assert.That(FactionSpending.PaysWages(_game, amc.FactionOwnerID), Is.True);
        Assert.That(FactionSpending.IsBlocked(_game, amc.FactionOwnerID), Is.False);
        var t0 = amc.StarSysDateTime.Date;
        var first = NextFirst(t0);
        decimal before = Info(amc).Money.GetCurrentFunds();
        PayDaily(t0, first);
        var tax = Lines(amc, TransactionCategory.Tax).Single();
        Assert.That(tax.Amount, Is.EqualTo(FactionSpending.Residents(Info(amc)) * 2m));
        Assert.That(tax.Amount, Is.EqualTo(2_000_000m), "1M people x 2 cr");
        decimal staff = -Lines(amc, TransactionCategory.StaffWages).Single().Amount;
        decimal crew = -Lines(amc, TransactionCategory.Wages).Single().Amount;
        Assert.That(staff, Is.EqualTo(Wf(amc).Employed * 2m * (first - t0).Days));
        Assert.That(crew, Is.GreaterThan(0));
        Assert.That(Info(amc).Money.GetCurrentFunds(), Is.EqualTo(before + tax.Amount - staff - crew));
    }

    [Test]
    public void CommuterWages_ArePaidIntoTheBlocsFunds()
    {
        var amc = Named("Americas Compact");
        Workforce.SetTarget(_company, Wf(_company).Required + 500);
        Assert.That(Workforce.TryContract(_company, amc, 500), Is.Null);
        var t0 = _company.StarSysDateTime.Date;
        var first = NextFirst(t0);
        PayDaily(t0, first);
        int days = (first - t0).Days;
        var income = Lines(amc, TransactionCategory.StaffWages).Single(t => t.Amount > 0);
        Assert.That(income.Amount, Is.EqualTo(500 * 2m * days));
        Assert.That(-Lines(_company, TransactionCategory.StaffWages).Single().Amount,
            Is.EqualTo((FactionSpending.StaffOnPayroll(Info(_company)) + 500) * 2m * days), "the company pays own staff and commuters");
    }

    [Test]
    public void BrokeBloc_StopsBidding_ButKeepsSelling()
    {
        var spc = Named("Sino-Pacific Coalition");
        var info = Info(spc);
        decimal reserve = BlocDemand.ReserveFor(spc);
        TestContext.Out.WriteLine($"SPC reserve {reserve:N0} (3 x {FactionSpending.MonthlyRunningCosts(_game, spc.FactionOwnerID):N0}/month)");
        info.Money.AddExpense(spc.StarSysDateTime, TransactionCategory.Trade, "test", info.Money.GetCurrentFunds() - reserve + 1m);
        var iron = Info(spc).Data.CargoGoods.GetAny("iron")!;
        var db = spc.GetDataBlob<CargoStorageDB>();
        db.RemoveCargoByUnit(iron, db.GetUnitsStored(iron, false));
        GameEngine.Engine.Orders.AgentProcessor.AssignGoal(spc, new GameEngine.Engine.Orders.Goal(GameEngine.Engine.Orders.GoalType.OfferStock));
        Assert.That(!MarketBook.TryGet(spc, "iron", out var buy) || buy.BuyQuantity == 0, Is.True, "below three months of costs: no bids");
        Assert.That(MarketBook.TryGet(spc, "stainless-steel", out var sell) && sell.SellQuantity > 0, Is.True, "still selling");
    }

    [Test]
    public void LunaAndCeres_AreExempt_AndDoNotGoBroke()
    {
        foreach (var name in new[] { "Luna Concord", "Ceres Depot" })
        {
            var c = Named(name);
            Assert.That(FactionSpending.PaysWages(_game, c.FactionOwnerID), Is.False, name);
            decimal before = Info(c).Money.GetCurrentFunds();
            var t0 = c.StarSysDateTime.Date;
            PayDaily(t0, NextFirst(t0));
            Assert.That(Info(c).Money.GetCurrentFunds(), Is.EqualTo(before), name);
        }
    }

    [Test]
    public void FactionSnapshot_SplitsCrewStaffLabsAndTax()
    {
        var projector = new GameProjector(_game);
        var company = projector.ProjectFaction(_player.Id)!;
        int days = company.DaysInMonth;
        Assert.That(company.StaffOnPayroll, Is.EqualTo(FactionSpending.StaffOnPayroll(Info(_company))).And.GreaterThan(Wf(_company).Employed));
        Assert.That(company.StaffWagePerDay, Is.EqualTo(2m));
        Assert.That(company.StaffWagesPerMonth, Is.EqualTo(company.StaffOnPayroll * 2m * days));
        Assert.That(company.RunningCostsPerMonth, Is.EqualTo(company.PayrollPerMonth + company.LabCostsPerMonth + company.StaffWagesPerMonth));
        Assert.That(company.TaxIncomePerMonth, Is.EqualTo(0));
        var amc = projector.ProjectFaction(Named("Americas Compact").FactionOwnerID)!;
        Assert.That(amc.TaxIncomePerMonth, Is.EqualTo(2_000_000m));
        Assert.That(amc.BidReserve, Is.GreaterThan(0));
    }

    /// <summary>
    /// Log-only solvency run (plan §8 row 6): the Warp Tech company's monthly burn at start (crew, staff, one lab), lean
    /// variant (shipyard and mine laid off), and runway. There is no scripted income, so break-even is reported as the
    /// monthly income the company would need. Never fails on the numbers.
    /// </summary>
    [Test]
    public void SolvencyRun_LogsTheCompanyBurnAndRunway()
    {
        var t0 = _company.StarSysDateTime.Date;
        var first = NextFirst(t0);
        var second = NextFirst(first);
        PayDaily(t0, second);
        var month = Info(_company).Money.GetAllTransactions().Where(t => t.Date == second).ToList();
        decimal crew = -month.Where(t => t.Category == TransactionCategory.Wages).Sum(t => t.Amount);
        decimal staff = -month.Where(t => t.Category == TransactionCategory.StaffWages).Sum(t => t.Amount);
        var snap = new GameProjector(_game).ProjectFaction(_player.Id)!;
        decimal lab = snap.LabCostsPerDay * (second - first).Days;
        decimal full = crew + staff + lab;
        var instances = _company.GetDataBlob<Pulsar4X.Datablobs.ComponentInstancesDB>();
        long laidOff = instances.DesignsAndComponentCount.Where(d => d.Key.UniqueID is "default-design-shipyard" or "default-design-mine")
            .Sum(d => (long)d.Key.CrewReq * d.Value);
        decimal lean = full - laidOff * 2m * (second - first).Days;
        decimal funds = Info(_company).Money.GetCurrentFunds();
        string log = $"Solvency (Warp Tech company, {first:yyyy-MM}): crew {crew:N0}, staff {staff:N0}, lab {lab:N0}, total {full:N0} cr/month; " +
            $"lean (shipyard + mine laid off, -{laidOff:N0} staff) {lean:N0}; funds {funds:N0}; runway {(double)(funds / full):0.0} months full, " +
            $"{(double)(funds / lean):0.0} lean; break-even needs {full:N0} cr/month of income (none scripted: never, passively).";
        TestContext.Out.WriteLine(log);
        System.IO.File.WriteAllText(System.IO.Path.Combine(TestContext.CurrentContext.WorkDirectory, "solvency.txt"), log);
        Assert.Pass(log);
    }
}
