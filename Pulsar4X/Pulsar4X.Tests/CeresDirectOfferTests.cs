using System.Collections.Generic;
using System.Linq;
using GameEngine.Engine.Orders;
using NUnit.Framework;
using Pulsar4X.Colonies;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Fleets;
using Pulsar4X.Galaxy;
using Pulsar4X.Industry;
using Pulsar4X.Logistics;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Orbital;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

/// <summary>
/// The ship offers its whole mineral hold to one office. The clock is not run.
/// </summary>
[TestFixture]
public class CeresDirectOfferTests
{
    const long IronUnits = 40;
    const long CopperUnits = 15;

    [Test]
    public void PlacedIron_IsForSale_WithNoBuy()
    {
        var (game, sol) = Place();
        var depotFaction = Faction(game, CeresStart.DepotFactionName);
        var depot = Depot(sol);
        var luna = sol.GetAllEntitiesWithDataBlob<ColonyInfoDB>()
            .Single(c => c.GetDataBlob<NameDB>().DefaultName == "Luna Concord");

        var ironGood = depotFaction.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetAny("iron");
        long stock = MarketBook.Stock(depot, ironGood);
        Assert.That(MarketBook.TryGet(depot, "iron", out var iron), Is.True);
        Assert.That(iron.Ask, Is.EqualTo(CeresStart.IronAsk));
        Assert.That(iron.Bid, Is.EqualTo(CeresStart.IronBid));
        Assert.That(iron.SellQuantity, Is.EqualTo(stock));
        Assert.That(iron.BuyQuantity, Is.EqualTo(0));

        Assert.That(MarketBook.TryGet(depot, "methalox", out var fuel), Is.True);
        Assert.That(fuel.Bid, Is.EqualTo(0));
        Assert.That(fuel.BuyQuantity, Is.EqualTo(0));
        Assert.That(MarketBook.TryGet(depot, "water", out var water), Is.True);
        Assert.That(water.Bid, Is.EqualTo(0));
        Assert.That(water.BuyQuantity, Is.EqualTo(0));

        Assert.That(MarketBook.TryGet(luna, "iron", out var lunaIron), Is.True);
        Assert.That(lunaIron.Bid, Is.EqualTo(0));
        Assert.That(lunaIron.BuyQuantity, Is.EqualTo(0));
    }

    [Test]
    public void AlongsideCeres_SellsTheWholeHold_AndLeavesTheFuel()
    {
        var (game, sol) = Place();
        var lode = Faction(game, CeresStart.MiningFactionName);
        var depotFaction = Faction(game, CeresStart.DepotFactionName);
        var fleet = Fleet(sol, lode.Id);
        var ship = Ship(sol, lode.Id);
        var depot = Depot(sol);
        var standing = fleet.GetDataBlob<GoalsDB>().ActiveGoal!;
        Assert.That(standing.Type, Is.EqualTo(GoalType.MineStanding));

        var (iron, copper) = LoadHold(lode, ship);
        var depotIron = depotFaction.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetMineral("iron");
        var depotCopper = depotFaction.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetMineral("copper");
        long fuelBefore = FuelUnits(ship, lode);
        Assert.That(fuelBefore, Is.GreaterThan(0));
        BringAlongside(ship, depot);
        ReadyToSell(ship, standing);

        long depotIronBefore = MarketBook.Stock(depot, depotIron);
        long depotCopperBefore = MarketBook.Stock(depot, depotCopper);
        decimal lodeBefore = lode.GetDataBlob<FactionInfoDB>().Money.GetCurrentFunds();
        decimal purseBefore = depotFaction.GetDataBlob<FactionInfoDB>().Money.GetCurrentFunds();
        var book = BookIds(depot);
        var policy = PolicyIds(depot);
        int lodeTrades = lode.GetDataBlob<FactionInfoDB>().Money.GetTransactionsByCategory(TransactionCategory.Trade).Count;

        AgentProcessor.RunAgentNow(ship);

        long units = IronUnits + CopperUnits;
        decimal cost = units * CeresStart.LotPrice;
        Assert.That(MarketBook.Stock(ship, iron), Is.EqualTo(0));
        Assert.That(MarketBook.Stock(ship, copper), Is.EqualTo(0));
        Assert.That(MarketBook.Stock(depot, depotIron), Is.EqualTo(depotIronBefore + IronUnits));
        Assert.That(MarketBook.Stock(depot, depotCopper), Is.EqualTo(depotCopperBefore + CopperUnits));
        Assert.That(FuelUnits(ship, lode), Is.EqualTo(fuelBefore));
        Assert.That(lode.GetDataBlob<FactionInfoDB>().Money.GetCurrentFunds(), Is.EqualTo(lodeBefore + cost));
        Assert.That(depotFaction.GetDataBlob<FactionInfoDB>().Money.GetCurrentFunds(), Is.EqualTo(purseBefore - cost));
        Assert.That(lode.GetDataBlob<FactionInfoDB>().Money.GetTransactionsByCategory(TransactionCategory.Trade).Count,
            Is.EqualTo(lodeTrades + 1));
        Assert.That(BookIds(depot), Is.EquivalentTo(book));
        Assert.That(PolicyIds(depot), Is.EquivalentTo(policy));
        Assert.That(BookIds(depot), Does.Not.Contain("copper"));

        var mine = ship.GetDataBlob<GoalsDB>().ActiveGoal!;
        Assert.That(mine.Type, Is.EqualTo(GoalType.MineAsteroids));
        Assert.That(mine.Status, Is.EqualTo(GoalStatus.Active));
        Assert.That(standing.Type, Is.EqualTo(GoalType.MineStanding));
        Assert.That(standing.Status, Is.EqualTo(GoalStatus.Active));
    }

    [Test]
    public void FullDepot_SellsTheWholeHoldToTheNextOffice()
    {
        var (game, sol) = Place();
        var lode = Faction(game, CeresStart.MiningFactionName);
        var depotFaction = Faction(game, CeresStart.DepotFactionName);
        var lunaFaction = Faction(game, "Luna Concord");
        var fleet = Fleet(sol, lode.Id);
        var ship = Ship(sol, lode.Id);
        var depot = Depot(sol);
        var luna = sol.GetAllEntitiesWithDataBlob<ColonyInfoDB>()
            .Single(c => c.GetDataBlob<NameDB>().DefaultName == "Luna Concord");
        var standing = fleet.GetDataBlob<GoalsDB>().ActiveGoal!;

        var (iron, copper) = LoadHold(lode, ship);
        var depotIron = depotFaction.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetMineral("iron");
        var lunaIron = lunaFaction.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetMineral("iron");
        var lunaCopper = lunaFaction.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetMineral("copper");
        long depotIronBefore = MarketBook.Stock(depot, depotIron);
        long lunaIronBefore = MarketBook.Stock(luna, lunaIron);
        long lunaCopperBefore = MarketBook.Stock(luna, lunaCopper);
        decimal depotBefore = depotFaction.GetDataBlob<FactionInfoDB>().Money.GetCurrentFunds();
        decimal lunaBefore = lunaFaction.GetDataBlob<FactionInfoDB>().Money.GetCurrentFunds();
        lunaFaction.GetDataBlob<FactionInfoDB>().Money.AddIncome(
            ship.StarSysDateTime, TransactionCategory.InitialInvestment, "test", 1000);
        lunaBefore += 1000;
        var lunaBook = BookIds(luna);

        FillGeneralStorage(depot);
        // Cargo range needs the same orbit parent. Sitting at Ceres can choose Luna and still not transfer.
        SitWith(ship, luna);
        ReadyToSell(ship, standing);

        AgentProcessor.RunAgentNow(ship);

        long units = IronUnits + CopperUnits;
        decimal cost = units * CeresStart.LotPrice;
        Assert.That(ship.GetDataBlob<GoalsDB>().ActiveGoal!.DestEntityId, Is.EqualTo(luna.Id));
        Assert.That(MarketBook.Stock(ship, iron), Is.EqualTo(0));
        Assert.That(MarketBook.Stock(ship, copper), Is.EqualTo(0));
        Assert.That(MarketBook.Stock(depot, depotIron), Is.EqualTo(depotIronBefore));
        Assert.That(MarketBook.Stock(luna, lunaIron), Is.EqualTo(lunaIronBefore + IronUnits));
        Assert.That(MarketBook.Stock(luna, lunaCopper), Is.EqualTo(lunaCopperBefore + CopperUnits));
        Assert.That(depotFaction.GetDataBlob<FactionInfoDB>().Money.GetCurrentFunds(), Is.EqualTo(depotBefore));
        Assert.That(lunaFaction.GetDataBlob<FactionInfoDB>().Money.GetCurrentFunds(), Is.EqualTo(lunaBefore - cost));
        Assert.That(BookIds(luna), Is.EquivalentTo(lunaBook));
        Assert.That(standing.Status, Is.EqualTo(GoalStatus.Active));
    }

    [Test]
    public void NoRoom_Waits_AndKeepsBothMinerals()
    {
        var (game, sol) = Place();
        var lode = Faction(game, CeresStart.MiningFactionName);
        var lunaFaction = Faction(game, "Luna Concord");
        var fleet = Fleet(sol, lode.Id);
        var ship = Ship(sol, lode.Id);
        var depot = Depot(sol);
        var luna = sol.GetAllEntitiesWithDataBlob<ColonyInfoDB>()
            .Single(c => c.GetDataBlob<NameDB>().DefaultName == "Luna Concord");
        var standing = fleet.GetDataBlob<GoalsDB>().ActiveGoal!;

        var (iron, copper) = LoadHold(lode, ship);
        lunaFaction.GetDataBlob<FactionInfoDB>().Money.AddIncome(
            ship.StarSysDateTime, TransactionCategory.InitialInvestment, "test", 1000);
        FillGeneralStorage(depot);
        FillGeneralStorage(luna);
        BringAlongside(ship, depot);
        ReadyToSell(ship, standing);

        AgentProcessor.RunAgentNow(ship);

        var queue = ship.GetDataBlob<ActionQueueDB>();
        Assert.That(queue.ActionList.OfType<UnloadWaitOrder>().Any(), Is.True);
        Assert.That(queue.ActionList.OfType<LotOfferAction>().Any(), Is.False);
        Assert.That(queue.ActionList.OfType<MarketExchangeAction>().Any(), Is.False);
        Assert.That(MarketBook.Stock(ship, iron), Is.EqualTo(IronUnits));
        Assert.That(MarketBook.Stock(ship, copper), Is.EqualTo(CopperUnits));
        var mine = ship.GetDataBlob<GoalsDB>().ActiveGoal!;
        Assert.That(mine.Type, Is.EqualTo(GoalType.MineAsteroids));
        Assert.That(mine.Status, Is.EqualTo(GoalStatus.Active));
        Assert.That(standing.Type, Is.EqualTo(GoalType.MineStanding));
        Assert.That(standing.Status, Is.EqualTo(GoalStatus.Active));
    }

    [Test]
    public void EmptyPurse_Waits_AndKeepsBothMinerals()
    {
        var (game, sol) = Place();
        var lode = Faction(game, CeresStart.MiningFactionName);
        var depotFaction = Faction(game, CeresStart.DepotFactionName);
        var fleet = Fleet(sol, lode.Id);
        var ship = Ship(sol, lode.Id);
        var depot = Depot(sol);
        var standing = fleet.GetDataBlob<GoalsDB>().ActiveGoal!;

        var (iron, copper) = LoadHold(lode, ship);
        BringAlongside(ship, depot);
        ReadyToSell(ship, standing);

        var purse = depotFaction.GetDataBlob<FactionInfoDB>().Money;
        purse.AddExpense(ship.StarSysDateTime, TransactionCategory.Trade, "test", purse.GetCurrentFunds());

        AgentProcessor.RunAgentNow(ship);

        var queue = ship.GetDataBlob<ActionQueueDB>();
        Assert.That(queue.ActionList.OfType<UnloadWaitOrder>().Any(), Is.True);
        Assert.That(queue.ActionList.OfType<LotOfferAction>().Any(), Is.False);
        Assert.That(queue.ActionList.OfType<MarketExchangeAction>().Any(), Is.False);
        Assert.That(MarketBook.Stock(ship, iron), Is.EqualTo(IronUnits));
        Assert.That(MarketBook.Stock(ship, copper), Is.EqualTo(CopperUnits));
        var mine = ship.GetDataBlob<GoalsDB>().ActiveGoal!;
        Assert.That(mine.Type, Is.EqualTo(GoalType.MineAsteroids));
        Assert.That(mine.Status, Is.EqualTo(GoalStatus.Active));
        Assert.That(standing.Status, Is.EqualTo(GoalStatus.Active));
    }

    [Test]
    public void OutOfRange_Moves_AndDoesNotTransfer()
    {
        var (game, sol) = Place();
        var lode = Faction(game, CeresStart.MiningFactionName);
        var depotFaction = Faction(game, CeresStart.DepotFactionName);
        var fleet = Fleet(sol, lode.Id);
        var ship = Ship(sol, lode.Id);
        var depot = Depot(sol);
        var standing = fleet.GetDataBlob<GoalsDB>().ActiveGoal!;

        var (iron, copper) = LoadHold(lode, ship);
        long fuelBefore = FuelUnits(ship, lode);
        decimal lodeBefore = lode.GetDataBlob<FactionInfoDB>().Money.GetCurrentFunds();
        decimal purseBefore = depotFaction.GetDataBlob<FactionInfoDB>().Money.GetCurrentFunds();
        // The spawn orbit is inside the spaceport range. Close it. The ship stays in Ceres's well,
        // so the move is an altitude change, not a warp.
        ship.GetDataBlob<CargoStorageDB>().TransferRangeDv_mps = 100;
        depot.GetDataBlob<CargoStorageDB>().TransferRangeDv_mps = 100;
        Assert.That(MarketExchangeAction.InRange(ship, depot), Is.False);
        ReadyToSell(ship, standing);

        AgentProcessor.RunAgentNow(ship);

        var queue = ship.GetDataBlob<ActionQueueDB>();
        var mine = ship.GetDataBlob<GoalsDB>().ActiveGoal!;
        Assert.That(mine.DestEntityId, Is.EqualTo(depot.Id));
        Assert.That(queue.ActionList.OfType<ChangeOrbitalAltitudeAction>().Any(), Is.True,
            string.Join(", ", queue.ActionList.Select(a => a.GetType().Name)));
        Assert.That(queue.ActionList.OfType<LotOfferAction>().Any(), Is.False);
        Assert.That(MarketBook.Stock(ship, iron), Is.EqualTo(IronUnits));
        Assert.That(MarketBook.Stock(ship, copper), Is.EqualTo(CopperUnits));
        Assert.That(FuelUnits(ship, lode), Is.EqualTo(fuelBefore));
        Assert.That(lode.GetDataBlob<FactionInfoDB>().Money.GetCurrentFunds(), Is.EqualTo(lodeBefore));
        Assert.That(depotFaction.GetDataBlob<FactionInfoDB>().Money.GetCurrentFunds(), Is.EqualTo(purseBefore));
        Assert.That(mine.Status, Is.EqualTo(GoalStatus.Active));
        Assert.That(standing.Status, Is.EqualTo(GoalStatus.Active));
    }

    static (Mineral iron, Mineral copper) LoadHold(Entity lode, Entity ship)
    {
        var data = lode.GetDataBlob<FactionInfoDB>().Data;
        data.Unlock("iron");
        data.Unlock("copper");
        var iron = data.CargoGoods.GetMineral("iron");
        var copper = data.CargoGoods.GetMineral("copper");
        var cargo = ship.GetDataBlob<CargoStorageDB>();
        Assert.That(cargo.AddCargoByUnit(iron, IronUnits), Is.EqualTo(IronUnits));
        Assert.That(cargo.AddCargoByUnit(copper, CopperUnits), Is.EqualTo(CopperUnits));
        return (iron, copper);
    }

    static long FuelUnits(Entity ship, Entity faction)
    {
        var fuel = faction.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetMaterial("methalox");
        var cargo = ship.GetDataBlob<CargoStorageDB>();
        if (!cargo.TypeStores.TryGetValue(fuel.CargoTypeID, out var store))
            return 0;
        return store.CurrentStoreInUnits.TryGetValue(fuel.ID, out var units) ? units : 0;
    }

    static void FillGeneralStorage(Entity colony)
    {
        colony.GetDataBlob<CargoStorageDB>().TypeStores["general-storage"].FreeVolume = 0;
    }

    static HashSet<string> BookIds(Entity colony)
        => colony.GetDataBlob<LogiBaseDB>().Listings.Keys.ToHashSet();

    static HashSet<string> PolicyIds(Entity colony)
        => colony.GetDataBlob<ColonyMarketPolicyDB>().Rows.Keys.ToHashSet();

    static void ReadyToSell(Entity ship, Goal standing)
    {
        // An Active goal with an empty queue is completed and never planned.
        var goal = new Goal(GoalType.MineAsteroids)
        {
            ParentGoalId = standing.Id,
            TargetEntityID = -1,
            Status = GoalStatus.Planning,
        };
        if (!ship.TryGetDataBlob<GoalsDB>(out var goals))
        {
            goals = new GoalsDB();
            ship.SetDataBlob(goals);
        }
        goals.GivenGoal = goal;
        goals.ActiveGoal = goal;
        if (ship.TryGetDataBlob<ActionQueueDB>(out var queue))
            queue.ActionList.Clear();
    }

    static void BringAlongside(Entity ship, Entity market)
    {
        ship.GetDataBlob<CargoStorageDB>().TransferRangeDv_mps = 1e9;
        market.GetDataBlob<CargoStorageDB>().TransferRangeDv_mps = 1e9;
    }

    /// <summary>
    /// Parent the ship to the colony's body and open cargo range. A different parent is never in range.
    /// </summary>
    static void SitWith(Entity ship, Entity colony)
    {
        var planet = colony.GetDataBlob<ColonyInfoDB>().PlanetEntity;
        double radius = planet.GetDataBlob<MassVolumeDB>().RadiusInM;
        var pos = ship.GetDataBlob<PositionDB>();
        pos.SetParent(planet);
        pos.RelativePosition = new Vector3(radius * 2, 0, 0);
        BringAlongside(ship, colony);
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
        => sol.GetAllEntitiesWithDataBlob<ColonyInfoDB>()
            .Single(c => c.GetDataBlob<NameDB>().DefaultName == CeresStart.DepotFactionName);
}
