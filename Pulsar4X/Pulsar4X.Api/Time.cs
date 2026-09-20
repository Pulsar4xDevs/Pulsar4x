namespace Pulsar4X.Api;

/// <summary>A snapshot of the simulation clock.</summary>
/// <param name="GameDateTime">Current global simulation time.</param>
/// <param name="IsRunning">Whether the clock is advancing.</param>
/// <param name="IsStopping">Whether the clock is running but has a pending pause/stop request.</param>
/// <param name="TickLength">How much simulation time advances per tick/step.</param>
/// <param name="TickFrequency">Real-time interval between ticks while running (also sets the speed).</param>
public sealed record TimeState(
    DateTime GameDateTime,
    bool IsRunning,
    bool IsStopping,
    TimeSpan TickLength,
    TimeSpan TickFrequency);

public enum TimeControlAction
{
    Pause,
    Start,
    StepOnce,
    SetTickLength,
    SetTickFrequency,
}

/// <summary>A request to change the simulation clock (server/SM authority). Only the field relevant to
/// <see cref="Action"/> is read.</summary>
public sealed record TimeControlRequest(
    TimeControlAction Action,
    TimeSpan? StepLength = null,
    TimeSpan? TickLength = null,
    TimeSpan? TickFrequency = null);
