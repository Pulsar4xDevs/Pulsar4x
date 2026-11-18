namespace Pulsar4X.Api;

/// <summary>Wording the engine and the API share for ships with no bridge (crew rules phase 1: automated ships).</summary>
public static class CrewText
{
    /// <summary>Why an automated ship (no bridge) refuses an order of its own.</summary>
    public const string AutomatedNoOrders =
        "This ship has no bridge: it is automated and takes no orders of its own. Put it in a fleet to have it follow the flagship.";

    /// <summary>The span line and reach fix for an automated ship: no bridge, no chair, no captain, so no reach to describe.</summary>
    public const string AutomatedSpan = "Automated: no bridge, takes no orders of its own; follows its fleet.";
}
