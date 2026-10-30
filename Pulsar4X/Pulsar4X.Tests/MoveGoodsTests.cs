using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.Engine.Orders;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Factions;
using Pulsar4X.Fleets;
using Pulsar4X.Galaxy;
using Pulsar4X.Logistics;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Orbital;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

[TestFixture]
public class MoveGoodsTests
{
    [Test]
    public void OneTrip_LoadsTheHold_AndDoesNotTouchTheBook()
    {
        var scene = Scene.Build(holdUnits: 10, sourceStock: 50, sell: 50);
        var goal = scene.Goal(50);
        var plan = new MoveGoodsPlan().Plan(scene.Ship, goal, scene.Ship.StarSysDateTime);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active));
        Assert.That(plan.Actions, Has.Count.GreaterThan(0));
        Assert.That(plan.Actions.OfType<CargoTransferOrder>().Any(), Is.False);
        Assert.That(goal.MoveGoodsLeg, Is.EqualTo(MoveGoodsLeg.Load));

        scene.Source.GetDataBlob<CargoStorageDB>().TransferRangeDv_mps = 1e12;
        plan = new MoveGoodsPlan().Plan(scene.Ship, goal, scene.Ship.StarSysDateTime);

        Assert.That(plan.Actions.OfType<CargoTransferOrder>().Count(), Is.EqualTo(1));
        Assert.That(scene.Source.GetDataBlob<CargoStorageDB>().GetUnitsStored(scene.Iron, false), Is.EqualTo(40));
        Assert.That(MarketBook.TryGet(scene.Source, "iron", out var listing), Is.True);
        Assert.That(listing.SellQuantity, Is.EqualTo(50));

        Scene.Drain(scene.Source);
        Assert.That(scene.Ship.GetDataBlob<CargoStorageDB>().GetUnitsStored(scene.Iron, false), Is.EqualTo(10));

        plan = new MoveGoodsPlan().Plan(scene.Ship, goal, scene.Ship.StarSysDateTime);
        Assert.That(goal.MoveGoodsLeg, Is.EqualTo(MoveGoodsLeg.Unload));
        Assert.That(plan.Actions.OfType<CargoTransferOrder>().Any(), Is.False);
        Assert.That(plan.Actions, Has.Count.GreaterThan(0));

        scene.Dest.GetDataBlob<CargoStorageDB>().TransferRangeDv_mps = 1e12;
        plan = new MoveGoodsPlan().Plan(scene.Ship, goal, scene.Ship.StarSysDateTime);
        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active));
        Assert.That(goal.MoveGoodsUnloaded, Is.True);

        Scene.Drain(scene.Dest);
        plan = new MoveGoodsPlan().Plan(scene.Ship, goal, scene.Ship.StarSysDateTime);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Completed));
        Assert.That(plan.Message, Does.Contain("40"));
        Assert.That(plan.Message, Does.Contain("0"));
        Assert.That(scene.Dest.GetDataBlob<CargoStorageDB>().GetUnitsStored(scene.Iron, false), Is.EqualTo(10));
        Assert.That(listing.SellQuantity, Is.EqualTo(50));
    }

    [Test]
    public void Cancel_ReturnsEscrowToTheSource()
    {
        var scene = Scene.Build(holdUnits: 10, sourceStock: 50, sell: 0);
        scene.Source.GetDataBlob<CargoStorageDB>().TransferRangeDv_mps = 1e12;
        var goal = scene.Goal(50);
        var plan = new MoveGoodsPlan().Plan(scene.Ship, goal, scene.Ship.StarSysDateTime);
        var transfer = plan.Actions.OfType<CargoTransferOrder>().Single();

        transfer.Stop(scene.Ship.StarSysDateTime);

        var source = scene.Source.GetDataBlob<CargoStorageDB>();
        Assert.That(source.GetUnitsStored(scene.Iron, false), Is.EqualTo(50));
        Assert.That(source.EscroItems, Is.Empty);
        Assert.That(scene.Ship.GetDataBlob<CargoStorageDB>().EscroItems, Is.Empty);
    }

    [Test]
    public void FullDestination_LeavesTheLoadAboard()
    {
        var scene = Scene.Build(holdUnits: 10, sourceStock: 50, sell: 0, destVolume: 0);
        scene.Source.GetDataBlob<CargoStorageDB>().TransferRangeDv_mps = 1e12;
        var goal = scene.Goal(50);
        var plan = new MoveGoodsPlan().Plan(scene.Ship, goal, scene.Ship.StarSysDateTime);
        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active));
        Scene.Drain(scene.Source);

        scene.Dest.GetDataBlob<CargoStorageDB>().TransferRangeDv_mps = 1e12;
        plan = new MoveGoodsPlan().Plan(scene.Ship, goal, scene.Ship.StarSysDateTime);
        Assert.That(goal.MoveGoodsLeg, Is.EqualTo(MoveGoodsLeg.Unload));
        plan = new MoveGoodsPlan().Plan(scene.Ship, goal, scene.Ship.StarSysDateTime);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Failed));
        Assert.That(plan.Message, Is.EqualTo("no place to unload"));
        Assert.That(scene.Ship.GetDataBlob<CargoStorageDB>().GetUnitsStored(scene.Iron, false), Is.EqualTo(10));
        Assert.That(scene.Dest.GetDataBlob<CargoStorageDB>().GetUnitsStored(scene.Iron, false), Is.EqualTo(0));
    }

    [Test]
    public void ComponentInstance_ArrivesAsTheSameObject()
    {
        var scene = Scene.Build(holdUnits: 10, sourceStock: 0, sell: 0);
        var design = new ComponentDesign
        {
            Name = "Crate",
            CargoTypeID = "general-storage",
            VolumePerUnit = 1,
            MassPerUnit = 1,
            Density = 1,
        };
        var crate = new ComponentInstance(design);
        var hold = scene.Ship.GetDataBlob<CargoStorageDB>().TypeStores["general-storage"];
        hold.MaxVolume = 10;
        hold.FreeVolume = 10;
        Assert.That(scene.Source.GetDataBlob<CargoStorageDB>().AddCargoByUnit(crate, 1), Is.EqualTo(1));

        scene.Source.GetDataBlob<CargoStorageDB>().TransferRangeDv_mps = 1e12;
        var goal = scene.Goal(1, crate.ID);
        var plan = new MoveGoodsPlan().Plan(scene.Ship, goal, scene.Ship.StarSysDateTime);
        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active));
        Scene.Drain(scene.Source);

        plan = new MoveGoodsPlan().Plan(scene.Ship, goal, scene.Ship.StarSysDateTime);
        Assert.That(goal.MoveGoodsLeg, Is.EqualTo(MoveGoodsLeg.Unload));
        scene.Dest.GetDataBlob<CargoStorageDB>().TransferRangeDv_mps = 1e12;
        plan = new MoveGoodsPlan().Plan(scene.Ship, goal, scene.Ship.StarSysDateTime);
        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active));
        Scene.Drain(scene.Dest);
        plan = new MoveGoodsPlan().Plan(scene.Ship, goal, scene.Ship.StarSysDateTime);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Completed));
        Assert.That(scene.Source.GetDataBlob<CargoStorageDB>().GetUnitsStored(crate, false), Is.EqualTo(0));
        var stored = scene.Dest.GetDataBlob<CargoStorageDB>().TypeStores["general-storage"].GetCargoables();
        Assert.That(stored.ContainsKey(crate.ID), Is.True);
        Assert.That(ReferenceEquals(stored[crate.ID], crate), Is.True);
    }

    [Test]
    public void CaptainedShip_RefuelAt_AssignsTheGoal()
    {
        var scene = Scene.Build(holdUnits: 10, sourceStock: 50, sell: 0);
        var result = new CommandTranslator(scene.Game).Translate(
            scene.Faction, scene.Ship, new RefuelAtCommand(scene.Ship.Id, scene.Source.Id));

        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        var goal = scene.Ship.GetDataBlob<GoalsDB>().GivenGoal;
        Assert.That(goal, Is.Not.Null);
        Assert.That(goal!.Type, Is.EqualTo(GoalType.RefuelAt));
        Assert.That(goal.TargetEntityID, Is.EqualTo(scene.Source.Id));
        Assert.That(goal.Name, Is.EqualTo("Refuel"));
    }

    [Test]
    public void EmptyChair_RefuelAt_IsRejected()
    {
        var scene = Scene.Build(holdUnits: 10, sourceStock: 50, sell: 0, commanderId: -1);
        var result = new CommandTranslator(scene.Game).Translate(
            scene.Faction, scene.Ship, new RefuelAtCommand(scene.Ship.Id, scene.Source.Id));

        Assert.That(result.Accepted, Is.False);
        Assert.That(result.RejectionReason, Does.Contain("captain"));
        Assert.That(scene.Ship.TryGetDataBlob<GoalsDB>(out var goals) && goals.GivenGoal != null, Is.False);
    }

    [Test]
    public void EmptyChair_LoadFuel_UsesTheLoad()
    {
        var scene = Scene.Build(holdUnits: 10, sourceStock: 50, sell: 0, commanderId: -1);
        scene.Source.GetDataBlob<CargoStorageDB>().TransferRangeDv_mps = 1e12;
        var items = new List<CargoTransferItem> { new(scene.Iron.ID, 50) };
        var result = new CommandTranslator(scene.Game).Translate(
            scene.Faction, scene.Ship, new LoadGoodsCommand(scene.Ship.Id, scene.Source.Id, items));

        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        var load = scene.Ship.GetDataBlob<ActionQueueDB>().ActionList.OfType<LoadGoodsAction>().Single();
        Assert.That(load.IsValidCommand(scene.Game), Is.True);
        load.Execute(scene.Ship.StarSysDateTime);
        Assert.That(load.Status, Is.EqualTo(ActionStatus.Running), load.FailureMessage);
        Assert.That(Scene.Log(scene.Ship), Does.Contain("Load 50 Iron from Colony."));
        Scene.Drain(scene.Source);
        Assert.That(load.IsFinished(), Is.True);
        Assert.That(scene.Ship.GetDataBlob<CargoStorageDB>().GetUnitsStored(scene.Iron, false), Is.EqualTo(10));
        Assert.That(scene.Source.GetDataBlob<CargoStorageDB>().GetUnitsStored(scene.Iron, false), Is.EqualTo(40));
        Assert.That(Scene.Log(scene.Ship), Does.Contain("Loaded 10 Iron from Colony."));
        Assert.That(Scene.Log(scene.Ship), Has.No.Member("Loaded 50 Iron from Colony."));

        var unload = new CommandTranslator(scene.Game).Translate(
            scene.Faction, scene.Ship, new UnloadGoodsCommand(
                scene.Ship.Id, scene.Source.Id, new[] { new CargoTransferItem(scene.Iron.ID, 10) }));
        Assert.That(unload.Accepted, Is.True, unload.RejectionReason);
        Assert.That(Scene.Log(scene.Ship), Does.Contain("Unload 10 Iron at Colony."));
        var put = scene.Ship.GetDataBlob<ActionQueueDB>().ActionList.OfType<UnloadGoodsAction>().Single();
        scene.Ship.GetDataBlob<CargoStorageDB>().TransferRangeDv_mps = 1e12;
        Scene.Drain(scene.Ship);
        Assert.That(put.IsFinished(), Is.True);
        Assert.That(Scene.Log(scene.Ship), Does.Contain("Unloaded 10 Iron at Colony."));
    }

    [Test]
    public void EmptyChair_QueuesTheFlight_AndAnOutOfRangeLoadEscrowsNothing()
    {
        var scene = Scene.Build(holdUnits: 10, sourceStock: 50, sell: 0, commanderId: -1);
        var items = new List<CargoTransferItem> { new(scene.Iron.ID, 50) };
        var result = new CommandTranslator(scene.Game).Translate(
            scene.Faction, scene.Ship, new QueueMoveGoodsCommand(scene.Ship.Id, scene.Source.Id, scene.Dest.Id, items));

        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        Assert.That(scene.Ship.TryGetDataBlob<GoalsDB>(out var goals) && goals.GivenGoal != null, Is.False);

        var queue = scene.Ship.GetDataBlob<ActionQueueDB>().ActionList;
        Assert.That(queue.Count, Is.GreaterThan(1));
        Assert.That(queue.Count(action => action is LoadGoodsAction), Is.EqualTo(1));
        var load = queue.OfType<LoadGoodsAction>().Single();
        Assert.That(queue[queue.Count - 1], Is.SameAs(load));

        Assert.That(load.IsValidCommand(scene.Game), Is.True);
        load.Execute(scene.Ship.StarSysDateTime);

        Assert.That(load.Status, Is.EqualTo(ActionStatus.Failed));
        Assert.That(load.FailureMessage, Is.EqualTo("out of range"));
        Assert.That(scene.Source.GetDataBlob<CargoStorageDB>().GetUnitsStored(scene.Iron, false), Is.EqualTo(50));
        Assert.That(scene.Source.GetDataBlob<CargoStorageDB>().EscroItems, Is.Empty);
        Assert.That(Scene.Log(scene.Ship), Has.No.Member("Loaded 50 Iron from Colony."));
        Assert.That(Scene.Log(scene.Ship).Exists(line => line.StartsWith("Loaded ")), Is.False);
    }

    [Test]
    public void Fleet_HandsTheCaptainedCargoShip()
    {
        var scene = Scene.Build(holdUnits: 10, sourceStock: 50, sell: 0);
        var empty = scene.ShipAt(scene.SourcePos + new Vector3(1e9, 0, 0), commanderId: -1);
        var captain = scene.ShipAt(new Vector3(4e11, 0, 0), commanderId: 1);
        var fleet = Entity.Create(scene.Faction.Id);
        scene.System.AddEntity(fleet, new List<BaseDataBlob> { new FleetDB() });
        var fleetDB = fleet.GetDataBlob<FleetDB>();
        fleetDB.AddChild(empty);
        fleetDB.AddChild(captain);

        var goal = new Goal(GoalType.FleetMoveGoods)
        {
            Name = "Move goods",
            SourceEntityId = scene.Source.Id,
            DestEntityId = scene.Dest.Id,
            MoveGoodsLines = new List<MoveGoodsLine>
            {
                new MoveGoodsLine { ItemId = scene.Iron.ID, Requested = 50 },
            },
        };
        var plan = new FleetMoveGoodsPlan().Plan(fleet, goal, fleet.StarSysDateTime);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active));
        Assert.That(plan.SubGoals, Has.Count.EqualTo(1));
        Assert.That(plan.SubGoals[0].Sub.Id, Is.EqualTo(captain.Id));
        Assert.That(plan.SubGoals[0].Goal.Type, Is.EqualTo(GoalType.MoveGoods));
        Assert.That(plan.SubGoals[0].Goal.ParentGoalId, Is.EqualTo(goal.Id));
    }

    [Test]
    public void OtherFactionsColony_IsRejected()
    {
        var scene = Scene.Build(holdUnits: 10, sourceStock: 50, sell: 0, commanderId: 1);
        var other = FactionFactory.CreateFaction(scene.Game, "Others");
        var foreign = scene.Colony(other, new Vector3(2e11, 0, 0), stock: 0, volume: 1000, sell: 0);
        var items = new List<CargoTransferItem> { new(scene.Iron.ID, 10) };
        var result = new CommandTranslator(scene.Game).Translate(
            scene.Faction,
            scene.Ship,
            new MoveGoodsCommand(scene.Ship.Id, scene.Source.Id, foreign.Id, items));

        Assert.That(result.Accepted, Is.False);
        Assert.That(result.RejectionReason, Is.EqualTo("The colony is not yours."));
    }

    sealed class Scene
    {
        public Game Game = null!;
        public StarSystem System = null!;
        public Entity Faction = null!;
        public Entity Star = null!;
        public Entity Ship = null!;
        public Entity Source = null!;
        public Entity Dest = null!;
        public ICargoable Iron = null!;
        public Vector3 SourcePos;

        public Goal Goal(long units, int? itemId = null)
        {
            return new Goal(GoalType.MoveGoods)
            {
                Name = "Move goods",
                SourceEntityId = Source.Id,
                DestEntityId = Dest.Id,
                MoveGoodsLines = new List<MoveGoodsLine>
                {
                    new MoveGoodsLine { ItemId = itemId ?? Iron.ID, Requested = units },
                },
            };
        }

        public static Scene Build(
            int holdUnits, long sourceStock, long sell, double destVolume = 100000, int commanderId = 1)
        {
            var modLoader = new ModLoader();
            var store = new ModDataStore();
            modLoader.LoadModManifest("Data/basemod/modInfo.json", store);
            var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);

            var faction = FactionFactory.CreateFaction(game, "Haulers");
            faction.GetDataBlob<FactionInfoDB>().Data.Unlock("iron");
            var iron = faction.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetAny("iron");

            var system = new StarSystem();
            system.Initialize(game, "Goods", -1);
            var star = Entity.Create();
            system.AddEntity(star, new List<BaseDataBlob>
            {
                new PositionDB(0, 0, 0),
                MassVolumeDB.NewFromMassAndRadius_m(1.989e30, 6.96342e8),
            });

            var scene = new Scene
            {
                Game = game,
                System = system,
                Faction = faction,
                Star = star,
                Iron = iron,
                SourcePos = new Vector3(1.496e11, 0, 0),
            };
            scene.Source = scene.Colony(faction, scene.SourcePos, sourceStock, 100000, sell);
            scene.Dest = scene.Colony(faction, new Vector3(2.28e11, 0, 0), 0, destVolume, sell: 0);
            scene.Ship = scene.ShipAt(new Vector3(5e11, 0, 0), commanderId, holdUnits);
            Assert.That(scene.Ship.GetDataBlob<CargoStorageDB>().GetFreeUnitSpace(iron), Is.EqualTo(holdUnits));
            return scene;
        }

        public Entity ShipAt(Vector3 at, int commanderId, int holdUnits = 10)
        {
            double hold = Iron.VolumePerUnit * (holdUnits + 0.5);
            var info = new ShipInfoDB { CommanderID = commanderId };
            var ship = Entity.Create(Faction.Id);
            System.AddEntity(ship, new List<BaseDataBlob>
            {
                info,
                new CargoStorageDB("general-storage", hold),
                new PositionDB(at, Star),
                new ActionQueueDB(),
                new WarpAbilityDB { MaxSpeed = 1e9 },
                new NewtonThrustAbilityDB("iron"),
                new NewtonMoveDB(Star, Vector3.Zero),
                MassVolumeDB.NewFromMassAndRadius_m(1e6, 20),
            });
            return ship;
        }

        public Entity Colony(Entity owner, Vector3 at, long stock, double volume, long sell)
        {
            var colony = Entity.Create(owner.Id);
            var cargo = new CargoStorageDB("general-storage", volume);
            var planet = Entity.Create();
            var body = new SystemBodyInfoDB { LengthOfDay = TimeSpan.Zero };
            System.AddEntity(planet, new List<BaseDataBlob>
            {
                new NameDB("Body"),
                body,
                new PositionDB(at, Star),
                MassVolumeDB.NewFromMassAndRadius_m(1e24, 6e6),
            });
            var colonyInfo = new ColonyInfoDB { PlanetEntity = planet };
            var blobs = new List<BaseDataBlob>
            {
                new NameDB("Colony"),
                colonyInfo,
                cargo,
                new PositionDB(at, Star),
                MassVolumeDB.NewFromMassAndRadius_m(1e24, 6e6),
                new ActionQueueDB(),
            };
            if (sell > 0)
                blobs.Add(new LogiBaseDB { Capacity = 5 });
            System.AddEntity(colony, blobs);
            if (stock > 0)
                Assert.That(cargo.AddCargoByUnit(Iron, stock), Is.EqualTo(stock));
            if (sell > 0)
            {
                Assert.That(MarketBook.SetListing(colony, new MarketListing
                {
                    CargoId = Iron.UniqueID,
                    SellQuantity = sell,
                    Ask = 10,
                }), Is.True);
            }
            return colony;
        }

        public static List<string> Log(Entity entity)
            => entity.GetDataBlob<ShipLogDB>().Entries.Select(entry => entry.Text).ToList();

        public static void Drain(Entity holder)
        {
            var store = holder.GetDataBlob<CargoStorageDB>();
            // Rate times seconds is computed as a 32-bit int. Keep the product positive.
            store.TransferRate = 1_000_000;
            var blob = holder.GetDataBlob<CargoTransferDB>();
            var processor = new CargoTransferProcessor();
            for (int i = 0; i < 8; i++)
            {
                processor.ProcessEntity(blob, 10);
                if (Empty(blob))
                    return;
            }
            Assert.That(Empty(blob), Is.True, "transfer escrow did not clear");
        }

        static bool Empty(CargoTransferDB blob)
        {
            foreach (var tup in blob.TransferData.EscroHeldInPrimary)
            {
                if (tup.count > 0)
                    return false;
            }
            foreach (var tup in blob.TransferData.EscroHeldInSecondary)
            {
                if (tup.count > 0)
                    return false;
            }
            return true;
        }
    }
}
