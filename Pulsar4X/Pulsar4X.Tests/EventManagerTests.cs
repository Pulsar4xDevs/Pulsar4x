using System;
using System.Collections.Generic;
using NUnit.Framework;
using Pulsar4X.Events;

namespace Pulsar4X.Tests;

[TestFixture]
public class EventManagerTests
{
    [SetUp]
    public void SetUp()
    {
        EventManager.Instance.Clear();
    }

    [TearDown]
    public void TearDown()
    {
        EventManager.Instance.Clear();
    }

    [Test]
    public void ResearchCompleted_subscriber_does_not_receive_GeoSurveyCompleted()
    {
        var received = new List<EventType>();
        void OnEvent(Event e) => received.Add(e.EventType);

        EventManager.Instance.Subscribe(EventType.ResearchCompleted, OnEvent);
        EventManager.Instance.Publish(Event.Create(EventType.GeoSurveyCompleted, DateTime.UtcNow, "geo"));
        EventManager.Instance.Publish(Event.Create(EventType.FuelExhausted, DateTime.UtcNow, "fuel"));
        EventManager.Instance.Publish(Event.Create(EventType.ResearchCompleted, DateTime.UtcNow, "research"));

        Assert.That(received, Is.EqualTo(new[] { EventType.ResearchCompleted }));
    }

    [Test]
    public void SubscribeAll_receives_both_types_and_unsubscribe_stops_delivery()
    {
        var received = new List<EventType>();
        void OnEvent(Event e) => received.Add(e.EventType);

        EventManager.Instance.SubscribeAll(OnEvent);
        EventManager.Instance.Publish(Event.Create(EventType.ResearchCompleted, DateTime.UtcNow, "research"));
        EventManager.Instance.Publish(Event.Create(EventType.GeoSurveyCompleted, DateTime.UtcNow, "geo"));
        Assert.That(received, Is.EqualTo(new[] { EventType.ResearchCompleted, EventType.GeoSurveyCompleted }));

        EventManager.Instance.UnsubscribeAll(OnEvent);
        EventManager.Instance.Publish(Event.Create(EventType.FuelExhausted, DateTime.UtcNow, "fuel"));
        Assert.That(received, Is.EqualTo(new[] { EventType.ResearchCompleted, EventType.GeoSurveyCompleted }));
    }
}
