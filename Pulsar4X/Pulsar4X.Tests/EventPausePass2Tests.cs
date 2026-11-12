using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.Engine.Orders;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Datablobs;
using Pulsar4X.Energy;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Events;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

/// <summary>
/// Pass 2: the pulse cap ends a long tick at the warp-exit instant, the queue
/// yields so circularise does not start in that same pass, and one-shot
/// PauseOnAction only fires for the current order.
/// </summary>
[TestFixture]
public class EventPausePass2Tests
{
    Game _game;
    Entity _faction;
    StarSystem _alpha;
    StarSystem _beta;
    CommandTranslator _translator;
    FactionEventLog _playerLog;
    List<(int entityId, DateTime at)> _dropIns;

    [SetUp]
    public void SetUp()
    {
        var store = new ModDataStore();
        new ModLoader().LoadModManifest("Data/basemod/modInfo.json", store);
        _game = new Game(new NewGameSettings { MaxSystems = 2, CreatePlayerFaction = false }, store);
        _game.Settings.EnforceSingleThread = true;
        _game.Settings.EnableMultiThreading = false;
        _game.Settings.StrictNewtonion = true;

        _faction = FactionFactory.CreateBasicFaction(_game, "United Earth Corp", "UEC", 1000);
        _playerLog = (FactionEventLog)_faction.GetDataBlob<FactionInfoDB>().EventLog;
        _translator = new CommandTranslator(_game);

        _alpha = AddSystem("Alpha");
        _beta = AddSystem("Beta");

        _dropIns = new List<(int entityId, DateTime at)>();
        WarpMoveProcessor.TestDropIn = (entity, at) => _dropIns.Add((entity.Id, at));
    }

    [TearDown]
    public void TearDown()
    {
        WarpMoveProcessor.TestDropIn = null;
        _game?.TimePulse.PauseTime();
    }

    [Test]
    public void FiveDayTick_pauses_after_warp_exit_before_circularise()
    {
        DateTime start = _game.TimePulse.GameGlobalDateTime;
        var (ship, warp, circ, exit) = StartWarpThenCircularise(_beta, TimeSpan.FromDays(2));

        Assert.That(_playerLog.HaltsOn(EventType.WarpExited), Is.True, "standing WarpExited is on by default");
        Assert.That(_playerLog.HaltsOn(EventType.OrdersCompleted), Is.False);

        _game.TimePulse.TimeStep(start + TimeSpan.FromDays(5));

        Assert.That(_game.TimePulse.LastFault, Is.Null, _game.TimePulse.LastFault?.Report);
        AssertClocks(exit);
        Assert.That(_dropIns.Count, Is.EqualTo(1), "drop-in must apply at the halt instant");
        Assert.That(_dropIns[0].at, Is.EqualTo(exit));
        Assert.That(ship.HasDataBlob<WarpMovingDB>(), Is.False, "ship has arrived");
        Assert.That(warp.IsFinished(), Is.True);
        Assert.That(circ.IsRunning, Is.False, "circularise must not start in the drop-in pass");
        Assert.That(circ.IsFinished(), Is.False);
        Assert.That(HasEvent(EventType.WarpExited), Is.True);
        Assert.That(WarpExitedAt(), Is.EqualTo(exit));

        _game.TimePulse.TimeStep(start + TimeSpan.FromDays(5));

        Assert.That(_game.TimePulse.LastFault, Is.Null, _game.TimePulse.LastFault?.Report);
        Assert.That(circ.Status, Is.Not.EqualTo(ActionStatus.Failed), circ.FailureMessage ?? circ.Details);
        Assert.That(circ.IsRunning || circ.IsFinished(), Is.True, "after unpause the circularise runs");
        Assert.That(_dropIns.Count, Is.EqualTo(1), "drop-in must not run again");
    }

    [Test]
    public void Follower_PauseOnAction_with_past_start_does_not_rehalt_during_warp()
    {
        DateTime start = _game.TimePulse.GameGlobalDateTime;
        var (ship, warp, circ, exit) = StartWarpThenCircularise(_beta, TimeSpan.FromDays(2));
        SetPause(ship, circ, true);
        Assert.That(circ.ActionOnDate, Is.LessThanOrEqualTo(start), "follower start is already in the past");
        Assert.That(circ.PauseOnAction, Is.True);
        Assert.That(circ.IsRunning, Is.False);

        // Mid-transit: a one-hour step must not consume the follower's box or halt early.
        _game.TimePulse.TimeStep(start + TimeSpan.FromHours(1));

        Assert.That(_game.TimePulse.LastFault, Is.Null, _game.TimePulse.LastFault?.Report);
        AssertClocks(start + TimeSpan.FromHours(1));
        Assert.That(ship.HasDataBlob<WarpMovingDB>(), Is.True, "still in warp");
        Assert.That(circ.IsRunning, Is.False);
        Assert.That(circ.PauseOnAction, Is.True, "box is not cleared until the follower is current");
        Assert.That(HasEvent(EventType.OrdersHalt), Is.False);

        _game.TimePulse.TimeStep(start + TimeSpan.FromDays(5));

        AssertClocks(exit);
        Assert.That(ship.HasDataBlob<WarpMovingDB>(), Is.False);
        Assert.That(circ.IsRunning, Is.False, "yield: circularise has not started at drop-in");
        Assert.That(circ.PauseOnAction, Is.True, "one-shot waits until the follower is current, after unpause");
        Assert.That(HasEvent(EventType.OrdersHalt), Is.False);
        Assert.That(HasEvent(EventType.WarpExited), Is.True);

        _game.TimePulse.TimeStep();

        Assert.That(circ.Status, Is.Not.EqualTo(ActionStatus.Failed), circ.FailureMessage ?? circ.Details);
        Assert.That(circ.IsRunning || circ.IsFinished(), Is.True);
        Assert.That(circ.PauseOnAction, Is.False, "clears after it fires");
        Assert.That(HasEvent(EventType.OrdersHalt), Is.True);
        Assert.That(_playerLog.HaltsOn(EventType.OrdersCompleted), Is.False, "one-shot does not flip the generic setting");
    }

    [Test]
    public void Standing_WarpExited_off_lets_both_orders_complete_in_the_same_step()
    {
        DateTime start = _game.TimePulse.GameGlobalDateTime;
        var (ship, warp, circ, exit) = StartWarpThenCircularise(_beta, TimeSpan.FromDays(2));
        _playerLog.SetHaltsOn(EventType.WarpExited, false);

        _game.TimePulse.TimeStep(start + TimeSpan.FromDays(5));

        Assert.That(_game.TimePulse.LastFault, Is.Null, _game.TimePulse.LastFault?.Report);
        AssertClocks(start + TimeSpan.FromDays(5));
        Assert.That(_dropIns.Count, Is.EqualTo(1));
        Assert.That(_dropIns[0].at, Is.EqualTo(exit));
        Assert.That(ship.HasDataBlob<WarpMovingDB>(), Is.False);
        Assert.That(warp.IsFinished(), Is.True);
        Assert.That(circ.Status, Is.Not.EqualTo(ActionStatus.Failed), circ.FailureMessage ?? circ.Details);
        Assert.That(circ.IsRunning || circ.IsFinished(), Is.True, "circularise starts in the same step when WarpExited is off");
        Assert.That(HasEvent(EventType.WarpExited), Is.True, "the event is still published");
    }

    [Test]
    public void OneShot_PauseOnAction_pauses_a_simple_order_with_OrdersCompleted_off()
    {
        DateTime start = _game.TimePulse.GameGlobalDateTime;
        var ship = Ship(_beta, "Scout");
        var action = new ProbeAction(ship, start + TimeSpan.FromMinutes(10));
        Assert.That(_game.OrderHandler.HandleOrder(action), Is.True);
        SetPause(ship, action, true);

        Assert.That(_playerLog.HaltsOn(EventType.OrdersCompleted), Is.False);
        Assert.That(_playerLog.HaltsOn(EventType.OrdersHalt), Is.False);

        _game.TimePulse.TimeStep();

        Assert.That(_game.TimePulse.LastFault, Is.Null, _game.TimePulse.LastFault?.Report);
        DateTime when = action.ActionOnDate;
        AssertClocks(when);
        Assert.That(action.Runs, Is.EqualTo(1), "the order starts at the halt instant");
        Assert.That(action.PauseOnAction, Is.False, "the box clears when it fires");
        Assert.That(HasEvent(EventType.OrdersHalt), Is.True);
        Assert.That(_playerLog.HaltsOn(EventType.OrdersCompleted), Is.False);

        _game.TimePulse.TimeStep();

        Assert.That(action.Runs, Is.EqualTo(1), "already finished, does not run again");
        AssertClocks(when + _game.TimePulse.Ticklength);
    }

    (Entity ship, WarpMoveAction warp, CirculariseAction circ, DateTime exit)
        StartWarpThenCircularise(StarSystem system, TimeSpan transit)
    {
        var epoch = system.StarSysDateTime;
        var sol = TestingUtilities.BasicSol(system);
        var planet = AddOrbitingBody(system, sol, 5.972e24, 6_371_000, 1.0, epoch);
        var planetAbs = (Vector3)MoveMath.GetAbsoluteFuturePosition(planet, epoch);
        var ship = AddWarpShip(system, sol, planetAbs + new Vector3(5e9, 0, 0));
        ship.FactionOwnerID = _faction.Id;
        ship.SetDataBlob(new ActionQueueDB());

        double parking = planet.GetDataBlob<MassVolumeDB>().RadiusInM + 200_000;
        var offset = new Vector3(parking, 0, 0);
        var moveDB = new WarpMovingDB(ship, planet, offset, default);
        ship.SetDataBlob(moveDB);
        var pos = ship.GetDataBlob<PositionDB>();
        pos.SetParent(pos.Root);
        moveDB.HasStarted = true;
        moveDB.LastProcessDateTime = epoch;
        moveDB._position = (Vector2)pos.AbsolutePosition;
        DateTime exitAt = epoch + transit;
        // The ctor's intercept exit is for a different transit. Pin the exit to
        // the planet at PredictedExitTime so drop-in leftover is a circular orbit
        // and CirculariseAction can Execute ("already circular" or a short burn).
        var planetAtExit = (Vector3)MoveMath.GetAbsoluteFuturePosition(planet, exitAt);
        moveDB.ExitPointrelative = offset;
        moveDB.ExitPointAbsolute = planetAtExit + offset;
        moveDB.PredictedExitTime = exitAt;
        var dest = moveDB.ExitPointAbsolute;
        double seconds = transit.TotalSeconds;
        var delta = dest - (Vector3)moveDB._position;
        moveDB.CurrentNonNewtonionVectorMS = Vector3.Normalise(delta) * (delta.Length() / seconds);

        double sgp = GeneralMath.StandardGravitationalParameter(
            ship.GetDataBlob<MassVolumeDB>().MassTotal + planet.GetDataBlob<MassVolumeDB>().MassTotal);
        double r = offset.Length();
        var tangential = Vector3.Normalise(new Vector3(-offset.Y, offset.X, 0));
        moveDB.SavedNewtonionVector = tangential * Math.Sqrt(sgp / r);
        WarpMoveProcessor.ScheduleArrival(ship, moveDB);

        var warp = WarpMoveAction.CreateWarpOnly(ship, planet, epoch);
        warp.Status = ActionStatus.Running;
        warp.Rehydrate(_game);
        Assert.That(warp.IsRunning, Is.True, "rehydrate binds the in-flight bubble");
        ship.GetDataBlob<ActionQueueDB>().ActionList.Add(warp);

        var circ = CirculariseAction.CreateCommand(ship, epoch);
        Assert.That(_game.OrderHandler.HandleOrder(circ), Is.True);
        Assert.That(circ.IsRunning, Is.False, "circularise is queued behind the warp");
        Assert.That(circ.ActionOnDate, Is.LessThanOrEqualTo(epoch));
        return (ship, warp, circ, moveDB.PredictedExitTime);
    }

    StarSystem AddSystem(string name)
    {
        var system = new StarSystem();
        system.Initialize(_game, name);
        system.SetActivityState(SystemActivityState.Foreground);
        system.IncrementExternalObserver(priority: true);
        return system;
    }

    Entity Ship(StarSystem system, string name)
    {
        var ship = Entity.Create();
        ship.FactionOwnerID = _faction.Id;
        system.AddEntity(ship, new List<BaseDataBlob> { new NameDB(name), new ActionQueueDB() });
        return ship;
    }

    Entity AddWarpShip(StarSystem system, Entity parent, Vector3 absolutePos)
    {
        var ent = Entity.Create();
        var pos = new PositionDB { AbsolutePosition = absolutePos };
        var warp = new WarpAbilityDB { MaxSpeed = 1e7, EnergyType = "electricity" };
        var energy = new EnergyGenAbilityDB(system.StarSysDateTime) { EnergyType = new TestEnergyType() };
        energy.EnergyStored["electricity"] = 1e12;
        energy.EnergyStoreMax["electricity"] = 1e12;
        system.AddEntity(ent, new BaseDataBlob[]
        {
            pos,
            MassVolumeDB.NewFromMassAndRadius_m(1e6, 20),
            warp,
            energy,
            new NameDB("Warpship")
        });
        pos.SetParent(parent);
        return ent;
    }

    Entity AddOrbitingBody(StarSystem system, Entity parent, double mass, double radius_m, double smaAu, DateTime epoch)
    {
        var parentMass = parent.GetDataBlob<MassVolumeDB>().MassDry;
        var orbit = OrbitDB.FromAsteroidFormat(parent, parentMass, mass, smaAu, 0, 0, 0, 0, 0, epoch);
        var ent = Entity.Create();
        var pos = new PositionDB();
        system.AddEntity(ent, new BaseDataBlob[]
        {
            pos,
            MassVolumeDB.NewFromMassAndRadius_m(mass, radius_m),
            orbit,
            new NameDB("Planet")
        });
        OrbitProcessor.ProcessEntity(ent, epoch);
        return ent;
    }

    void SetPause(Entity ship, EntityAction action, bool pause)
    {
        var result = _translator.Translate(_faction, ship, new SetOrderPauseCommand(ship.Id, action.CmdID, pause));
        Assert.That(result.Accepted, Is.True, result.RejectionReason);
    }

    void AssertClocks(DateTime expected)
    {
        Assert.That(_game.TimePulse.GameGlobalDateTime, Is.EqualTo(expected), "global");
        Assert.That(_alpha.StarSysDateTime, Is.EqualTo(expected), "alpha");
        Assert.That(_beta.StarSysDateTime, Is.EqualTo(expected), "beta");
    }

    bool HasEvent(EventType type)
    {
        return _playerLog.GetEvents().Any(e => e.EventType == type);
    }

    DateTime? WarpExitedAt()
    {
        return _playerLog.GetEvents().FirstOrDefault(e => e.EventType == EventType.WarpExited)?.StarDate;
    }

    sealed class ProbeAction : EntityAction
    {
        readonly Entity _entity;
        public int Runs;

        public ProbeAction(Entity entity, DateTime when)
        {
            _entity = entity;
            EntityCommandingGuid = entity.Id;
            RequestingFactionGuid = entity.FactionOwnerID;
            ActionOnDate = when;
            CreatedDate = when;
        }

        public override ActionLaneTypes ActionLanes => ActionLaneTypes.Movement;
        public override bool IsBlocking => true;
        public override string Name => "Probe";
        public override string Details => "Probe";
        internal override Entity EntityCommanding => _entity;
        internal override bool IsValidCommand(Game game) => true;
        internal override bool IsFinished() => _isFinished;

        internal override void Execute(DateTime atDateTime)
        {
            Runs++;
            IsRunning = true;
            _isFinished = true;
            Status = ActionStatus.Succeeded;
        }

        public override EntityAction Clone() => throw new NotImplementedException();
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
}
