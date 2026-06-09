namespace Pulsar4X.Api;

/// <summary>A snapshot of the simulation clock.</summary>
public sealed record TimeState(DateTime GameDateTime, bool IsRunning, float Multiplier, TimeSpan TickLength);

public enum TimeControlAction
{
    Pause,
    Start,
    StepOnce,
    SetSpeed,
}

/// <summary>A request to change the simulation clock (server/SM authority).</summary>
public sealed record TimeControlRequest(TimeControlAction Action, float? Multiplier = null, TimeSpan? StepLength = null);
