using System.Numerics;
using ImGuiNET;

namespace Pulsar4X.SDL2UI
{
    public static class Styles
    {
        public static Vector4 HighlightColor = new(0.25f, 1f, 0.25f, 0.9f);
        public static Vector4 GoodColor = new (0.25f, 1f, 0.25f, 0.9f);
        public static Vector4 DescriptiveColor = new (0.45f, 0.45f, 0.45f, 1f);

        public static Vector4 OkColor = new (1.0f, 1.0f, 0.25f, 0.9f);
        public static Vector4 BadColor = new (1.0f, 0.25f, 0.25f, 0.9f);
        public static Vector4 TerribleColor = new (1.0f, 0.05f, 0.05f, 1.0f);

        public static Vector4 SelectedColor = new Vector4(0.75f, 0.25f, 0.25f, 1f);
        public static Vector4 InvisibleColor = new Vector4(0, 0, 0, 0f);

        public static ImGuiTableFlags TableFlags = ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg;
        public static float LeftColumnWidth = 204f;
        public static float LeftColumnWidthLg = 256f;
        public static Vector2 ToolTipsize = new Vector2(426, 0);
        public static Vector2 Indent = new Vector2(2, 2);

        public static string IntFormat = "###,###,###,##0";
        public static string DecimalFormat = "###,###,##0.##";
    }
}