using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.Engine.Orders;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.Datablobs;
using Pulsar4X.Energy;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.JumpPoints;
using Pulsar4X.Logistics;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Orbital;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

[TestFixture]
public class InterSystemRouteTests
{
    [Test]
    public void KnownJump_FirstActionsTargetTheGate()
    {
        var scene = Scene.Build(farPair: true);

        Assert.That(scene.Trade().Accepted, Is.True);
        var goal = scene.Ship.GetDataBlob<GoalsDB>().ActiveGoal;
        Assert.That(goal!.SourceEntityId, Is.EqualTo(scene.FarSell.Id));
        Assert.That(goal.DestEntityId, Is.EqualTo(scene.FarBuy.Id));

        var queued = scene.Ship.GetDataBlob<ActionQueueDB>().ActionList;
        Assert.That(queued, Is.Not.Empty);
        Assert.That(queued[0], Is.InstanceOf<WarpMoveAction>());
        Assert.That(((WarpMoveAction)queued[0]).TargetEntityGuid, Is.EqualTo(scene.GateHere.Id));
        Assert.That(((WarpMoveAction)queued[0]).TargetEntityGuid, Is.Not.EqualTo(scene.FarSell.Id));
        Assert.That(((WarpMoveAction)queued[0]).TargetEntityGuid, Is.Not.EqualTo(scene.FarBuy.Id));
    }

    [Test]
    public void UnknownSystem_IsNotChosen()
    {
        var scene = Scene.Build(farPair: true, hiddenPair: true);

        Assert.That(scene.Trade().Accepted, Is.True);
        var goal = scene.Ship.GetDataBlob<GoalsDB>().ActiveGoal;
        Assert.That(goal!.SourceEntityId, Is.EqualTo(scene.FarSell.Id));
        Assert.That(goal.DestEntityId, Is.EqualTo(scene.FarBuy.Id));
        Assert.That(goal.SourceEntityId, Is.Not.EqualTo(scene.HiddenSell.Id));
        Assert.That(goal.DestEntityId, Is.Not.EqualTo(scene.HiddenBuy.Id));
    }

    [Test]
    public void NoPath_SkipsTheFarPair_AndKeepsTheLocalOne()
    {
        var scene = Scene.Build(localPair: true, unlinkedPair: true);

        var traded = scene.Trade();
        Assert.That(traded.Accepted, Is.True, traded.RejectionReason);
        var goal = scene.Ship.GetDataBlob<GoalsDB>().ActiveGoal;
        Assert.That(goal!.SourceEntityId, Is.EqualTo(scene.LocalSell.Id));
        Assert.That(goal.DestEntityId, Is.EqualTo(scene.LocalBuy.Id));
        Assert.That(goal.SourceEntityId, Is.Not.EqualTo(scene.UnlinkedSell.Id));
        Assert.That(goal.DestEntityId, Is.Not.EqualTo(scene.UnlinkedBuy.Id));
    }

    [Test]
    public void Freighter_KnownJump_FirstActionsTargetTheGate()
    {
        var scene = Scene.Build(farColony: true);
        var result = new CommandTranslator(scene.Game).Translate(scene.Faction, scene.Ship, new FreighterCommand(scene.Ship.Id));

        Assert.That(result.Accepted, Is.True);
        var goal = scene.Ship.GetDataBlob<GoalsDB>().ActiveGoal;
        Assert.That(goal!.Type, Is.EqualTo(GoalType.Freighter));
        Assert.That(goal.SourceEntityId, Is.EqualTo(scene.FarColony.Id));

        var queued = scene.Ship.GetDataBlob<ActionQueueDB>().ActionList;
        Assert.That(queued[0], Is.InstanceOf<WarpMoveAction>());
        Assert.That(((WarpMoveAction)queued[0]).TargetEntityGuid, Is.EqualTo(scene.GateHere.Id));
    }

    [Test]
    public void ArrivalAtJumpPoint_DoesNotThrow()
    {
        var modLoader = new ModLoader();
        var store = new ModDataStore();
        modLoader.LoadModManifest("Data/basemod/modInfo.json", store);
        var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);
        var system = new StarSystem();
        system.Initialize(game, "Arrival", -1);

        var star = Entity.Create();
        system.AddEntity(star, new List<BaseDataBlob>
        {
            new PositionDB(0, 0, 0),
            MassVolumeDB.NewFromMassAndRadius_m(1.989e30, 6.96342e8),
        });

        var gate = Entity.Create(Game.NeutralFactionId);
        system.AddEntity(gate, new List<BaseDataBlob>
        {
            new PositionDB(new Vector3(3e11, 0, 0), star),
            new NameDB("Gate"),
        });

        var epoch = system.StarSysDateTime;
        var ship = Entity.Create();
        var warp = new WarpAbilityDB { MaxSpeed = 1e7, EnergyType = "electricity" };
        var energy = new EnergyGenAbilityDB(epoch) { EnergyType = new TestEnergyType() };
        energy.EnergyStored["electricity"] = 1e12;
        energy.EnergyStoreMax["electricity"] = 1e12;
        system.AddEntity(ship, new List<BaseDataBlob>
        {
            new PositionDB(new Vector3(1.496e11, 0, 0), star),
            MassVolumeDB.NewFromMassAndRadius_m(1e6, 20),
            warp,
            energy,
            new NameDB("Hauler"),
        });

        var moveDB = new WarpMovingDB(ship, gate, Vector3.Zero, default);
        ship.SetDataBlob(moveDB);
        var pos = ship.GetDataBlob<PositionDB>();
        pos.SetParent(pos.Root);
        moveDB.HasStarted = true;
        moveDB.LastProcessDateTime = epoch;
        moveDB._position = (Vector2)pos.AbsolutePosition;
        moveDB.ExitPointAbsolute = gate.GetDataBlob<PositionDB>().AbsolutePosition;
        moveDB.ExitPointrelative = Vector3.Zero;
        moveDB.PredictedExitTime = epoch + TimeSpan.FromMinutes(5);
        var delta = moveDB.ExitPointAbsolute - (Vector3)moveDB._position;
        moveDB.CurrentNonNewtonionVectorMS = Vector3.Normalise(delta) * (delta.Length() / TimeSpan.FromMinutes(5).TotalSeconds);

        Assert.That(gate.GetDataBlob<PositionDB>().MoveType, Is.EqualTo(PositionDB.MoveTypes.None));
        Assert.DoesNotThrow(() => WarpMoveProcessor.WarpMove(ship, moveDB, moveDB.PredictedExitTime));
        Assert.That(ship.HasDataBlob<WarpMovingDB>(), Is.False);
        Assert.That(ship.GetDataBlob<PositionDB>().Parent, Is.EqualTo(gate));
        Assert.That(ship.GetDataBlob<PositionDB>().MoveType, Is.EqualTo(PositionDB.MoveTypes.None));
    }

    sealed class TestEnergyType : ICargoable
    {
        public int ID => 0;
        public string UniqueID => "electricity";
        public string CargoTypeID => "energy";
        public string Name => "electricity";
        public long MassPerUnit => 0;
        public double VolumePerUnit => 0;
    }

    sealed class Scene
    {
        public Game Game = null!;
        public Entity Faction = null!;
        public Entity Ship = null!;
        public Entity GateHere = null!;
        public Entity LocalSell = null!;
        public Entity LocalBuy = null!;
        public Entity FarSell = null!;
        public Entity FarBuy = null!;
        public Entity HiddenSell = null!;
        public Entity HiddenBuy = null!;
        public Entity UnlinkedSell = null!;
        public Entity UnlinkedBuy = null!;
        public Entity FarColony = null!;

        public CommandResult Trade()
            => new CommandTranslator(Game).Translate(Faction, Ship, new TradeCommand(Ship.Id));

        public static Scene Build(bool localPair = false, bool farPair = false, bool hiddenPair = false, bool unlinkedPair = false, bool farColony = false)
        {
            var modLoader = new ModLoader();
            var store = new ModDataStore();
            modLoader.LoadModManifest("Data/basemod/modInfo.json", store);
            var game = new Game(new NewGameSettings { MaxSystems = 4, CreatePlayerFaction = false }, store);
            var faction = FactionFactory.CreateFaction(game, "Haulers");
            faction.GetDataBlob<FactionInfoDB>().Data.Unlock("iron");
            var info = faction.GetDataBlob<FactionInfoDB>();

            var here = SystemWithStar(game, "Here");
            var there = SystemWithStar(game, "There");
            var gateHere = Gate(here.System, here.Star, new Vector3(3e11, 0, 0));
            var gateThere = Gate(there.System, there.Star, new Vector3(3e11, 0, 0));
            gateHere.SetDataBlob(new JumpPointDB(gateThere));
            gateThere.SetDataBlob(new JumpPointDB(gateHere));
            info.KnownSystems.Add(here.System.ID);
            info.KnownSystems.Add(there.System.ID);
            info.RememberJumpPoint(gateHere);
            info.RememberJumpPoint(gateThere);

            var ship = Entity.Create(faction.Id);
            here.System.AddEntity(ship, new List<BaseDataBlob>
            {
                new ShipInfoDB(),
                new CargoStorageDB("general-storage", 100000),
                new PositionDB(new Vector3(1.496e11, 0, 0), here.Star),
                new ActionQueueDB(),
                new WarpAbilityDB { MaxSpeed = 1e9 },
                new NewtonThrustAbilityDB("iron"),
                new NewtonMoveDB(here.Star, Vector3.Zero),
                MassVolumeDB.NewFromMassAndRadius_m(1e6, 20),
            });

            var scene = new Scene
            {
                Game = game,
                Faction = faction,
                Ship = ship,
                GateHere = gateHere,
            };

            if (localPair)
            {
                scene.LocalSell = Market(here.System, faction, here.Star, new Vector3(1.496e11, 0, 0), sell: 30, ask: 10, buy: 0, bid: 0, stock: 40);
                scene.LocalBuy = Market(here.System, faction, here.Star, new Vector3(2.28e11, 0, 0), sell: 0, ask: 0, buy: 30, bid: 12, stock: 0);
            }

            if (farPair)
            {
                scene.FarSell = Market(there.System, faction, there.Star, new Vector3(1e11, 0, 0), sell: 30, ask: 10, buy: 0, bid: 0, stock: 40);
                scene.FarBuy = Market(there.System, faction, there.Star, new Vector3(2e11, 0, 0), sell: 0, ask: 0, buy: 30, bid: 40, stock: 0);
            }

            if (hiddenPair)
            {
                var hidden = SystemWithStar(game, "Hidden");
                scene.HiddenSell = Market(hidden.System, faction, hidden.Star, new Vector3(1e11, 0, 0), sell: 30, ask: 1, buy: 0, bid: 0, stock: 40);
                scene.HiddenBuy = Market(hidden.System, faction, hidden.Star, new Vector3(2e11, 0, 0), sell: 0, ask: 0, buy: 30, bid: 1000, stock: 0);
            }

            if (unlinkedPair)
            {
                var loose = SystemWithStar(game, "Unlinked");
                info.KnownSystems.Add(loose.System.ID);
                scene.UnlinkedSell = Market(loose.System, faction, loose.Star, new Vector3(1e11, 0, 0), sell: 30, ask: 1, buy: 0, bid: 0, stock: 40);
                scene.UnlinkedBuy = Market(loose.System, faction, loose.Star, new Vector3(2e11, 0, 0), sell: 0, ask: 0, buy: 30, bid: 1000, stock: 0);
            }

            if (farColony)
            {
                scene.FarColony = Colony(there.System, faction, there.Star, new Vector3(1e11, 0, 0), sell: 80, ask: 10, buy: 0, bid: 0, stock: 80);
                Colony(here.System, faction, here.Star, new Vector3(2.28e11, 0, 0), sell: 0, ask: 0, buy: 80, bid: 12, stock: 0);
            }

            return scene;
        }

        static (StarSystem System, Entity Star) SystemWithStar(Game game, string name)
        {
            var system = new StarSystem();
            system.Initialize(game, name, -1);
            var star = Entity.Create();
            system.AddEntity(star, new List<BaseDataBlob>
            {
                new PositionDB(0, 0, 0),
                MassVolumeDB.NewFromMassAndRadius_m(1.989e30, 6.96342e8),
            });
            return (system, star);
        }

        static Entity Gate(StarSystem system, Entity star, Vector3 at)
        {
            var gate = Entity.Create(Game.NeutralFactionId);
            system.AddEntity(gate, new List<BaseDataBlob>
            {
                new PositionDB(at, star),
                new NameDB("Gate"),
            });
            return gate;
        }

        static Entity Market(StarSystem system, Entity owner, Entity star, Vector3 at, long sell, decimal ask, long buy, decimal bid, long stock)
        {
            var market = Entity.Create(owner.Id);
            var cargo = new CargoStorageDB("general-storage", 100000);
            system.AddEntity(market, new List<BaseDataBlob>
            {
                cargo,
                new LogiBaseDB { Capacity = 5 },
                new PositionDB(at, star),
                MassVolumeDB.NewFromMassAndRadius_m(1e24, 6e6),
                new ActionQueueDB(),
            });
            if (stock > 0)
            {
                var good = owner.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetAny("iron");
                Assert.That(cargo.AddCargoByUnit(good, stock), Is.EqualTo(stock));
            }
            Assert.That(MarketBook.SetListing(market, new MarketListing
            {
                CargoId = "iron",
                SellQuantity = sell,
                Ask = ask,
                BuyQuantity = buy,
                Bid = bid,
            }), Is.True);
            return market;
        }

        static Entity Colony(StarSystem system, Entity owner, Entity star, Vector3 at, long sell, decimal ask, long buy, decimal bid, long stock)
        {
            var colony = Market(system, owner, star, at, sell, ask, buy, bid, stock);
            colony.SetDataBlob(new NameDB("Colony"));
            colony.SetDataBlob(new ColonyInfoDB());
            return colony;
        }
    }
}
