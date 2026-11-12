using System;
using System.Threading;
using System.Collections.Generic;
using NUnit.Framework;
using Pulsar4X.Engine;
using Pulsar4X.Events;
using Pulsar4X.Factions;
using Pulsar4X.Modding;

namespace Pulsar4X.Tests;

[TestFixture]
public class EventLogTests
{
    FactionEventLog? _factionOneEventLog;
    FactionEventLog? _factionTwoEventLog;
    SpaceMasterEventLog? _spaceMasterEventLog;
    Game? _game;

    [SetUp]
    public void Setup()
    {
        var modLoader = new ModLoader();
        var modDataStore = new ModDataStore();

        modLoader.LoadModManifest("Data/basemod/modInfo.json", modDataStore);
        modLoader.LoadScenario("Data/scenarios/warp-tech-start/scenarioInfo.json", modDataStore);

        var settings = new NewGameSettings() {
            StartDateTime = new System.DateTime(2100, 9, 1)
        };

        _game  = new Game(settings, modDataStore);

        EventManager.Instance.Clear();
        _factionOneEventLog = FactionEventLog.Create(1, _game.TimePulse);
        _factionOneEventLog.Subscribe();

        _factionTwoEventLog = FactionEventLog.Create(2, _game.TimePulse);
        _factionTwoEventLog.Subscribe();
        _spaceMasterEventLog = SpaceMasterEventLog.Create();
        _spaceMasterEventLog.Subscribe();
    }

    [TearDown]
    public void TearDown()
    {
        _game?.TimePulse.PauseTime();
        _game?.TimePulse.WaitUntilStopped();
        _factionOneEventLog?.Unsubscribe();
        _factionTwoEventLog?.Unsubscribe();
        _spaceMasterEventLog?.Unsubscribe();
        EventManager.Instance.Clear();
    }

    [Test]
    public void TestFactionEventLogs()
    {
        if(_factionOneEventLog == null || _factionTwoEventLog == null)
            throw new NullReferenceException();

        Event e = Event.Create(EventType.ResearchCompleted, DateTime.Now, "Testing Events", 1);
        EventManager.Instance.Publish(e);

        Assert.AreEqual(1, _factionOneEventLog.GetEvents().Count);
        Assert.AreEqual(0, _factionTwoEventLog.GetEvents().Count);

        e = Event.Create(EventType.ResearchCompleted, DateTime.Now, "Testing Events", 2);
        EventManager.Instance.Publish(e);

        Assert.AreEqual(1, _factionOneEventLog.GetEvents().Count);
        Assert.AreEqual(1, _factionTwoEventLog.GetEvents().Count);

        e = Event.Create(EventType.ResearchCompleted, DateTime.Now, "Testing Events", 3);
        EventManager.Instance.Publish(e);

        Assert.AreEqual(1, _factionOneEventLog.GetEvents().Count);
        Assert.AreEqual(1, _factionTwoEventLog.GetEvents().Count);

        e = Event.Create(EventType.ResearchCompleted, DateTime.Now, "Testing Events");
        EventManager.Instance.Publish(e);

        Assert.AreEqual(1, _factionOneEventLog.GetEvents().Count);
        Assert.AreEqual(1, _factionTwoEventLog.GetEvents().Count);

        e = Event.Create(EventType.ResearchCompleted, DateTime.Now, "Testing Events", null, null, null, new List<int>() { 1 });
        EventManager.Instance.Publish(e);

        Assert.AreEqual(2, _factionOneEventLog.GetEvents().Count);
        Assert.AreEqual(1, _factionTwoEventLog.GetEvents().Count);

        e = Event.Create(EventType.ResearchCompleted, DateTime.Now, "Testing Events", null, null, null, new List<int>() { 1, 2 });
        EventManager.Instance.Publish(e);

        Assert.AreEqual(3, _factionOneEventLog.GetEvents().Count);
        Assert.AreEqual(2, _factionTwoEventLog.GetEvents().Count);
    }

    [Test]
    public void TestSpaceMasterEventLog()
    {
        if(_spaceMasterEventLog == null)
            throw new NullReferenceException();

        Event e = Event.Create(EventType.ResearchCompleted, DateTime.Now, "Testing Events", 1);
        EventManager.Instance.Publish(e);

        Assert.AreEqual(1, _spaceMasterEventLog.GetEvents().Count);

        e = Event.Create(EventType.ResearchCompleted, DateTime.Now, "Testing Events");
        EventManager.Instance.Publish(e);

        Assert.AreEqual(2, _spaceMasterEventLog.GetEvents().Count);
    }


    [Test]
    public void DefaultHaltOnEvents_match_the_agreed_defaults()
    {
        var defaults = new GameSettings().DefaultHaltOnEvents;
        Assert.That(defaults[EventType.ResearchCompleted], Is.True);
        Assert.That(defaults[EventType.GeoSurveyCompleted], Is.True);
        Assert.That(defaults[EventType.JumpPointSurveyCompleted], Is.True);
        Assert.That(defaults[EventType.FuelExhausted], Is.True);
        Assert.That(defaults[EventType.WarpExited], Is.True);
        Assert.That(defaults[EventType.OrdersCompleted], Is.False);
    }

    [Test]
    public void Publishing_ResearchCompleted_pauses_player_time_and_does_not_pause_space_master()
    {
        var log = FactionEventLog.Create(1, _game!.TimePulse, new Dictionary<EventType, bool>
        {
            { EventType.ResearchCompleted, true }
        });
        log.Subscribe();

        Assert.That(log.HaltsOn(EventType.ResearchCompleted), Is.True);
        Assert.That(_spaceMasterEventLog!.HaltsOn(EventType.ResearchCompleted), Is.False);
        _spaceMasterEventLog.ToggleHaltsOn(EventType.ResearchCompleted);
        Assert.That(_spaceMasterEventLog.HaltsOn(EventType.ResearchCompleted), Is.False, "space master never pauses for these");

        PreparePulse();
        _game.TimePulse.StartTime();
        Assert.That(_game.TimePulse.IsRunning, Is.True, "should be running before the event");

        EventManager.Instance.Publish(Event.Create(EventType.ResearchCompleted, DateTime.Now, "research done", 1));
        _game.TimePulse.WaitUntilStopped();
        Assert.That(_game.TimePulse.IsRunning, Is.False, "player log should pause the clock");

        log.Unsubscribe();
    }

    [Test]
    public void Toggling_ResearchCompleted_off_does_not_pause()
    {
        var log = FactionEventLog.Create(1, _game!.TimePulse, new Dictionary<EventType, bool>
        {
            { EventType.ResearchCompleted, true }
        });
        log.Subscribe();
        log.ToggleHaltsOn(EventType.ResearchCompleted);
        Assert.That(log.HaltsOn(EventType.ResearchCompleted), Is.False);

        PreparePulse();
        _game.TimePulse.StartTime();
        Assert.That(_game.TimePulse.IsRunning, Is.True);

        EventManager.Instance.Publish(Event.Create(EventType.ResearchCompleted, DateTime.Now, "research done", 1));
        Thread.Sleep(100);
        Assert.That(_game.TimePulse.IsRunning, Is.True, "clock should keep running when the type is off");

        _game.TimePulse.PauseTime();
        _game.TimePulse.WaitUntilStopped();
        log.Unsubscribe();
    }

    private void PreparePulse()
    {
        _game!.TimePulse.Ticklength = TimeSpan.FromSeconds(1);
        _game.TimePulse.TickFrequency = TimeSpan.FromMilliseconds(50);
    }
}
