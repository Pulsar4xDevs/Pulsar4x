using System;
using System.Numerics;
using ImGuiNET;
using Vector3 = System.Numerics.Vector3;

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


    public class BorderGroup
    {
        private static Vector2 _startPos;
        private static Vector2 _labelSize;
        private static uint _colour;
        public static void BeginBorder(string label, uint colour)
        {
            ImGui.PushID(label);
            _colour = colour;
            _startPos = ImGui.GetCursorScreenPos();
            _startPos.X -= 3;
            _startPos.Y += ImGui.GetTextLineHeight() * 0.5f;
            ImGui.Text(label);
            _labelSize = ImGui.GetItemRectSize();
        }

        public static void EndBoarder()
        { 
            Vector2 size = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetCursorScreenPos().Y - _startPos.Y);
            //ImGui.get
            ImDrawListPtr wdl = ImGui.GetWindowDrawList();

            Vector2[] pts = new Vector2[6];
            pts[0] = new Vector2(_startPos.X + 2, _startPos.Y);
            pts[1] = _startPos; //top left
            pts[2] = new Vector2(_startPos.X, _startPos.Y + size.Y); //bottom left
            pts[3] = new Vector2(_startPos.X + size.X + 3, _startPos.Y + size.Y); //bottom right
            pts[4] = new Vector2(_startPos.X + size.X + 3, _startPos.Y);
            pts[5] = new Vector2(_startPos.X + _labelSize.X + 3, _startPos.Y);
            wdl.AddPolyline(ref pts[0], pts.Length, _colour, false, 1.0f);
            ImGui.PopID();

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
            return ECSLib.Stringify.Distance(value, format);
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
