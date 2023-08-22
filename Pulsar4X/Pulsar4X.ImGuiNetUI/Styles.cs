using System.Numerics;

namespace Pulsar4X.SDL2UI
{
    public static class Styles
    {
        public static Vector4 HighlightColor = new(0.25f, 1f, 0.25f, 0.9f);
        public static Vector4 GoodColor = new (0.25f, 1f, 0.25f, 0.9f);
        public static Vector4 DescriptiveColor = new (0.45f, 0.45f, 0.45f, 1f);

        public static Vector4 OkColor = new (1.0f, 1.0f, 0.25f, 0.9f);
        public static Vector4 BadColor = new (1.0f, 0.25f, 0.25f, 0.9f);

        public static Vector4 SelectedColor = new Vector4(0.75f, 0.25f, 0.25f, 1f);
        public static Vector4 InvisibleColor = new Vector4(0, 0, 0, 0f);
    }
}