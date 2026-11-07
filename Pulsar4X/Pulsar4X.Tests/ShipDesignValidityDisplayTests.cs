using System.Linq;
using NUnit.Framework;
using Pulsar4X.Client;

namespace Pulsar4X.Tests;

[TestFixture]
public class ShipDesignValidityDisplayTests
{
    [TestCase(0, "Design mass")]
    [TestCase(1, "Newtonian thrust")]
    [TestCase(2, "Thrust-to-mass ratio")]
    [TestCase(3, "Add a working generator")]
    [TestCase(4, "Energy storage")]
    public void Every_failed_rule_has_an_explanation_including_negative_and_NaN(int field, string label)
    {
        foreach (double invalid in new[] { 0d, -1d, double.NaN })
        {
            var stats = new[] { 1d, 1d, 1d, 1d, 1d };
            stats[field] = invalid;
            var reasons = ShipDesignWindow.GetInvalidDesignReasons(stats[0], stats[1], stats[2], stats[3], stats[4]);
            Assert.That(reasons, Has.Count.EqualTo(1));
            Assert.That(reasons.Single(), Does.StartWith(label));
        }
    }

    [Test]
    public void Empty_design_explains_all_failed_rules_including_undefined_thrust_ratio()
    {
        var reasons = ShipDesignWindow.GetInvalidDesignReasons(0, 0, double.NaN, 0, 0);
        Assert.That(reasons, Has.Count.EqualTo(5));
    }

    [Test]
    public void Positive_stats_need_no_warp_or_cargo_handling_to_be_valid()
    {
        Assert.That(ShipDesignWindow.GetInvalidDesignReasons(100, 1000, 0.1, 15, 1000), Is.Empty);
    }

    [Test]
    public void Solar_collector_satisfies_generation_but_not_storage_or_thrust()
    {
        Assert.That(ShipDesignWindow.GetInvalidDesignReasons(100, 1000, 0.1, 0, 1000, 100), Is.Empty);
        Assert.That(ShipDesignWindow.GetInvalidDesignReasons(100, 1000, 0.1, 0, 0, 100).Single(),
            Does.StartWith("Energy storage"));
        Assert.That(ShipDesignWindow.GetInvalidDesignReasons(100, 0, 0, 0, 1000, 100), Has.Count.EqualTo(2));
    }

    [Test]
    public void Positive_infinity_preserves_the_existing_strict_positive_predicate()
    {
        Assert.That(ShipDesignWindow.GetInvalidDesignReasons(double.PositiveInfinity,
            double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity), Is.Empty);
    }
}
