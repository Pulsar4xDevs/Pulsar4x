namespace Pulsar4X.Api;

/// <summary>How the order chooses a drive. Auto is <c>MovePlanner.Select</c>.</summary>
public enum MoveDriveChoice
{
    Auto,
    Warp,
    Newtonian,
}

/// <summary>When a Newtonian transfer leaves. Warp ignores this and leaves now.</summary>
public enum DepartureTiming
{
    BestWindow,
    DepartNow,
    PickOnPorkchop,
}

/// <summary>What the ship does at the target. Only orbit is built.</summary>
public enum ArrivalKind
{
    Orbit,
    Flyby,
    Stop,
}

/// <summary>
/// One porkchop cell the player picked. Dates and inertial parent-relative Δv.
/// The engine turns this into a <c>TransferWindow</c>.
/// </summary>
public sealed record WindowChoice(DateTime Depart, DateTime Arrive, Vec3 DepartDeltaV, Vec3 ArriveDeltaV);

/// <summary>
/// What the Orders panel sends with a move. Every field defaults to today's behaviour.
/// </summary>
public sealed record MoveOrderOptions(
    MoveDriveChoice Mode = MoveDriveChoice.Auto,
    DepartureTiming Timing = DepartureTiming.BestWindow,
    WindowChoice? PickedWindow = null,
    double Periapsis = 0,
    double Apoapsis = 0,
    ArrivalKind Arrival = ArrivalKind.Orbit);

/// <summary>
/// One arrival orbit the ship can finish with the Δv it has.
/// Periapsis and apoapsis are equal for a circular orbit; apoapsis is 0 when the offer is circular
/// and the radius is <see cref="Periapsis"/>.
/// </summary>
public sealed record ArrivalChoice(double Periapsis, double Apoapsis, double DeltaV, string Label);

/// <summary>A mode <c>Select</c> did not pick. The numbers were already computed.</summary>
public sealed record ModeOffer(string Mode, bool Feasible, double EtaSeconds, double DeltaV, string Reason);

/// <summary>One priced departure. <see cref="FuelKg"/> is NaN when the rocket equation cannot be run.</summary>
public sealed record TimingOffer(
    DepartureTiming Kind,
    double WaitSeconds,
    double EtaSeconds,
    double DeltaV,
    double FuelKg,
    bool Feasible,
    string Reason);

/// <summary>One leg of the route. Times are seconds from now.</summary>
public sealed record TripLeg(string Kind, double DeltaV, double StartSeconds, double EndSeconds);

/// <summary>
/// A move plan for one ship and one body. The Orders panel asks once per click.
/// </summary>
public sealed record TripPreview(
    bool Feasible,
    string Reason,
    string Mode,
    double DeltaV,
    double Have,
    double EtaSeconds,
    bool RefusedForDeltaV,
    IReadOnlyList<ArrivalChoice> Alternatives,
    double DepartInSeconds = 0,
    IReadOnlyList<ModeOffer>? OtherModes = null,
    IReadOnlyList<TripLeg>? Legs = null,
    DepartureTiming Timing = DepartureTiming.BestWindow,
    double WaitSeconds = 0,
    IReadOnlyList<TimingOffer>? TimingOptions = null,
    string TimingNote = "",
    bool LocalTransferAfterWarp = false,
    bool CanPickWindow = false)
{
    public IReadOnlyList<ModeOffer> Modes => OtherModes ?? Array.Empty<ModeOffer>();
    public IReadOnlyList<TripLeg> Route => Legs ?? Array.Empty<TripLeg>();
    public IReadOnlyList<TimingOffer> Timings => TimingOptions ?? Array.Empty<TimingOffer>();

    public static TripPreview None(string reason) =>
        new(false, reason, "", 0, 0, 0, false, Array.Empty<ArrivalChoice>());
}
