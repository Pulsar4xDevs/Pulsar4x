namespace Pulsar4X.Api;

/// <summary>
/// One arrival orbit the ship can finish with the Δv it has.
/// Periapsis and apoapsis are equal for a circular orbit; apoapsis is 0 when the offer is circular
/// and the radius is <see cref="Periapsis"/>.
/// </summary>
public sealed record ArrivalChoice(double Periapsis, double Apoapsis, double DeltaV, string Label);

/// <summary>
/// A move plan for one ship and one body. The Orders tree asks once per game day.
/// </summary>
public sealed record TripPreview(
    bool Feasible,
    string Reason,
    string Mode,
    double DeltaV,
    double Have,
    double EtaSeconds,
    bool RefusedForDeltaV,
    IReadOnlyList<ArrivalChoice> Alternatives)
{
    public static TripPreview None(string reason) =>
        new(false, reason, "", 0, 0, 0, false, Array.Empty<ArrivalChoice>());
}
