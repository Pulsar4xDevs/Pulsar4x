using System;
using System.Collections.Generic;
using NUnit.Framework;
using Pulsar4X.Components;
using Pulsar4X.Energy;
using Pulsar4X.Sensors;
using Pulsar4X.Ships;
using Pulsar4X.Client;

namespace Pulsar4X.Tests;

[TestFixture]
public class SolarDesignCapabilityTests
{
    [TestCase(100, 20, 10, 300, 1, 100)]
    [TestCase(100, 20, 10, 300, 3, 300)]
    [TestCase(100, 20, 10, 300, 0, 0)]
    [TestCase(0, 20, 10, 300, 1, 0)]
    [TestCase(100, 0, 0, 300, 1, 0)]
    [TestCase(100, 20, 10, 0, 1, 0)]
    public void UI_and_server_share_counted_capable_collector_area_not_watts(
        double area, double best, double worst, double bandwidth, int count, double expectedArea)
    {
        var component = new ComponentDesign();
        component.AttributesByType[typeof(EnergySolarGenerationAtb)] =
            new EnergySolarGenerationAtb(550, bandwidth, best, worst, area);
        var collectorArea = ShipDesign.GetSolarCollectionArea(new[] { (component, count) });
        Assert.That(collectorArea, Is.EqualTo(expectedArea));
        var reasons = ShipDesignWindow.GetInvalidDesignReasons(100, 1000, 0.1, 0, 1000, collectorArea);
        Assert.That(reasons.Count, Is.EqualTo(expectedArea > 0 ? 0 : 1));
        // Adding a conventional generator doesn't add its watts to the solar-area metric.
        component.AttributesByType[typeof(EnergyGenerationAtb)] =
            new EnergyGenerationAtb("fissile-fuels", 1, "electricity", 15000, 1000);
        Assert.That(ShipDesign.GetSolarCollectionArea(new[] { (component, count) }), Is.EqualTo(expectedArea));
    }

    [TestCase(double.NaN, 20, 10, 300)]
    [TestCase(double.PositiveInfinity, 20, 10, 300)]
    [TestCase(-1, 20, 10, 300)]
    [TestCase(100, double.NaN, 10, 300)]
    [TestCase(100, 20, double.PositiveInfinity, 300)]
    [TestCase(100, -1, 10, 300)]
    [TestCase(100, 20, -1, 300)]
    [TestCase(100, 20, 10, -1)]
    [TestCase(100, 20, 10, double.NaN)]
    public void Positive_count_malformed_panel_cannot_hide_behind_valid_panel(
        double area, double best, double worst, double bandwidth)
    {
        var good = Panel(100, 20, 10, 300);
        var bad = Panel(area, best, worst, bandwidth);
        var collection = ShipDesign.GetSolarCollectionArea(new[] { (good, 1), (bad, 1) });
        Assert.That(double.IsNaN(collection), Is.True, "NaN is the invalid-solar-data sentinel");
        Assert.That(double.IsNaN(ShipDesign.GetSolarCollectionArea(new[] { (bad, 1) })), Is.True);
        Assert.That(ShipDesignWindow.GetInvalidDesignReasons(100, 1000, 0.1, 0, 1000, collection),
            Has.Count.EqualTo(1));
        Assert.That(ShipDesign.GetSolarCollectionArea(new[] { (good, 1), (bad, 0) }), Is.EqualTo(100));
        Assert.That(ShipDesignWindow.GetInvalidDesignReasons(100, 1000, 0.1, 15, 1000, collection), Is.Empty,
            "conventional-generator semantics are unchanged even if solar cannot be assessed");
    }

    [Test]
    public void Area_product_and_sum_overflow_fail_closed()
    {
        var panel = Panel(double.MaxValue, 20, 10, 300);
        Assert.That(double.IsNaN(ShipDesign.GetSolarCollectionArea(new[] { (panel, 2) })), Is.True);
        Assert.That(double.IsNaN(ShipDesign.GetSolarCollectionArea(new[] { (panel, 1), (panel, 1) })), Is.True);
    }

    [Test]
    public void Multiple_finite_types_and_zero_conversion_panels_are_not_malformed()
    {
        Assert.That(ShipDesign.GetSolarCollectionArea(new[] {
            (Panel(100, 20, 10, 300), 2), (Panel(40, 10, 5, 200), 3),
            (Panel(100, 0, 0, 300), 1), (Panel(0, 20, 10, 300), 1),
            (Panel(100, 20, 10, 0), 1) }), Is.EqualTo(320));
    }

    private static ComponentDesign Panel(double area, double best, double worst, double bandwidth)
    {
        var component = new ComponentDesign();
        component.AttributesByType[typeof(EnergySolarGenerationAtb)] =
            new EnergySolarGenerationAtb(550, bandwidth, best, worst, area);
        return component;
    }

    [Test]
    public void Runtime_solar_power_still_depends_on_illumination_not_design_area_alone()
    {
        var panel = new EnergySolarGenerationAtb(550, 300, 20, 10, 100);
        Assert.That(EnergyGenHotloopProcessor.AbsorbedPower(panel, new List<EMData>()), Is.Zero);
        var illuminated = new List<EMData> { new() { WaveForm = new EMWaveForm(400, 550, 700), Magnitude = 1 } };
        Assert.That(EnergyGenHotloopProcessor.AbsorbedPower(panel, illuminated), Is.EqualTo(20).Within(1e-9));
    }
}
