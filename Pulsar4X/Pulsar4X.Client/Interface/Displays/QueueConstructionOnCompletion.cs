using Pulsar4X.Api;

namespace Pulsar4X.Client;

/// <summary>
/// Decision logic for the production "Queue construction on completion?" checkbox.
/// Site-only designs match <c>LocalConstructionJob.IsSiteOnly</c>: PlanetInstallation
/// and not ShipCargo. The engine always follows that up with a construction job when
/// the colony has installation points, so the UI forces the box on and sends
/// <c>AutoInstall = true</c>.
/// </summary>
public static class QueueConstructionOnCompletion
{
    public const bool Default = false;

    public const string SiteOnlyTooltip = "Built on site: always constructed when complete.";

    /// <summary>
    /// Exact engine condition: <c>LocalConstructionJob.IsSiteOnly</c>.
    /// </summary>
    public static bool IsSiteOnly(ComponentMountFlags mountFlags)
        => mountFlags.HasFlag(ComponentMountFlags.PlanetInstallation)
           && !mountFlags.HasFlag(ComponentMountFlags.ShipCargo);

    public static bool IsSiteOnly(ConstructibleItemView item)
        => IsSiteOnly(item.MountFlags);

    /// <summary>Checked and disabled: a site-only item on a colony that can install it.</summary>
    public static bool IsForced(ConstructibleItemView item)
        => IsSiteOnly(item) && item.CanAutoInstall;

    public static bool IsDisabled(ConstructibleItemView item)
        => !item.CanAutoInstall || IsSiteOnly(item);

    public static bool DisplayedValue(bool userPreference, ConstructibleItemView item)
        => IsForced(item) || (item.CanAutoInstall && userPreference);

    /// <summary>Value for <c>QueueIndustryJobCommand.AutoInstall</c>.</summary>
    public static bool CommandAutoInstall(bool userPreference, ConstructibleItemView item)
        => DisplayedValue(userPreference, item);

    public static string Tooltip(ConstructibleItemView item)
    {
        if (IsForced(item))
            return SiteOnlyTooltip;
        return item.AutoInstallReason;
    }
}
