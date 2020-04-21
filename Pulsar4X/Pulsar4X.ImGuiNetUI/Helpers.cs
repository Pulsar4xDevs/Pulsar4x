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
        private static Vector2[] _startPos = new Vector2[8];
        private static Vector2[] _labelSize = new Vector2[8];
        private static uint[] _colour = new uint[8];
        private static byte _nestIndex = 0;
        private static float _dentMulitpier = 3;
        public static void BeginBorder(string label, uint colour)
        {
            ImGui.PushID(label);
            
            _colour[_nestIndex] = colour;
            _startPos[_nestIndex] = ImGui.GetCursorScreenPos();
            _startPos[_nestIndex].X -= 3;
            _startPos[_nestIndex].Y += ImGui.GetTextLineHeight() * 0.5f;
            ImGui.Text(label);
            _labelSize[_nestIndex] = ImGui.GetItemRectSize();
            _nestIndex++;
            ImGui.Indent(_dentMulitpier * _nestIndex);
        }



        public static void BeginBorder(string label)
        {
            BeginBorder(label, ImGui.GetColorU32(ImGuiCol.Border)); 
        }
        
        public static void BeginBorder(string label, ImGuiCol colorIdx)
        {
            BeginBorder(label, ImGui.GetColorU32(colorIdx)); 
        }

        public static void EndBoarder()
        { 
            ImGui.Unindent(_dentMulitpier * _nestIndex);
            _nestIndex--;
            Vector2 size = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetCursorScreenPos().Y - _startPos[_nestIndex].Y);
            //ImGui.get
            ImDrawListPtr wdl = ImGui.GetWindowDrawList();

            float by = _startPos[_nestIndex].Y + size.Y -_dentMulitpier * _nestIndex;
            float rx = _startPos[_nestIndex].X + size.X - _dentMulitpier * _nestIndex;
            
            Vector2[] pts = new Vector2[6];
            pts[0] = new Vector2(_startPos[_nestIndex].X + _dentMulitpier, _startPos[_nestIndex].Y);
            pts[1] = _startPos[_nestIndex]; //top left
            pts[2] = new Vector2(_startPos[_nestIndex].X, by); //bottom left
            pts[3] = new Vector2(rx, by); //bottom right
            pts[4] = new Vector2(rx, _startPos[_nestIndex].Y); //top right
            pts[5] = new Vector2(_startPos[_nestIndex].X + _labelSize[_nestIndex].X + _dentMulitpier, _startPos[_nestIndex].Y);
            wdl.AddPolyline(ref pts[0], pts.Length, _colour[_nestIndex], false, 1.0f);
            
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
