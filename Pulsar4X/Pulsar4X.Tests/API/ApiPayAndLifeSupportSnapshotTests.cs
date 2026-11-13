using System;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Colonies;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Factions;
using Pulsar4X.Ships;
using Pulsar4X.Technology;

namespace Pulsar4X.Tests.API;

/// <summary>
/// Crew plan phases 4-5, step A: the life support and crew pay fields the UI binds to. Crew counts,
/// lab costs, days until broke, the negative-funds flag and the colony rate are real; ship life support,
/// wages, the spending block and the funds-negative pause reasons hold their defaults until the phases land.
/// </summary>
[TestFixture]
[NonParallelizable]
public class ApiPayAndLifeSupportSnapshotTests
{
    Game _game = null!;
    Entity _player = null!;
    GameProjector _projector = null!;

    [SetUp]
    public void SetUp()
    {
        ModsState.RefreshModsList("Data");
        var mods = ModsState.AvailableMods;
        var paths = new[] { mods.Single(m => m.Mod.ModName == "Pulsar4x").Path }.ToList();
        var store = GameLifecycle.LoadModStore(paths, mods.Select(m => m.Path), "Pulsar4x.Scenario.WarpTechStart");
        var created = GameLifecycle.CreateGameCore(store, GameLifecycle.BuildQuickstartRequest(paths, GameLifecycle.BuildCatalog(store), 12345))!.Value;
        _game = created.game;
        _player = created.faction;
        _projector = new GameProjector(_game);
    }

    System.Collections.Generic.IEnumerable<Entity> Owned<T>() where T : BaseDataBlob
        => _game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<T>()).Where(e => e.FactionOwnerID == _player.Id);

    [Test]
    public void Faction_CarriesPayrollCrew_RunningCosts_AndTheFundsFlags()
    {
        long crew = Owned<ShipCrewDB>().Sum(s => (long)s.GetDataBlob<ShipCrewDB>().Aboard)
            + Owned<ColonyCrewPoolDB>().Sum(c => c.GetDataBlob<ColonyCrewPoolDB>().Total);
        decimal labs = Owned<ResearcherDB>().Select(l => l.GetDataBlob<ResearcherDB>())
            .Where(r => r.TechQueue.Count > 0 && r.PointsPerDay.GetValue() > 0).Sum(r => r.CostPerDay.GetValue());

        var faction = _projector.ProjectFaction(_player.Id)!;
        Assert.That(crew, Is.GreaterThan(500), "ships aboard plus the 500 Earth pool");
        Assert.That(faction.CrewOnPayroll, Is.EqualTo(crew));
        Assert.That(faction.CrewWagePerDay, Is.EqualTo(0m), "wages are charged from phase 5");
        Assert.That(faction.PayrollPerDay, Is.EqualTo(0m));
        Assert.That(faction.LabCostsPerDay, Is.EqualTo(labs));
        Assert.That(faction.RunningCostsPerDay, Is.EqualTo(labs));
        if (labs > 0)
            Assert.That(faction.DaysUntilBroke, Is.EqualTo((double)(faction.Funds / labs)));
        else
            Assert.That(faction.DaysUntilBroke, Is.Null, "nothing costs anything: ∞");
        Assert.That(faction.Funds, Is.GreaterThan(0m));
        Assert.That(faction.FundsNegative, Is.False);
        Assert.That(faction.SpendingBlocked, Is.False);
        Assert.That(faction.BlockedSpending, Is.Empty);
        Assert.That(faction.LastWages, Is.EqualTo(0m));
        Assert.That(faction.LastWagesDate, Is.Null);
        Assert.That(FactionSnapshot.WagesCategory, Is.EqualTo(TransactionCategory.Wages.ToString()));
    }

    [Test]
    public void Faction_ShowsTheLastWagesLine_AndNegativeFunds()
    {
        var info = _player.GetDataBlob<FactionInfoDB>();
        var day = _game.TimePulse.GameGlobalDateTime;
        decimal funds = info.Money.GetCurrentFunds();
        info.Money.AddExpense(day.AddDays(-1), TransactionCategory.Wages, "Crew pay", 1_000);
        info.Money.AddExpense(day, TransactionCategory.Wages, "Crew pay", 2_000);
        info.Money.AddExpense(day, TransactionCategory.Wages, "Crew pay", funds);

        var faction = _projector.ProjectFaction(_player.Id)!;
        Assert.That(faction.Funds, Is.EqualTo(-3_000m));
        Assert.That(faction.FundsNegative, Is.True);
        Assert.That(faction.LastWages, Is.EqualTo(-(2_000m + funds)), "the last day's lines");
        Assert.That(faction.LastWagesDate, Is.EqualTo(day.Date));
        Assert.That(faction.Transactions.Count(t => t.Category == FactionSnapshot.WagesCategory), Is.EqualTo(3));
        if (faction.RunningCostsPerDay > 0)
            Assert.That(faction.DaysUntilBroke, Is.EqualTo(0));
    }

    [Test]
    public void ShipPower_HasTheLifeSupportRowShape_WithTheCrewAboard()
    {
        var ships = Owned<ShipCrewDB>().Where(s => s.HasDataBlob<Pulsar4X.Energy.EnergyGenAbilityDB>()).ToList();
        Assert.That(ships, Is.Not.Empty);
        foreach (var ship in ships)
        {
            var power = _projector.ProjectEntity(ship, _player.Id).GetView<ShipPowerView>()!;
            Assert.That(power.LifeSupportCrew, Is.EqualTo(ship.GetDataBlob<ShipCrewDB>().Aboard));
            Assert.That(power.HasLifeSupport, Is.True, "crew plan phase 4: ship life support draws");
            Assert.That(power.LifeSupportPerCrew_kW, Is.EqualTo(1.0).Within(1e-12), "1 kW per crew at life-support tech 0");
            Assert.That(power.LifeSupport_kW, Is.EqualTo(power.LifeSupportCrew * power.LifeSupportPerCrew_kW).Within(1e-9));
            Assert.That(power.LifeSupportMustRun, Is.True);
            var group = power.LoadGroups.Single(g => g.Kind == "Life support");
            Assert.That(group.Demand_kW, Is.EqualTo(power.LifeSupport_kW).Within(1e-9));
        }
    }

    [Test]
    public void ColonyPower_HasALifeSupportRateLabel()
    {
        var colonies = Owned<ColonyInfoDB>().Where(c => c.HasDataBlob<Pulsar4X.Energy.ColonyPowerDB>()).ToList();
        Assert.That(colonies, Is.Not.Empty);
        bool sawGarden = false;
        foreach (var colony in colonies)
        {
            var power = _projector.ProjectEntity(colony, _player.Id).GetView<ColonyPowerView>()!;
            if (power.LifeSupportColonists <= 0)
            {
                Assert.That(power.LifeSupportRateLabel, Is.Empty);
                continue;
            }
            double rate = power.LifeSupport_kW / power.LifeSupportColonists;
            Assert.That(power.LifeSupportPerPerson_kW, Is.EqualTo(rate).Within(1e-12));
            Assert.That(power.LifeSupportRateLabel, Is.EqualTo($"{(power.LifeSupportSealed ? "Sealed" : "Garden")} world: {rate:0.###} kW per person"));
            if (!power.LifeSupportSealed)
            {
                sawGarden = true;
                Assert.That(rate, Is.EqualTo(Pulsar4X.Energy.ColonyPowerProcessor.LifeSupportBase_kWPerColonist).Within(1e-12));
            }
        }
        Assert.That(sawGarden, Is.True, "Earth is a garden world");
    }

    [Test]
    public void Jobs_Pads_AndAcademies_HaveAnEmptyFundsReason()
    {
        Assert.That(FundsText.PausedFundsNegative, Is.EqualTo("Paused: funds negative"));
        var earth = Owned<ColonyInfoDB>().First(c => c.HasDataBlob<Pulsar4X.People.NavalAcademyDB>());
        var projected = _projector.ProjectEntity(earth, _player.Id);
        var industry = projected.GetView<IndustryView>()!;
        Assert.That(industry.Pads, Is.Not.Empty);
        Assert.That(industry.Pads.All(p => p.FundsReason == ""), Is.True);
        Assert.That(industry.ProductionLines.SelectMany(l => l.Jobs).All(j => j.FundsReason == ""), Is.True);
        var academy = projected.GetView<NavalAcademyView>()!.Academies.Single();
        Assert.That(academy.FundsReason, Is.Empty);
    }
}
