namespace Pulsar4X.Api;

/// <summary>A snapshot of the simulation clock.</summary>
/// <param name="GameDateTime">Current global simulation time.</param>
/// <param name="IsRunning">Whether the clock is advancing.</param>
/// <param name="Multiplier">Speed multiplier applied to the real-time tick interval.</param>
/// <param name="TickLength">How much simulation time advances per tick/step.</param>
/// <param name="TickFrequency">Real-time interval between ticks while running.</param>
public sealed record TimeState(
    DateTime GameDateTime,
    bool IsRunning,
    float Multiplier,
    TimeSpan TickLength,
    TimeSpan TickFrequency);

public enum TimeControlAction
{
    Pause,
    Start,
    StepOnce,
    SetSpeed,
    SetTickLength,
    SetTickFrequency,
}

/// <summary>A request to change the simulation clock (server/SM authority). Only the field relevant to
/// <see cref="Action"/> is read.</summary>
public sealed record TimeControlRequest(
    TimeControlAction Action,
    float? Multiplier = null,
    TimeSpan? StepLength = null,
    TimeSpan? TickLength = null,
    TimeSpan? TickFrequency = null);
