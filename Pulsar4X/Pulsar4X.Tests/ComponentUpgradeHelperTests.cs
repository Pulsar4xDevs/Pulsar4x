using System;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;

namespace Pulsar4X.Tests;

[TestFixture]
public class ComponentUpgradeHelperTests
{
    [Test]
    public void ClassifyStat_LowerIsBetter_IncludesCostsAndDraw()
    {
        Assert.That(ComponentUpgradeHelper.ClassifyStat("Mass"), Is.EqualTo(StatDirection.LowerIsBetter));
        Assert.That(ComponentUpgradeHelper.ClassifyStat(" volume "), Is.EqualTo(StatDirection.LowerIsBetter));
        Assert.That(ComponentUpgradeHelper.ClassifyStat("Crew Required"), Is.EqualTo(StatDirection.LowerIsBetter));
        Assert.That(ComponentUpgradeHelper.ClassifyStat("Cost"), Is.EqualTo(StatDirection.LowerIsBetter));
        Assert.That(ComponentUpgradeHelper.ClassifyStat("Research"), Is.EqualTo(StatDirection.LowerIsBetter));
        Assert.That(ComponentUpgradeHelper.ClassifyStat("Production"), Is.EqualTo(StatDirection.LowerIsBetter));
        Assert.That(ComponentUpgradeHelper.ClassifyStat("Build energy"), Is.EqualTo(StatDirection.LowerIsBetter));
        Assert.That(ComponentUpgradeHelper.ClassifyStat("Power Draw"), Is.EqualTo(StatDirection.LowerIsBetter));
        Assert.That(ComponentUpgradeHelper.ClassifyStat("Fuel Consumption"), Is.EqualTo(StatDirection.LowerIsBetter));
        Assert.That(ComponentUpgradeHelper.ClassifyStat("Bubble Sustain Energy Cost"), Is.EqualTo(StatDirection.LowerIsBetter));
        Assert.That(ComponentUpgradeHelper.ClassifyStat("Thermal Output"), Is.EqualTo(StatDirection.LowerIsBetter));
    }

    [Test]
    public void ClassifyStat_HigherIsBetter_IncludesOutputAndCapacity()
    {
        Assert.That(ComponentUpgradeHelper.ClassifyStat("Power Output"), Is.EqualTo(StatDirection.HigherIsBetter));
        Assert.That(ComponentUpgradeHelper.ClassifyStat("power output"), Is.EqualTo(StatDirection.HigherIsBetter));
        Assert.That(ComponentUpgradeHelper.ClassifyStat("Energy Storage"), Is.EqualTo(StatDirection.HigherIsBetter));
        Assert.That(ComponentUpgradeHelper.ClassifyStat("Thrust"), Is.EqualTo(StatDirection.HigherIsBetter));
        Assert.That(ComponentUpgradeHelper.ClassifyStat("Conversion Efficency"), Is.EqualTo(StatDirection.HigherIsBetter));
        Assert.That(ComponentUpgradeHelper.ClassifyStat("MiningAmount"), Is.EqualTo(StatDirection.HigherIsBetter));
        Assert.That(ComponentUpgradeHelper.ClassifyStat("Tank Volume"), Is.EqualTo(StatDirection.HigherIsBetter));
        Assert.That(ComponentUpgradeHelper.ClassifyStat("Input Light"), Is.EqualTo(StatDirection.HigherIsBetter));
    }

    [Test]
    public void ClassifyStat_ResourceCosts_AreAlwaysLowerIsBetter()
    {
        Assert.That(ComponentUpgradeHelper.ClassifyStat("Stainless Steel", isResourceCost: true),
            Is.EqualTo(StatDirection.LowerIsBetter));
        Assert.That(ComponentUpgradeHelper.ClassifyStat("Power Output", isResourceCost: true),
            Is.EqualTo(StatDirection.LowerIsBetter));
    }

    [Test]
    public void ClassifyStat_UnknownAndTrimmedNeutralLabels_AreNeutral()
    {
        Assert.That(ComponentUpgradeHelper.ClassifyStat("Area "), Is.EqualTo(StatDirection.Neutral));
        Assert.That(ComponentUpgradeHelper.ClassifyStat("Area"), Is.EqualTo(StatDirection.Neutral));
        Assert.That(ComponentUpgradeHelper.ClassifyStat("Fuel"), Is.EqualTo(StatDirection.Neutral));
        Assert.That(ComponentUpgradeHelper.ClassifyStat("Radiator Thickness"), Is.EqualTo(StatDirection.Neutral));
        Assert.That(ComponentUpgradeHelper.ClassifyStat("Radiator area"), Is.EqualTo(StatDirection.Neutral));
        Assert.That(ComponentUpgradeHelper.ClassifyStat("Operating Temprature"), Is.EqualTo(StatDirection.Neutral));
        Assert.That(ComponentUpgradeHelper.ClassifyStat("Beam waist"), Is.EqualTo(StatDirection.Neutral));
        Assert.That(ComponentUpgradeHelper.ClassifyStat("Fuel Heating Value"), Is.EqualTo(StatDirection.Neutral));
        Assert.That(ComponentUpgradeHelper.ClassifyStat("Hostility"), Is.EqualTo(StatDirection.Neutral));
        Assert.That(ComponentUpgradeHelper.ClassifyStat(""), Is.EqualTo(StatDirection.Neutral));
        Assert.That(ComponentUpgradeHelper.ClassifyStat(null), Is.EqualTo(StatDirection.Neutral));
    }

    [Test]
    public void IsChanged_UsesRelativeToleranceBasedOnOldValue()
    {
        Assert.That(ComponentUpgradeHelper.Tolerance(0), Is.EqualTo(1e-9));
        Assert.That(ComponentUpgradeHelper.Tolerance(1e9), Is.EqualTo(1000).Within(1e-9));

        // 1e9 * 1e-6 = 1000, so a +100 tick is noise and a +2000 tick is a real change.
        Assert.That(ComponentUpgradeHelper.IsChanged(1e9, 1e9 + 100), Is.False);
        Assert.That(ComponentUpgradeHelper.IsChanged(1e9, 1e9 + 2000), Is.True);

        // Near zero, the floor of 1e-9 applies.
        Assert.That(ComponentUpgradeHelper.IsChanged(0, 1e-12), Is.False);
        Assert.That(ComponentUpgradeHelper.IsChanged(0, 1e-8), Is.True);
    }

    [Test]
    public void CompareValues_DetectsBetterWorseNeutralAndUnchanged()
    {
        var betterPower = ComponentUpgradeHelper.CompareValues("Power Output", 10, 25, "kW");
        Assert.That(betterPower.Change, Is.EqualTo(StatChange.Better));
        Assert.That(betterPower.Direction, Is.EqualTo(StatDirection.HigherIsBetter));

        var worsePower = ComponentUpgradeHelper.CompareValues("Power Output", 25, 10, "kW");
        Assert.That(worsePower.Change, Is.EqualTo(StatChange.Worse));

        var betterMass = ComponentUpgradeHelper.CompareValues("Mass", 200, 150, "kg");
        Assert.That(betterMass.Change, Is.EqualTo(StatChange.Better));

        var worseMass = ComponentUpgradeHelper.CompareValues("Mass", 200, 250, "kg");
        Assert.That(worseMass.Change, Is.EqualTo(StatChange.Worse));

        var neutral = ComponentUpgradeHelper.CompareValues("Hostility", 1, 4);
        Assert.That(neutral.Change, Is.EqualTo(StatChange.Neutral));
        Assert.That(neutral.Direction, Is.EqualTo(StatDirection.Neutral));

        var unchanged = ComponentUpgradeHelper.CompareValues("Power Output", 15, 15 + 1e-12, "kW");
        Assert.That(unchanged.Change, Is.EqualTo(StatChange.Unchanged));
    }

    [Test]
    public void IsUpgradeable_RequiresAtLeastOneStrictImprovement()
    {
        var improvedAndWorse = new[]
        {
            ComponentUpgradeHelper.CompareValues("Power Output", 10, 25, "kW"),
            ComponentUpgradeHelper.CompareValues("Mass", 200, 250, "kg"),
        };
        Assert.That(ComponentUpgradeHelper.IsUpgradeable(improvedAndWorse), Is.True);
        Assert.That(ComponentUpgradeHelper.ImprovedDeltas(improvedAndWorse), Has.Count.EqualTo(1));

        var worseOnly = new[] { ComponentUpgradeHelper.CompareValues("Mass", 200, 250, "kg") };
        Assert.That(ComponentUpgradeHelper.IsUpgradeable(worseOnly), Is.False);

        var neutralOnly = new[] { ComponentUpgradeHelper.CompareValues("Hostility", 1, 4) };
        Assert.That(ComponentUpgradeHelper.IsUpgradeable(neutralOnly), Is.False);

        var unchangedOnly = new[] { ComponentUpgradeHelper.CompareValues("Power Output", 15, 15, "kW") };
        Assert.That(ComponentUpgradeHelper.IsUpgradeable(unchangedOnly), Is.False);
    }

    [Test]
    public void FormatValue_NormalizesPowerEnergyDistanceAndSmallWatts()
    {
        Assert.That(ComponentUpgradeHelper.FormatValue(15, "kW"), Is.EqualTo(Stringify.Power(15)));
        Assert.That(ComponentUpgradeHelper.FormatValue(15, "Kw"), Is.EqualTo(Stringify.Power(15)));
        Assert.That(ComponentUpgradeHelper.FormatValue(15, "KW"), Is.EqualTo(Stringify.Power(15)));

        Assert.That(ComponentUpgradeHelper.FormatValue(12, "kJ"), Is.EqualTo(Stringify.Energy(12)));
        Assert.That(ComponentUpgradeHelper.FormatValue(12, "KJ"), Is.EqualTo(Stringify.Energy(12)));

        Assert.That(ComponentUpgradeHelper.FormatValue(5, "m"), Is.EqualTo(Stringify.Distance(5)));
        Assert.That(ComponentUpgradeHelper.FormatValue(5, "M"), Is.EqualTo(Stringify.Distance(5)));

        // "m^2" must not be treated as a distance in metres.
        Assert.That(ComponentUpgradeHelper.FormatValue(4, "m^2"), Is.EqualTo(Stringify.Area(4)));

        Assert.That(ComponentUpgradeHelper.FormatValue(0.001, "W"), Is.EqualTo("0.001 W"));
        Assert.That(ComponentUpgradeHelper.FormatValue(1.2345, "W"), Is.EqualTo(1.2345.ToString("G4") + " W"));
    }

    [Test]
    public void FormatSignedDelta_UsesCleanSignAndLeavesPercentUnescaped()
    {
        Assert.That(ComponentUpgradeHelper.FormatSignedDelta(15, "kW"),
            Is.EqualTo("(+ " + Stringify.Power(15) + ")"));
        Assert.That(ComponentUpgradeHelper.FormatSignedDelta(-0.75, "Kw"),
            Is.EqualTo("(- " + Stringify.Power(0.75) + ")"));
        Assert.That(ComponentUpgradeHelper.FormatSignedDelta(-200, "kg"),
            Is.EqualTo("(- " + Stringify.Mass(200) + ")"));

        // '%' must be a literal character (ImGui tooltips use TextUnformatted, not printf).
        string percent = ComponentUpgradeHelper.FormatSignedDelta(5, "%");
        Assert.That(percent, Does.Contain("%"));
        Assert.That(percent, Does.Not.Contain("%%"));
        Assert.That(percent, Does.StartWith("(+ "));
    }
}
