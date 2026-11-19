using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;

namespace Pulsar4X.Tests;

/// <summary>
/// Decision logic for "Queue construction on completion?". No ImGui and no game.
/// Site-only is the same condition as LocalConstructionJob.IsSiteOnly.
/// </summary>
[TestFixture]
public class QueueConstructionOnCompletionTests
{
    static ConstructibleItemView Item(
        ComponentMountFlags flags,
        bool canAutoInstall,
        string reason = "")
        => new("id", "name", 1, 1, canAutoInstall)
        {
            MountFlags = flags,
            AutoInstallReason = reason,
        };

    static readonly ComponentMountFlags Crate =
        ComponentMountFlags.PlanetInstallation | ComponentMountFlags.ShipCargo;

    static readonly ComponentMountFlags SiteOnly = ComponentMountFlags.PlanetInstallation;

    [Test]
    public void Default_IsOff()
    {
        Assert.That(QueueConstructionOnCompletion.Default, Is.False);
    }

    [Test]
    public void IsSiteOnly_MatchesEngineCondition()
    {
        Assert.That(QueueConstructionOnCompletion.IsSiteOnly(SiteOnly), Is.True);
        Assert.That(QueueConstructionOnCompletion.IsSiteOnly(Crate), Is.False);
        Assert.That(QueueConstructionOnCompletion.IsSiteOnly(ComponentMountFlags.ShipCargo), Is.False);
        Assert.That(QueueConstructionOnCompletion.IsSiteOnly(0), Is.False);
        Assert.That(
            QueueConstructionOnCompletion.IsSiteOnly(ComponentMountFlags.PlanetInstallation | ComponentMountFlags.ShipComponent),
            Is.True,
            "Only PlanetInstallation and the absence of ShipCargo matter.");
    }

    [Test]
    public void OrdinaryCrate_FollowsTheUserPreference_AndDefaultsOff()
    {
        var crate = Item(Crate, canAutoInstall: true);
        Assert.That(QueueConstructionOnCompletion.IsSiteOnly(crate), Is.False);
        Assert.That(QueueConstructionOnCompletion.IsForced(crate), Is.False);
        Assert.That(QueueConstructionOnCompletion.IsDisabled(crate), Is.False);
        Assert.That(QueueConstructionOnCompletion.DisplayedValue(QueueConstructionOnCompletion.Default, crate), Is.False);
        Assert.That(QueueConstructionOnCompletion.CommandAutoInstall(false, crate), Is.False);
        Assert.That(QueueConstructionOnCompletion.CommandAutoInstall(true, crate), Is.True);
        Assert.That(QueueConstructionOnCompletion.Tooltip(crate), Is.Empty);
    }

    [Test]
    public void SiteOnlyItem_IsCheckedAndDisabled_AndTheCommandIsAlwaysOn()
    {
        var site = Item(SiteOnly, canAutoInstall: true);
        Assert.That(QueueConstructionOnCompletion.IsSiteOnly(site), Is.True);
        Assert.That(QueueConstructionOnCompletion.IsForced(site), Is.True);
        Assert.That(QueueConstructionOnCompletion.IsDisabled(site), Is.True);
        Assert.That(QueueConstructionOnCompletion.DisplayedValue(false, site), Is.True);
        Assert.That(QueueConstructionOnCompletion.CommandAutoInstall(false, site), Is.True);
        Assert.That(QueueConstructionOnCompletion.CommandAutoInstall(true, site), Is.True);
        Assert.That(QueueConstructionOnCompletion.Tooltip(site), Is.EqualTo(QueueConstructionOnCompletion.SiteOnlyTooltip));
        Assert.That(QueueConstructionOnCompletion.SiteOnlyTooltip, Is.EqualTo("Built on site: always constructed when complete."));
    }

    [Test]
    public void SiteOnlyWithoutInstallationPoints_StaysDisabled_AndDoesNotForceTheCommand()
    {
        var site = Item(SiteOnly, canAutoInstall: false, reason: "This colony has no installation points.");
        Assert.That(QueueConstructionOnCompletion.IsSiteOnly(site), Is.True);
        Assert.That(QueueConstructionOnCompletion.IsForced(site), Is.False);
        Assert.That(QueueConstructionOnCompletion.IsDisabled(site), Is.True);
        Assert.That(QueueConstructionOnCompletion.DisplayedValue(true, site), Is.False);
        Assert.That(QueueConstructionOnCompletion.CommandAutoInstall(true, site), Is.False);
        Assert.That(QueueConstructionOnCompletion.Tooltip(site), Is.EqualTo("This colony has no installation points."));
    }

    [Test]
    public void NonInstallation_StaysOffAndDisabled()
    {
        var steel = Item(0, canAutoInstall: false, reason: "This design is not a colony installation.");
        Assert.That(QueueConstructionOnCompletion.IsSiteOnly(steel), Is.False);
        Assert.That(QueueConstructionOnCompletion.IsForced(steel), Is.False);
        Assert.That(QueueConstructionOnCompletion.IsDisabled(steel), Is.True);
        Assert.That(QueueConstructionOnCompletion.DisplayedValue(true, steel), Is.False);
        Assert.That(QueueConstructionOnCompletion.CommandAutoInstall(true, steel), Is.False);
        Assert.That(QueueConstructionOnCompletion.Tooltip(steel), Is.EqualTo("This design is not a colony installation."));
    }
}
