using System.Linq;
using GameEngine.Engine.Orders;
using GameEngine.People;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Fleets;
using Pulsar4X.Galaxy;
using Pulsar4X.GeoSurveys;
using Pulsar4X.Industry;
using Pulsar4X.Logistics;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Extensions;
using Pulsar4X.Names;
using Pulsar4X.People;
using Pulsar4X.Ships;

namespace Pulsar4X.Tests;

/// <summary>
/// Standing jobs are planned here. The clock is not run: a finished geo site can replan into a warp-to-self.
/// </summary>
[TestFixture]
public class CeresStandingJobTests
{
    [Test]
    public void PlacedSurveyFlight_KeepsTheStandingGoal_AndAShipBridge()
    {
        var (game, sol) = Place();
        var strata = Faction(game, CeresStart.SurveyFactionName);
        var fleet = Fleet(sol, strata.Id);
        var ship = Ship(sol, strata.Id);

        var standing = fleet.GetDataBlob<GoalsDB>().GivenGoal;
        Assert.That(standing, Is.SameAs(fleet.GetDataBlob<GoalsDB>().ActiveGoal));
        Assert.That(standing!.Type, Is.EqualTo(GoalType.SurveyStanding));
        Assert.That(standing.Status, Is.EqualTo(GoalStatus.Active));
        Assert.That(ship.GetDataBlob<AdminSpaceDB>().CommanderSeats, Is.Not.Empty);
        Assert.That(ship.GetDataBlob<AdminSpaceDB>().CommanderSeats.All(s => s.SeatType == AdminLevel.Ship), Is.True);

        Assert.That(ship.TryGetDataBlob<GoalsDB>(out var shipGoals) && shipGoals.GivenGoal != null, Is.True, "Pathfinder was not handed a rock");
        if (ship.TryGetDataBlob<GoalsDB>(out shipGoals) && shipGoals.GivenGoal != null)
        {
            Assert.That(shipGoals.GivenGoal.ParentGoalId, Is.EqualTo(standing.Id));
            Assert.That(shipGoals.GivenGoal.Type, Is.EqualTo(GoalType.ServeyBodies));
            Assert.That(sol.TryGetEntityById(shipGoals.GivenGoal.TargetEntityID, out var target), Is.True);
            Assert.That(target!.GetDataBlob<SystemBodyInfoDB>().BodyType, Is.EqualTo(BodyType.Asteroid));
            Assert.That(target.GetDataBlob<GeoSurveyableDB>().IsSurveyComplete(strata.Id), Is.False);
            double distance = ship.GetDataBlob<PositionDB>().GetDistanceTo_m(target.GetDataBlob<PositionDB>());
            Assert.That(distance, Is.LessThanOrEqualTo(ServeyBodyPlanner.AsteroidSurveyRadius_m));
        }

        var depot = Depot(sol);
        int listed = depot.GetDataBlob<LogiBaseDB>().Intel.Values.Count(row => row.Kind == IntelKind.Geo && row.ForSale);
        Assert.That(listed, Is.EqualTo(CeresStart.ChartCount));
        Assert.That(depot.GetDataBlob<GoalsDB>().ActiveGoal!.Type, Is.EqualTo(GoalType.OfferStock));
    }

    [Test]
    public void FinishedUnseededSurvey_IsListedForStrata()
    {
        var (game, sol) = Place();
        var strata = Faction(game, CeresStart.SurveyFactionName);
        var fleet = Fleet(sol, strata.Id);
        var ship = Ship(sol, strata.Id);
        var standing = fleet.GetDataBlob<GoalsDB>().ActiveGoal!;
        var depot = Depot(sol);

        var rock = sol.GetAllEntitiesWithDataBlob<SystemBodyInfoDB>()
            .First(body => body.GetDataBlob<SystemBodyInfoDB>().BodyType == BodyType.Asteroid
                && body.TryGetDataBlob<GeoSurveyableDB>(out var geo)
                && !geo.IsSurveyComplete(strata.Id));
        rock.GetDataBlob<GeoSurveyableDB>().GeoSurveyStatus[strata.Id] = 0;
        string subject = IntelBook.SubjectOf(rock.Id);
        Assert.That(IntelBook.TryGet(depot, IntelKind.Geo, subject, out _), Is.False);

        var child = new Goal(GoalType.ServeyBodies)
        {
            ParentGoalId = standing.Id,
            TargetEntityID = rock.Id,
            Status = GoalStatus.Completed,
        };
        ship.SetDataBlob(new GoalsDB { GivenGoal = child, ActiveGoal = child });

        AgentProcessor.RunAgentNow(fleet);

        Assert.That(IntelBook.TryGet(depot, IntelKind.Geo, subject, out var row), Is.True);
        Assert.That(row.ForSale, Is.True);
        Assert.That(row.Ask, Is.EqualTo(CeresStart.ChartAsk));
        Assert.That(IntelBook.SellerOf(depot, row), Is.EqualTo(strata.Id));
        Assert.That(rock.GetDataBlob<GeoSurveyableDB>().IsSurveyComplete(DepotFaction(game).Id), Is.False);
        Assert.That(fleet.GetDataBlob<GoalsDB>().ActiveGoal!.Type, Is.EqualTo(GoalType.SurveyStanding));
        Assert.That(fleet.GetDataBlob<GoalsDB>().ActiveGoal!.Status, Is.EqualTo(GoalStatus.Active));
    }

    [Test]
    public void PlacedMiningFlight_BuysOneChart_AndTheMinePlannerAcceptsIt()
    {
        var (game, sol) = Place();
        var lode = Faction(game, CeresStart.MiningFactionName);
        var fleet = Fleet(sol, lode.Id);
        var ship = Ship(sol, lode.Id);

        var standing = fleet.GetDataBlob<GoalsDB>().GivenGoal;
        Assert.That(standing, Is.SameAs(fleet.GetDataBlob<GoalsDB>().ActiveGoal));
        Assert.That(standing!.Type, Is.EqualTo(GoalType.MineStanding));
        Assert.That(standing.Status, Is.EqualTo(GoalStatus.Active));
        Assert.That(ship.GetDataBlob<AdminSpaceDB>().CommanderSeats, Is.Not.Empty);
        Assert.That(ship.GetDataBlob<AdminSpaceDB>().CommanderSeats.All(s => s.SeatType == AdminLevel.Ship), Is.True);

        var owned = sol.GetAllEntitiesWithDataBlob<GeoSurveyableDB>()
            .Where(body => body.GetDataBlob<GeoSurveyableDB>().IsSurveyComplete(lode.Id))
            .ToList();
        Assert.That(owned, Has.Count.EqualTo(1));
        Assert.That(lode.GetDataBlob<FactionInfoDB>().Money.GetCurrentFunds(), Is.EqualTo(CeresStart.StartingFunds - CeresStart.ChartAsk));

        var child = ship.GetDataBlob<GoalsDB>().GivenGoal;
        Assert.That(child, Is.Not.Null);
        Assert.That(child!.Type, Is.EqualTo(GoalType.MineAsteroids));
        Assert.That(child.ParentGoalId, Is.EqualTo(standing.Id));
        Assert.That(child.TargetEntityID, Is.EqualTo(owned[0].Id));

        var plan = new MineAsteroidsPlan().Plan(ship, new Goal(GoalType.MineAsteroids) { TargetEntityID = owned[0].Id }, ship.StarSysDateTime);
        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active), plan.Message);
        Assert.That(plan.Actions.OfType<AsteroidMineOrder>().Single().Target.Id, Is.EqualTo(owned[0].Id));
    }

    [Test]
    public void EmptyBelt_LeavesTheStandingMineGoalActive()
    {
        var (game, sol) = Place();
        var lode = Faction(game, CeresStart.MiningFactionName);
        var fleet = Fleet(sol, lode.Id);
        var ship = Ship(sol, lode.Id);
        var standing = fleet.GetDataBlob<GoalsDB>().ActiveGoal!;

        var money = lode.GetDataBlob<FactionInfoDB>().Money;
        money.AddExpense(fleet.StarSysDateTime, TransactionCategory.Trade, "test", money.GetCurrentFunds());
        foreach (var body in sol.GetAllEntitiesWithDataBlob<GeoSurveyableDB>())
            body.GetDataBlob<GeoSurveyableDB>().GeoSurveyStatus.Remove(lode.Id);

        var child = new Goal(GoalType.MineAsteroids)
        {
            ParentGoalId = standing.Id,
            Status = GoalStatus.Completed,
        };
        ship.SetDataBlob(new GoalsDB { GivenGoal = child, ActiveGoal = child });

        AgentProcessor.RunAgentNow(fleet);

        Assert.That(fleet.GetDataBlob<GoalsDB>().GivenGoal, Is.SameAs(standing));
        Assert.That(standing.Type, Is.EqualTo(GoalType.MineStanding));
        Assert.That(standing.Status, Is.EqualTo(GoalStatus.Active));
        Assert.That(money.GetCurrentFunds(), Is.EqualTo(0));
        Assert.That(sol.GetAllEntitiesWithDataBlob<GeoSurveyableDB>().Any(body => body.GetDataBlob<GeoSurveyableDB>().IsSurveyComplete(lode.Id)), Is.False);
    }

    [Test]
    public void PlayerMineAndSurvey_StillCompleteWhenTheWorkIsDone()
    {
        var (game, sol) = Place();
        var strata = Faction(game, CeresStart.SurveyFactionName);
        var lode = Faction(game, CeresStart.MiningFactionName);
        var surveyShip = Ship(sol, strata.Id);
        var miner = Ship(sol, lode.Id);

        int seeded = Depot(sol).GetDataBlob<LogiBaseDB>().Intel.Values
            .Where(row => row.Kind == IntelKind.Geo)
            .Select(row => int.Parse(row.Subject))
            .First();
        var surveyed = new ServeyBodyPlanner().Plan(
            surveyShip,
            new Goal(GoalType.ServeyBodies) { TargetEntityID = seeded },
            surveyShip.StarSysDateTime);
        Assert.That(surveyed.Status, Is.EqualTo(GoalStatus.Completed), surveyed.Message);

        foreach (var body in sol.GetAllEntitiesWithDataBlob<GeoSurveyableDB>())
            body.GetDataBlob<GeoSurveyableDB>().GeoSurveyStatus.Remove(lode.Id);
        var mined = new MineAsteroidsPlan().Plan(miner, new Goal(GoalType.MineAsteroids), miner.StarSysDateTime);
        Assert.That(mined.Status, Is.EqualTo(GoalStatus.Completed), mined.Message);

        var fleet = Fleet(sol, lode.Id);
        var playerJob = new Goal(GoalType.MineAsteroids) { Status = GoalStatus.Active };
        fleet.SetDataBlob(new GoalsDB { GivenGoal = playerJob, ActiveGoal = playerJob });
        var doneChild = new Goal(GoalType.MineAsteroids)
        {
            ParentGoalId = playerJob.Id,
            Status = GoalStatus.Completed,
        };
        miner.SetDataBlob(new GoalsDB { GivenGoal = doneChild, ActiveGoal = doneChild });
        AgentProcessor.RunAgentNow(fleet);
        Assert.That(playerJob.Status, Is.EqualTo(GoalStatus.Completed));
    }

    [Test]
    public void CompletedChild_DoesNotGiveTheShipANewGoal()
    {
        var (game, sol) = Place();
        var strata = Faction(game, CeresStart.SurveyFactionName);
        var fleet = Fleet(sol, strata.Id);
        var ship = Ship(sol, strata.Id);
        var standing = fleet.GetDataBlob<GoalsDB>().ActiveGoal!;
        int seeded = int.Parse(Depot(sol).GetDataBlob<LogiBaseDB>().Intel.Values.First(row => row.Kind == IntelKind.Geo).Subject);

        var child = new Goal(GoalType.ServeyBodies)
        {
            ParentGoalId = standing.Id,
            TargetEntityID = seeded,
            Status = GoalStatus.Active,
        };
        ship.SetDataBlob(new GoalsDB { GivenGoal = child, ActiveGoal = child });

        AgentProcessor.RunAgentNow(ship);
        Assert.That(ship.GetDataBlob<GoalsDB>().GivenGoal, Is.SameAs(child));
        Assert.That(child.Status, Is.EqualTo(GoalStatus.Completed));
        Assert.That(child.Type, Is.EqualTo(GoalType.ServeyBodies));

        AgentProcessor.RunAgentNow(ship);
        Assert.That(ship.GetDataBlob<GoalsDB>().GivenGoal, Is.SameAs(child));
        Assert.That(ship.GetDataBlob<GoalsDB>().ActiveGoal, Is.SameAs(child));
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

    static Entity DepotFaction(Game game) => Faction(game, CeresStart.DepotFactionName);

    static Entity Fleet(StarSystem sol, int factionId)
        => sol.GetAllEntitiesWithDataBlob<FleetDB>().Single(f => f.FactionOwnerID == factionId);

    static Entity Ship(StarSystem sol, int factionId)
        => sol.GetAllEntitiesWithDataBlob<ShipInfoDB>().Single(s => s.FactionOwnerID == factionId);

    static Entity Depot(StarSystem sol)
        => sol.GetAllEntitiesWithDataBlob<ColonyInfoDB>().Single(c => c.GetDataBlob<NameDB>().DefaultName == CeresStart.DepotFactionName);
}
