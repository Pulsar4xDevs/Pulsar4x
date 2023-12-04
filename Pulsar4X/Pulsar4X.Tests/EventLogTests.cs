using System;
using System.Collections.Generic;
using NUnit.Framework;
using Pulsar4X.Engine;
using Pulsar4X.Events;
using Pulsar4X.Modding;

namespace Pulsar4X.Tests;

[TestFixture]
public class EventLogTests
{
    FactionEventLog? _factionOneEventLog;
    FactionEventLog? _factionTwoEventLog;
    SpaceMasterEventLog? _spaceMasterEventLog;
    HaltEventLog? _haltOnEventLog;
    Game? _game;

    [SetUp]
    public void Setup()
    {
        var modLoader = new ModLoader();
        var modDataStore = new ModDataStore();

        modLoader.LoadModManifest("Data/basemod/modInfo.json", modDataStore);

        var settings = new NewGameSettings() {
            StartDateTime = new System.DateTime(2100, 9, 1)
        };

        _game  = new Game(settings, modDataStore);

        EventManager.Instance.Clear();
        _factionOneEventLog = FactionEventLog.Create(1);
        _factionOneEventLog.Subscribe();

        _factionTwoEventLog = FactionEventLog.Create(2);
        _factionTwoEventLog.Subscribe();
        _spaceMasterEventLog = SpaceMasterEventLog.Create();
        _spaceMasterEventLog.Subscribe();

        _haltOnEventLog = HaltEventLog.Create(new List<EventType>() { EventType.GlobalDateChange, EventType.SystemDateChange }, _game.TimePulse);
        _haltOnEventLog.Subscribe();
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
    public void TestHaltEventLog()
    {
        if(_haltOnEventLog == null)
            throw new NullReferenceException();

        Event e = Event.Create(EventType.ActiveContactLost, DateTime.Now, "Testing Events");
        EventManager.Instance.Publish(e);

        Assert.AreEqual(0, _haltOnEventLog.GetEvents().Count);

        e = Event.Create(EventType.GlobalDateChange, DateTime.Now, "Testing Events");
        EventManager.Instance.Publish(e);

        Assert.AreEqual(1, _haltOnEventLog.GetEvents().Count);

        e = Event.Create(EventType.SystemDateChange, DateTime.Now, "Testing Events");
        EventManager.Instance.Publish(e);

        Assert.AreEqual(2, _haltOnEventLog.GetEvents().Count);
    }
}