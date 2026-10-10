using System;
using System.Linq;
using GameEngine.Engine.Orders;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Fleets;
using Pulsar4X.Galaxy;
using Pulsar4X.Logistics;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

/// <summary>
/// Fuel buys are planned here. The clock is not run.
/// </summary>
[TestFixture]
public class CeresFuelBuyTests
{
    [Test]
    public void LowTank_BuysFromTheDepot_ThenTheSurveyContinues()
    {
        var (game, sol) = Place();
        var strata = Faction(game, CeresStart.SurveyFactionName);
        var depotFaction = Faction(game, CeresStart.DepotFactionName);
        var fleet = Fleet(sol, strata.Id);
        var ship = Ship(sol, strata.Id);
        var depot = Depot(sol);
        var standing = fleet.GetDataBlob<GoalsDB>().ActiveGoal!;

        FinishChild(ship);
        long beforeUnits = Drain(ship, 1);
        BringAlongside(ship, depot);
        decimal strataBefore = strata.GetDataBlob<FactionInfoDB>().Money.GetCurrentFunds();
        decimal depotBefore = depotFaction.GetDataBlob<FactionInfoDB>().Money.GetCurrentFunds();
        long space = ship.GetDataBlob<CargoStorageDB>().GetFreeUnitSpace(Fuel(ship));

        AgentProcessor.RunAgentNow(fleet);

        var child = ship.GetDataBlob<GoalsDB>().GivenGoal;
        Assert.That(child, Is.Not.Null);
        Assert.That(child!.Type, Is.EqualTo(GoalType.BuyFuel));
        Assert.That(child.Type, Is.Not.EqualTo(GoalType.ServeyBodies));
        Assert.That(child.ParentGoalId, Is.EqualTo(standing.Id));
        Assert.That(child.TargetEntityID, Is.EqualTo(depot.Id));

        AgentProcessor.RunAgentNow(ship);

        long bought = ship.GetDataBlob<CargoStorageDB>().GetUnitsStored(Fuel(ship), false) - beforeUnits;
        long affordable = (long)Math.Floor(strataBefore / CeresStart.FuelAsk);
        Assert.That(bought, Is.EqualTo(Math.Min(space, affordable)));
        Assert.That(bought, Is.GreaterThan(0));
        decimal cost = bought * CeresStart.FuelAsk;
        Assert.That(strata.GetDataBlob<FactionInfoDB>().Money.GetCurrentFunds(), Is.EqualTo(strataBefore - cost));
        Assert.That(depotFaction.GetDataBlob<FactionInfoDB>().Money.GetCurrentFunds(), Is.EqualTo(depotBefore + cost));

        AgentProcessor.RunAgentNow(ship);
        Assert.That(child.Status, Is.EqualTo(GoalStatus.Completed));

        AgentProcessor.RunAgentNow(fleet);
        var next = ship.GetDataBlob<GoalsDB>().GivenGoal;
        Assert.That(next!.Type, Is.EqualTo(GoalType.ServeyBodies));
        Assert.That(next.ParentGoalId, Is.EqualTo(standing.Id));
        Assert.That(standing.Type, Is.EqualTo(GoalType.SurveyStanding));
        Assert.That(standing.Status, Is.EqualTo(GoalStatus.Active));
    }

    [Test]
    public void Units_AreLimitedByFreeSpace()
    {
        var (game, sol) = Place();
        var strata = Faction(game, CeresStart.SurveyFactionName);
        var fleet = Fleet(sol, strata.Id);
        var ship = Ship(sol, strata.Id);

        FinishChild(ship);
        var fuel = Fuel(ship);
        long stored = Drain(ship, 2);
        var tank = ship.GetDataBlob<CargoStorageDB>().TypeStores[fuel.CargoTypeID];
        tank.FreeVolume = 4 * fuel.VolumePerUnit;
        BringAlongside(ship, Depot(sol));
        long space = ship.GetDataBlob<CargoStorageDB>().GetFreeUnitSpace(fuel);
        Assert.That(space, Is.EqualTo(4));
        Assert.That(FuelSituation.Observe(ship).IsLow, Is.True);

        decimal before = strata.GetDataBlob<FactionInfoDB>().Money.GetCurrentFunds();
        Assert.That(Math.Floor(before / CeresStart.FuelAsk), Is.GreaterThan(space));

        AgentProcessor.RunAgentNow(fleet);
        Assert.That(ship.GetDataBlob<GoalsDB>().GivenGoal!.Type, Is.EqualTo(GoalType.BuyFuel));
        AgentProcessor.RunAgentNow(ship);

        long bought = ship.GetDataBlob<CargoStorageDB>().GetUnitsStored(fuel, false) - stored;
        Assert.That(bought, Is.EqualTo(space));
        Assert.That(strata.GetDataBlob<FactionInfoDB>().Money.GetCurrentFunds(), Is.EqualTo(before - (bought * CeresStart.FuelAsk)));
    }

    [Test]
    public void FullTank_IsNotHandedAFuelChild()
    {
        var (game, sol) = Place();
        var strata = Faction(game, CeresStart.SurveyFactionName);
        var fleet = Fleet(sol, strata.Id);
        var ship = Ship(sol, strata.Id);
        var standing = fleet.GetDataBlob<GoalsDB>().ActiveGoal!;

        FinishChild(ship);
        Assert.That(FuelSituation.Observe(ship).IsLow, Is.False);

        AgentProcessor.RunAgentNow(fleet);

        var child = ship.GetDataBlob<GoalsDB>().GivenGoal;
        Assert.That(child!.Type, Is.EqualTo(GoalType.ServeyBodies));
        Assert.That(child.ParentGoalId, Is.EqualTo(standing.Id));
        Assert.That(standing.Status, Is.EqualTo(GoalStatus.Active));
    }

    [Test]
    public void EmptyPurse_LeavesTheStandingGoalActive()
    {
        var (game, sol) = Place();
        var strata = Faction(game, CeresStart.SurveyFactionName);
        var fleet = Fleet(sol, strata.Id);
        var ship = Ship(sol, strata.Id);
        var standing = fleet.GetDataBlob<GoalsDB>().ActiveGoal!;
        var finished = ship.GetDataBlob<GoalsDB>().ActiveGoal!;
        finished.Status = GoalStatus.Completed;
        Drain(ship, 1);

        var money = strata.GetDataBlob<FactionInfoDB>().Money;
        money.AddExpense(fleet.StarSysDateTime, TransactionCategory.Trade, "test", money.GetCurrentFunds());

        AgentProcessor.RunAgentNow(fleet);

        Assert.That(ship.GetDataBlob<GoalsDB>().GivenGoal, Is.SameAs(finished));
        Assert.That(standing.Type, Is.EqualTo(GoalType.SurveyStanding));
        Assert.That(standing.Status, Is.EqualTo(GoalStatus.Active));
        Assert.That(money.GetCurrentFunds(), Is.EqualTo(0));
    }

    [Test]
    public void NoMethaloxForSale_LeavesTheStandingGoalActive()
    {
        var (game, sol) = Place();
        var strata = Faction(game, CeresStart.SurveyFactionName);
        var fleet = Fleet(sol, strata.Id);
        var ship = Ship(sol, strata.Id);
        var standing = fleet.GetDataBlob<GoalsDB>().ActiveGoal!;
        var finished = ship.GetDataBlob<GoalsDB>().ActiveGoal!;
        finished.Status = GoalStatus.Completed;
        Drain(ship, 1);
        MarketBook.RemoveListing(Depot(sol), ship.GetDataBlob<NewtonThrustAbilityDB>().FuelType);
        decimal before = strata.GetDataBlob<FactionInfoDB>().Money.GetCurrentFunds();

        AgentProcessor.RunAgentNow(fleet);

        Assert.That(ship.GetDataBlob<GoalsDB>().GivenGoal, Is.SameAs(finished));
        Assert.That(standing.Status, Is.EqualTo(GoalStatus.Active));
        Assert.That(strata.GetDataBlob<FactionInfoDB>().Money.GetCurrentFunds(), Is.EqualTo(before));
    }

    [Test]
    public void LowMiningTank_BuysFuel_ThenMines()
    {
        var (game, sol) = Place();
        var lode = Faction(game, CeresStart.MiningFactionName);
        var fleet = Fleet(sol, lode.Id);
        var ship = Ship(sol, lode.Id);
        var standing = fleet.GetDataBlob<GoalsDB>().ActiveGoal!;

        FinishChild(ship);
        Drain(ship, 1);
        BringAlongside(ship, Depot(sol));
        decimal before = lode.GetDataBlob<FactionInfoDB>().Money.GetCurrentFunds();

        AgentProcessor.RunAgentNow(fleet);
        var child = ship.GetDataBlob<GoalsDB>().GivenGoal;
        Assert.That(child!.Type, Is.EqualTo(GoalType.BuyFuel));
        Assert.That(child.ParentGoalId, Is.EqualTo(standing.Id));
        Assert.That(lode.GetDataBlob<FactionInfoDB>().Money.GetCurrentFunds(), Is.EqualTo(before));

        AgentProcessor.RunAgentNow(ship);
        AgentProcessor.RunAgentNow(ship);
        Assert.That(child.Status, Is.EqualTo(GoalStatus.Completed));

        AgentProcessor.RunAgentNow(fleet);
        Assert.That(ship.GetDataBlob<GoalsDB>().GivenGoal!.Type, Is.EqualTo(GoalType.MineAsteroids));
        Assert.That(standing.Type, Is.EqualTo(GoalType.MineStanding));
        Assert.That(standing.Status, Is.EqualTo(GoalStatus.Active));
    }

    [Test]
    public void RefuelAt_CreatesACargoPair_AndDoesNotTouchTheLedger()
    {
        var (game, sol) = Place();
        var strata = Faction(game, CeresStart.SurveyFactionName);
        var depotFaction = Faction(game, CeresStart.DepotFactionName);
        var ship = Ship(sol, strata.Id);
        var depot = Depot(sol);

        Drain(ship, 1);
        decimal strataBefore = strata.GetDataBlob<FactionInfoDB>().Money.GetCurrentFunds();
        decimal depotBefore = depotFaction.GetDataBlob<FactionInfoDB>().Money.GetCurrentFunds();

        var plan = new RefuelShipPlanner().Plan(
            ship,
            new Goal(GoalType.RefuelAt) { TargetEntityID = depot.Id },
            ship.StarSysDateTime);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active), plan.Message);
        Assert.That(plan.Actions.OfType<CargoTransferOrder>().Count(), Is.EqualTo(1), plan.Message);
        Assert.That(plan.Actions.OfType<MarketExchangeAction>(), Is.Empty);
        Assert.That(strata.GetDataBlob<FactionInfoDB>().Money.GetCurrentFunds(), Is.EqualTo(strataBefore));
        Assert.That(depotFaction.GetDataBlob<FactionInfoDB>().Money.GetCurrentFunds(), Is.EqualTo(depotBefore));
    }

    [Test]
    public void StartingOrbit_HandsBuyFuel_AndMovesBeforeItPays()
    {
        var (game, sol) = Place();
        var strata = Faction(game, CeresStart.SurveyFactionName);
        var fleet = Fleet(sol, strata.Id);
        var ship = Ship(sol, strata.Id);
        var depot = Depot(sol);

        FinishChild(ship);
        Drain(ship, 1);
        decimal before = strata.GetDataBlob<FactionInfoDB>().Money.GetCurrentFunds();

        AgentProcessor.RunAgentNow(fleet);
        var child = ship.GetDataBlob<GoalsDB>().GivenGoal;
        Assert.That(child!.Type, Is.EqualTo(GoalType.BuyFuel));
        Assert.That(MarketExchangeAction.InRange(ship, depot), Is.False);

        var plan = new BuyFuelPlan().Plan(ship, child, ship.StarSysDateTime);
        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active), plan.Message);
        Assert.That(plan.Actions.OfType<MarketExchangeAction>(), Is.Empty);
        Assert.That(plan.Actions.OfType<ChangeOrbitalAltitudeAction>().Count(), Is.EqualTo(1), plan.Message);
        Assert.That(strata.GetDataBlob<FactionInfoDB>().Money.GetCurrentFunds(), Is.EqualTo(before));
    }

    /// <summary>
    /// Opens the cargo-transfer range so the exchange can run without flying the orbit.
    /// The starting orbit itself is outside the default range.
    /// </summary>
    static void BringAlongside(Entity ship, Entity market)
    {
        ship.GetDataBlob<CargoStorageDB>().TransferRangeDv_mps = 1e9;
        market.GetDataBlob<CargoStorageDB>().TransferRangeDv_mps = 1e9;
    }

    static void FinishChild(Entity ship)
    {
        ship.GetDataBlob<GoalsDB>().ActiveGoal!.Status = GoalStatus.Completed;
    }

    static ICargoable Fuel(Entity ship)
    {
        Assert.That(FuelSituation.TryGetFuelMass(ship, out var fuel, out _, out _), Is.True);
        return fuel;
    }

    static long Drain(Entity ship, long leave)
    {
        var fuel = Fuel(ship);
        var store = ship.GetDataBlob<CargoStorageDB>();
        long have = store.GetUnitsStored(fuel, false);
        if (have > leave)
            store.RemoveCargoByUnit(fuel, have - leave);
        return store.GetUnitsStored(fuel, false);
    }

    static (Game game, StarSystem sol) Place()
    {
        var modLoader = new ModLoader();
        var store = new ModDataStore();
        modLoader.LoadModManifest("Data/basemod/modInfo.json", store);
        var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);
        game.Settings.EnforceSingleThread = true;
        StarSystemFactory.LoadFromBlueprint(game, store.Systems["system-sol"]);
        var player = FactionFactory.CreateBasicFaction(game, "United Earth Corp", "UEC", 0);
        player.FactionOwnerID = player.Id;
        player.GetDataBlob<FactionInfoDB>().KnownSystems.Add("system-sol");
        ColonyFactory.PlaceOwnedColonies(game, store, player, store.Species["species-human"]);
        FactionFactory.PlaceFactions(game, store);
        return (game, game.Systems.Single(s => s.ID == "system-sol"));
    }

    static Entity Faction(Game game, string name)
        => game.Factions.Values.Single(f => f.GetDataBlob<NameDB>().DefaultName == name);

    static Entity Fleet(StarSystem sol, int factionId)
        => sol.GetAllEntitiesWithDataBlob<FleetDB>().Single(f => f.FactionOwnerID == factionId);

    static Entity Ship(StarSystem sol, int factionId)
        => sol.GetAllEntitiesWithDataBlob<ShipInfoDB>().Single(s => s.FactionOwnerID == factionId);

    static Entity Depot(StarSystem sol)
        => sol.GetAllEntitiesWithDataBlob<ColonyInfoDB>().Single(c => c.GetDataBlob<NameDB>().DefaultName == CeresStart.DepotFactionName);
}
