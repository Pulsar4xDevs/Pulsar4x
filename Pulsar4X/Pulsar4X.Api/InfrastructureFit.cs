using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.Api;

/// <summary>
/// The gravity (m/s²) and pressure (atm) range an infrastructure design works in. A design with no limit on an axis
/// carries <see cref="double.MinValue"/> to <see cref="double.MaxValue"/> there.
/// </summary>
public sealed record InfrastructureWindow(double MinGravity, double MaxGravity, double MinPressure, double MaxPressure)
{
    /// <summary>The body's gravity and pressure are both inside the window.</summary>
    public bool Covers(double gravity, double pressure)
        => gravity >= MinGravity && gravity <= MaxGravity && pressure >= MinPressure && pressure <= MaxPressure;

    /// <summary>Gravity width plus pressure width. The narrower the window, the more specific the design.</summary>
    public double CombinedWidth => (MaxGravity - MinGravity) + (MaxPressure - MinPressure);

    /// <summary>
    /// How far the body is from the middle of the window. Each axis is the distance from the window's midpoint divided
    /// by that window's width, so metres per second squared and atmospheres add up. Lower is closer.
    /// </summary>
    public double Distance(double gravity, double pressure)
        => Scaled(gravity, MinGravity, MaxGravity) + Scaled(pressure, MinPressure, MaxPressure);

    static double Scaled(double value, double min, double max)
    {
        double width = max - min;
        if (double.IsInfinity(width))
            return 0; // No limit on this axis, so every value is as good as another.
        double distance = Math.Abs(value - (min + max) / 2);
        if (width <= 0)
            return distance == 0 ? 0 : double.MaxValue;
        return distance / width;
    }
}

/// <summary>
/// Which infrastructure design suits a destination body (the fleet Bootstrap order, decision 18). The source cargo's
/// designs that cover the body's gravity and pressure are the candidates. Best fit is the candidate whose window
/// midpoint is closest to the body once each axis is divided by its window width. A tie goes to the narrower combined window,
/// then to the lower design id so the answer never depends on list order.
/// </summary>
public static class InfrastructureFit
{
    const double Tolerance = 1e-9;

    /// <summary>The candidates whose window covers the body, best fit first.</summary>
    public static List<string> Rank(IEnumerable<(string Id, InfrastructureWindow Window)> candidates, double gravity, double pressure)
    {
        var fitting = candidates
            .Where(c => c.Window.Covers(gravity, pressure))
            .Select(c => (c.Id, Distance: c.Window.Distance(gravity, pressure), Width: c.Window.CombinedWidth))
            .ToList();
        fitting.Sort((a, b) =>
        {
            if (Math.Abs(a.Distance - b.Distance) > Tolerance)
                return a.Distance.CompareTo(b.Distance);
            if (Math.Abs(a.Width - b.Width) > Tolerance)
                return a.Width.CompareTo(b.Width);
            return string.CompareOrdinal(a.Id, b.Id);
        });
        return fitting.Select(c => c.Id).ToList();
    }

    /// <summary>The best-fit design id, or null when no candidate covers the body.</summary>
    public static string? Best(IEnumerable<(string Id, InfrastructureWindow Window)> candidates, double gravity, double pressure)
        => Rank(candidates, gravity, pressure).FirstOrDefault();

    /// <summary>The reason shown when no design fits. The same words are the tooltip on the blocked button.</summary>
    public static string NoFitMessage(string destinationPlanet) => $"No suitable infrastructure for {destinationPlanet}";
}
