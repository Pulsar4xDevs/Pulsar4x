using System.Collections.Generic;
using System.ComponentModel;

namespace Pulsar4X.Client;

public class UserOrbitSettings
{
    internal enum OrbitBodyType
    {
        Unknown,
        Star,
        Planet,
        DwarfPlanet,
        Moon,
        Asteroid,
        Comet,
        Colony,
        Ship
    }

    /// <summary>Maps the API body classification to the client's display enum (used for icons,
    /// tooltips and the shared map view-filter).</summary>
    internal static OrbitBodyType FromBodyKind(Pulsar4X.Api.BodyKind kind) => kind switch
    {
        Pulsar4X.Api.BodyKind.Star => OrbitBodyType.Star,
        Pulsar4X.Api.BodyKind.Planet => OrbitBodyType.Planet,
        Pulsar4X.Api.BodyKind.DwarfPlanet => OrbitBodyType.DwarfPlanet,
        Pulsar4X.Api.BodyKind.Moon => OrbitBodyType.Moon,
        Pulsar4X.Api.BodyKind.Asteroid => OrbitBodyType.Asteroid,
        Pulsar4X.Api.BodyKind.Comet => OrbitBodyType.Comet,
        Pulsar4X.Api.BodyKind.Colony => OrbitBodyType.Colony,
        Pulsar4X.Api.BodyKind.Ship => OrbitBodyType.Ship,
        _ => OrbitBodyType.Unknown,
    };

    public static readonly string[] OrbitBodyTypeTooltips = new []
    {
        "Unknown", "Stars", "Planets", "Dwarf Planets", "Moons", "Asteroids",
        "Comets", "Colonies", "Ships"
    };

    public static readonly string[] OrbitBodyTypeShortNames = new []
    {
        "?", "*", "P", "D", "M", "A", "C", "H", "S"
    };

    internal enum OrbitTrajectoryType
    {
        Unknown,

        [Description("An Elliptical Orbit")]
        Elliptical,
        Hyperbolic,

        [Description("Newtonian Thrust")]
        NewtonionThrust,

        [Description("Non-Newtonian Translation")]
        NonNewtonionTranslation
    }
    //the arc thats actualy drawn, ie we don't normaly draw a full 360 degree (6.28rad) orbit, but only
    //a section of it ie 3/4 of the orbit (4.71rad) and this is player adjustable.
    public float EllipseSweepRadians = 4.71239f;
    //we stop showing names when zoomed out further than this number
    public float ShowNameAtZoom = 100;

    /// <summary>
    /// Number of segments in a full ellipse. this is basicaly the resolution of the orbits.
    /// 32 is a good low number, slightly ugly. 180 is a little overkill till you get really big orbits.
    /// </summary>
    public byte NumberOfArcSegments = 180;

    public byte Red = 0;
    public byte Grn = 0;
    public byte Blu = 255;
    public byte MaxAlpha = 255;
    public byte MinAlpha = 0;
    public byte GhostOrbitAlpha = 20;

    /// <summary>
    /// Minimum on-screen radius of the body icon, in pixels. Zooming in still grows
    /// the icon up to the body's real radius. 0 means a saved file predates this field.
    /// </summary>
    public float IconMinPixels = 0;

    /// <summary>Built-in minimum icon radius. Asteroids are half the old 8px default.</summary>
    internal static float DefaultIconMinPixels(OrbitBodyType type) => type switch
    {
        OrbitBodyType.Star => 16f,
        OrbitBodyType.Moon => 4f,
        OrbitBodyType.Asteroid => 4f,
        _ => 8f,
    };

    /// <summary>Fills icon sizes that a saved settings file left at 0.</summary>
    internal static void ApplyMissingIconSizes(List<List<UserOrbitSettings>> mtx)
    {
        for (int i = 0; i < mtx.Count; i++)
        {
            float fallback = DefaultIconMinPixels((OrbitBodyType)i);
            foreach (var settings in mtx[i])
            {
                if (settings.IconMinPixels <= 0)
                    settings.IconMinPixels = fallback;
            }
        }
    }

    internal static float IconMinPixelsFor(List<List<UserOrbitSettings>> mtx, OrbitBodyType type)
    {
        int i = (int)type;
        if ((uint)i >= (uint)mtx.Count || mtx[i].Count == 0)
            return DefaultIconMinPixels(type);
        float value = mtx[i][0].IconMinPixels;
        return value > 0 ? value : DefaultIconMinPixels(type);
    }
}
