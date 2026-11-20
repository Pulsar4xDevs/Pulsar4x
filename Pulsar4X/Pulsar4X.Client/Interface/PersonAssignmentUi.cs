using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Pulsar4X.Api;

namespace Pulsar4X.Client;

/// <summary>
/// Size clamping and candidate filtering for the shared person-assignment chooser. No ImGui.
/// </summary>
public static class PersonAssignmentUi
{
    public const float DefaultWidth = 720f;
    public const float DefaultHeight = 500f;
    public const float MinWidth = 520f;
    public const float MinHeight = 320f;
    public const float MaxHeightFraction = 0.7f;

    public static readonly Vector2 DefaultSize = new(DefaultWidth, DefaultHeight);
    public static readonly Vector2 MinSize = new(MinWidth, MinHeight);

    /// <summary>Maximum window height: about 70% of the viewport.</summary>
    public static float MaxHeight(float viewportHeight)
        => Math.Max(MinHeight, viewportHeight * MaxHeightFraction);

    /// <summary>Upper size bound: full viewport width, 70% of its height.</summary>
    public static Vector2 MaxSize(Vector2 viewport)
        => new(Math.Max(MinWidth, viewport.X), MaxHeight(viewport.Y));

    /// <summary>Clamp a requested size into the min/max box for this viewport.</summary>
    public static Vector2 Constrain(Vector2 requested, Vector2 viewport)
    {
        var max = MaxSize(viewport);
        return new Vector2(
            Math.Clamp(requested.X, MinWidth, Math.Max(MinWidth, max.X)),
            Math.Clamp(requested.Y, MinHeight, Math.Max(MinHeight, max.Y)));
    }

    /// <summary>Drop the person already in the post. The list is otherwise unchanged.</summary>
    public static IReadOnlyList<CommanderSnapshot> ExcludeCurrent(
        IEnumerable<CommanderSnapshot>? people,
        int currentId)
    {
        var source = people ?? Array.Empty<CommanderSnapshot>();
        if (currentId < 0)
            return source as IReadOnlyList<CommanderSnapshot> ?? source.ToList();
        return source.Where(person => person.Id != currentId).ToList();
    }

    /// <summary>Case-insensitive name substring filter. A blank query keeps everyone.</summary>
    public static IReadOnlyList<CommanderSnapshot> FilterByName(
        IEnumerable<CommanderSnapshot>? people,
        string? query)
    {
        var source = people ?? Array.Empty<CommanderSnapshot>();
        if (string.IsNullOrWhiteSpace(query))
            return source as IReadOnlyList<CommanderSnapshot> ?? source.ToList();
        string needle = query.Trim();
        return source
            .Where(person => person.Name != null
                && person.Name.Contains(needle, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public static IReadOnlyList<CommanderSnapshot> OfKind(
        IEnumerable<CommanderSnapshot>? people,
        CommanderKind kind)
        => (people ?? Array.Empty<CommanderSnapshot>())
            .Where(person => person.Kind == kind)
            .OrderBy(person => person.Name, StringComparer.Ordinal)
            .ToList();
}
