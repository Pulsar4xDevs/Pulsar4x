using System;
using System.Collections.Generic;
using GameEngine.Engine.Orders;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Events;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Modding;
using Pulsar4X.Names;

namespace Pulsar4X.Tests;

/// <summary>
/// Pass 3: an event or one-shot order pause records why the clock stopped, on the time pulse,
/// per faction, and the projector exposes it on the faction snapshot. Resuming clears it.
/// </summary>
[TestFixture]
public class PauseReasonTests
{
    Game _game;
    Entity _faction;
    StarSystem _system;
    CommandTranslator _translator;
    FactionEventLog _playerLog;
    GameProjector _projector;

    [SetUp]
    public void SetUp()
    {
        var store = new ModDataStore();
        new ModLoader().LoadModManifest("Data/basemod/modInfo.json", store);
        _game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);
        _game.Settings.EnforceSingleThread = true;
        _game.Settings.EnableMultiThreading = false;

        _faction = FactionFactory.CreateBasicFaction(_game, "United Earth Corp", "UEC", 1000);
        _playerLog = (FactionEventLog)_faction.GetDataBlob<FactionInfoDB>().EventLog;
        _translator = new CommandTranslator(_game);
        _projector = new GameProjector(_game);

        _system = new StarSystem();
        _system.Initialize(_game, "Sol");
        _system.SetActivityState(SystemActivityState.Foreground);
        _system.IncrementExternalObserver(priority: true);
    }

    [TearDown]
    public void TearDown()
    {
        _game?.TimePulse.PauseTime();
    }

    [Test]
    public void Standing_event_pause_records_type_message_and_entity()
    {
        var ship = Ship("Endeavour");
        DateTime at = _game.TimePulse.GameGlobalDateTime;
        Assert.That(_playerLog.HaltsOn(EventType.WarpExited), Is.True);

        var ev = Publish(EventType.WarpExited, at, "Endeavour arrived at Mars", ship);

        var reason = _game.TimePulse.GetPauseReason(_faction.Id);
        Assert.That(reason, Is.Not.Null);
        Assert.That(reason!.Event, Is.SameAs(ev));
        Assert.That(reason.OrderName, Is.Null);
        Assert.That(reason.OthersCount, Is.EqualTo(0));
        Assert.That(_game.TimePulse.GetPauseReason(_faction.Id + 100000), Is.Null, "other factions have no reason");

        var snapshot = _projector.ProjectFaction(_faction.Id)!.PauseReason;
        Assert.That(snapshot, Is.Not.Null);
        Assert.That(snapshot!.EventType, Is.EqualTo(nameof(EventType.WarpExited)));
        Assert.That(snapshot.Message, Is.EqualTo("Endeavour arrived at Mars"));
        Assert.That(snapshot.StarDate, Is.EqualTo(at));
        Assert.That(snapshot.EntityId, Is.EqualTo(ship.Id));
        Assert.That(snapshot.EntityName, Is.EqualTo("Endeavour"));
        Assert.That(snapshot.SystemId, Is.EqualTo(_system.ID));
        Assert.That(snapshot.OthersCount, Is.EqualTo(0));
    }

    [Test]
    public void Event_that_does_not_halt_records_no_reason()
    {
        var ship = Ship("Endeavour");
        _playerLog.SetHaltsOn(EventType.WarpExited, false);

        Publish(EventType.WarpExited, _game.TimePulse.GameGlobalDateTime, "Endeavour exited warp", ship);

        Assert.That(_game.TimePulse.GetPauseReason(_faction.Id), Is.Null);
    }

    [Test]
    public void OneShot_order_pause_records_the_order()
    {
        DateTime start = _game.TimePulse.GameGlobalDateTime;
        var ship = Ship("Endeavour");
        var action = new ProbeAction(ship, start + TimeSpan.FromMinutes(10));
        Assert.That(_game.OrderHandler.HandleOrder(action), Is.True);
        SetPause(ship, action, true);
        // A standing OrdersHalt pause sees the same event first; it must not count twice.
        _playerLog.SetHaltsOn(EventType.OrdersHalt, true);

        _game.TimePulse.TimeStep();

        Assert.That(_game.TimePulse.LastFault, Is.Null, _game.TimePulse.LastFault?.Report);
        Assert.That(action.Runs, Is.EqualTo(1));
        var reason = _game.TimePulse.GetPauseReason(_faction.Id);
        Assert.That(reason, Is.Not.Null);
        Assert.That(reason!.Event.EventType, Is.EqualTo(EventType.OrdersHalt));
        Assert.That(reason.OrderName, Is.EqualTo("Probe"));
        Assert.That(reason.Event.EntityId, Is.EqualTo(ship.Id));
        Assert.That(reason.Event.StarDate, Is.EqualTo(action.ActionOnDate));
        Assert.That(reason.OthersCount, Is.EqualTo(0), "the one event is one pause");

        var snapshot = _projector.ProjectFaction(_faction.Id)!.PauseReason;
        Assert.That(snapshot!.OrderName, Is.EqualTo("Probe"));
        Assert.That(snapshot.EntityName, Is.EqualTo("Endeavour"));
    }

    [Test]
    public void Several_pauses_at_one_instant_keep_the_first_and_count_the_others()
    {
        var a = Ship("Endeavour");
        var b = Ship("Resolute");
        DateTime at = _game.TimePulse.GameGlobalDateTime;

        var first = Publish(EventType.WarpExited, at, "Endeavour arrived at Mars", a);
        Publish(EventType.WarpExited, at, "Resolute arrived at Mars", b);
        Publish(EventType.ResearchCompleted, at, "Research done", null);

        var reason = _game.TimePulse.GetPauseReason(_faction.Id);
        Assert.That(reason!.Event, Is.SameAs(first));
        Assert.That(reason.OthersCount, Is.EqualTo(2));
        var snapshot = _projector.ProjectFaction(_faction.Id)!.PauseReason;
        Assert.That(snapshot!.EntityName, Is.EqualTo("Endeavour"));
        Assert.That(snapshot.OthersCount, Is.EqualTo(2));
    }

    [Test]
    public void Resuming_time_clears_the_reason()
    {
        var ship = Ship("Endeavour");
        Publish(EventType.WarpExited, _game.TimePulse.GameGlobalDateTime, "Endeavour arrived at Mars", ship);
        Assert.That(_game.TimePulse.GetPauseReason(_faction.Id), Is.Not.Null);

        _game.TimePulse.TimeStep();

        Assert.That(_game.TimePulse.LastFault, Is.Null, _game.TimePulse.LastFault?.Report);
        Assert.That(_game.TimePulse.GetPauseReason(_faction.Id), Is.Null);
        Assert.That(_projector.ProjectFaction(_faction.Id)!.PauseReason, Is.Null);

        Publish(EventType.WarpExited, _game.TimePulse.GameGlobalDateTime, "Endeavour arrived at Mars", ship);
        Assert.That(_game.TimePulse.GetPauseReason(_faction.Id), Is.Not.Null);

        _game.TimePulse.StartTime();
        _game.TimePulse.PauseTime();
        _game.TimePulse.WaitUntilStopped();

        Assert.That(_game.TimePulse.GetPauseReason(_faction.Id), Is.Null, "start clears it too");
    }

    [Test]
    public void Manual_pause_has_no_reason()
    {
        _game.TimePulse.TimeStep();
        _game.TimePulse.StartTime();
        _game.TimePulse.PauseTime();
        _game.TimePulse.WaitUntilStopped();

        Assert.That(_game.TimePulse.IsRunning, Is.False);
        Assert.That(_game.TimePulse.GetPauseReason(_faction.Id), Is.Null);
        Assert.That(_projector.ProjectFaction(_faction.Id)!.PauseReason, Is.Null);
    }

    Event Publish(EventType type, DateTime at, string message, Entity? entity)
    {
        var ev = Event.Create(type, at, message, _faction.Id, entity != null ? _system.ID : null, entity?.Id);
        EventManager.Instance.Publish(ev);
        return ev;
    }

    Entity Ship(string name)
    {
        var ship = Entity.Create();
        ship.FactionOwnerID = _faction.Id;
        _system.AddEntity(ship, new List<BaseDataBlob> { new NameDB(name), new ActionQueueDB() });
        return ship;
    }

    void SetPause(Entity ship, EntityAction action, bool pause)
    {
        var result = _translator.Translate(_faction, ship, new SetOrderPauseCommand(ship.Id, action.CmdID, pause));
        Assert.That(result.Accepted, Is.True, result.RejectionReason);
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
}
