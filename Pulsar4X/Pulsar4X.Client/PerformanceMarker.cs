using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Pulsar4X.Client;

public static class PerformanceMetrics
{
    private readonly static Dictionary<string, PerformanceData> s_Metrics = [];

    internal static PerformanceData RegisterMarker(string markerName)
    {
        if (!s_Metrics.TryGetValue(markerName, out var data))
        {
            data = new PerformanceData();
            s_Metrics[markerName] = data;
        }
        else
        {
            Debug.WriteLine($"Duplicate performance marker name: {markerName}");
        }
        return data;
    }
}

internal class PerformanceData
{
    public long BeginTicks { get; internal set; }
    public long EndTicks { get; internal set; }

    public TimeSpan Last => TimeSpan.FromTicks(EndTicks - BeginTicks);
}

public readonly struct PerformanceMarker(string name)
{
    private readonly PerformanceData _data = PerformanceMetrics.RegisterMarker(name);

    public TimeSpan Last => _data.Last;

    public void Begin()
    {
        _data.BeginTicks = Stopwatch.GetTimestamp();
    }

    public void End()
    {
        _data.EndTicks = Stopwatch.GetTimestamp();
    }
}
