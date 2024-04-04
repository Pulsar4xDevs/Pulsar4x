using System.ComponentModel;

namespace Pulsar4X.SDL2UI;

public class UserOrbitSettings
{
    internal enum OrbitBodyType
    {
        Star,
        Planet,
        Moon,
        Asteroid,
        Comet,
        Colony,
        Ship,
        Unknown,

        [Description("Number Of")]
        NumberOf
    }

    internal enum OrbitTrajectoryType
    {
        Unknown,
        [Description("An Elliptical Orbit")]
        Elliptical,
        Hyperbolic,

        [Description("Newtonian Thrust")]
        NewtonionThrust,

        [Description("Non-Newtonian Translation")]
        NonNewtonionTranslation,

        [Description("Number Of")]
        NumberOf
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
}