using System;
using System.Numerics;
using ImGuiNET;
namespace Pulsar4X.SDL2UI
{
    public static class Helpers
    {


        public static Vector3 Color(byte r, byte g, byte b)
        {
            float rf = (1.0f / 255) * r;
            float gf = (1.0f / 255) * g;
            float bf = (1.0f / 255) * b;
            return new Vector3(rf, gf, bf);
        }

        public static byte Color(float color)
        {
            return (byte)(Math.Max(0, Math.Min(255, (int)Math.Floor(color * 256.0))));
        }

    }

    public static class DistanceDisplay
    {
        public enum ValueType
        {
            Au,
            MKm,
            Km,
            m 
        }

        public enum DisplayType
        {
            Raw,
            Global,
            Au,
            Mkm,
            Km,
            m 
        }

        static DisplayType GlobalDisplayType = DisplayType.Km;
        static string GlobalFormat = "0.###";

        static string StringifyValue(double value, string format = "0.###")
        {
            return ECSLib.Misc.StringifyDistance(value, format);
        }

        public static void Display(string Id, double value, ValueType inputType, ref DisplayType displayType, ref string displayFormat )
        {
            //ImGui.GetID(Id);

            ImGui.Text(StringifyValue(value, displayFormat));
            if(ImGui.BeginPopupContextItem(Id, 1))
            {
                if(ImGui.SmallButton("Set Display Type"))
                { }
                if(ImGui.SmallButton("Set Display Format"))
                { }

            }
        }

    }
}
