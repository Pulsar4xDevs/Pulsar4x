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

    public static class BorderListOptions
    {
        
        private static Vector2 _labelSize = new Vector2();
        private static float _xleft;
        private static float _xcentr;
        private static float _xright;

        private static float _ytop;
        private static float _yctr1;
        private static float _yctr2;
        private static float _ybot;
        
        private static uint _colour;
        private static float _dentMulitpier = 3;
        public static void Begin(string id, string[] list, ref int selected, float width)
        {
            ImGui.PushID(id);
            _colour = ImGui.GetColorU32(ImGuiCol.Border);
            _labelSize = new Vector2( width, ImGui.GetTextLineHeight());
            ImGui.Columns(2, id, false);
            ImGui.SetColumnWidth(0, width);
            
            _xleft = ImGui.GetCursorScreenPos().X;
            _ytop = ImGui.GetCursorScreenPos().Y;
            _xcentr = _xleft + width;

            var vpad = ImGui.GetTextLineHeightWithSpacing() - ImGui.GetTextLineHeight();

            
            ImGui.Indent(_dentMulitpier);
            //display the list of items:
            for (int i = 0; i < list.Length; i++)
            {
                var pos = ImGui.GetCursorScreenPos();
 
                ImGui.Text(list[i]);
                if (ImGui.IsItemClicked())
                    selected = i;
                
                if(i == selected)
                {   
                    _yctr1 = pos.Y - vpad * 0.5f;
                    _yctr2 = _yctr1 + ImGui.GetTextLineHeightWithSpacing();
                }
                
            }
            
            
            _ybot = ImGui.GetCursorScreenPos().Y;
            //if nothing is selected we'll draw a line at the bottom instead of around one of the items:
            if(selected < 0)
            {
                _yctr1 = _ybot;
                _yctr2 = _ybot;
            }
            ImGui.NextColumn(); //set nextColomn so the imgui.items placed after this get put into the righthand side
            ImGui.Indent(_dentMulitpier);
        }
        /*
        public static void Begin(string id, ref int selected, string[] list)
        {
            Begin(id, list, ref selected, ImGui.GetColorU32(ImGuiCol.Border)); 
        }
        
        public static void Begin(string id, string[] list, ref int selected, ImGuiCol colorIdx)
        {
            Begin(id, list, ref selected, ImGui.GetColorU32(colorIdx)); 
        }
*/
        public static void End(int width2ndColomn)
        {
            
            ImGui.NextColumn();
            ImGui.Columns(1); 
            ImGui.Unindent(_dentMulitpier);
            
            _xright = _xcentr + width2ndColomn;
            float boty = Math.Max(_ybot, ImGui.GetCursorScreenPos().Y); //is the list bigger, or the items drawn after it.

            ImDrawListPtr wdl = ImGui.GetWindowDrawList();
            
            
            Vector2[] pts = new Vector2[9];
            pts[0] = new Vector2(_xleft, _yctr1);          //topleft of the selected item
            pts[1] = new Vector2(_xleft, _yctr2);          //botomleft of the selected item
            pts[2] = new Vector2(_xcentr, _yctr2);         //bottom rigth of selected item
            pts[3] = new Vector2(_xcentr, boty);           //bottom left of rh colomn
            pts[4] = new Vector2(_xright, boty);           //bottom Right
            pts[5] = new Vector2(_xright, _ytop);          //top righht
            pts[6] = new Vector2(_xcentr, _ytop);          //top mid
            pts[7] = new Vector2(_xcentr, _yctr1);         //selected top right
            pts[8] = pts[0];                                    //selected top left


            wdl.AddPolyline(ref pts[0], pts.Length, _colour, false, 1.0f);
            
            ImGui.PopID();
            
        }
    }

    public static class BorderGroup
    {
        private static Vector2[] _startPos = new Vector2[8];
        private static Vector2[] _labelSize = new Vector2[8];
        private static uint[] _colour = new uint[8];
        private static byte _nestIndex = 0;
        private static float _dentMulitpier = 3;
        public static void Begin(string label, uint colour)
        {
            ImGui.PushID(label);
            
            _colour[_nestIndex] = colour;
            _startPos[_nestIndex] = ImGui.GetCursorScreenPos();
            _startPos[_nestIndex].X -= _dentMulitpier;
            _startPos[_nestIndex].Y += ImGui.GetTextLineHeight() * 0.5f;
            ImGui.Text(label);
            _labelSize[_nestIndex] = ImGui.GetItemRectSize();
            _nestIndex++;
            ImGui.Indent(_dentMulitpier * _nestIndex);
        }
        
        public static void Begin(string label)
        {
            Begin(label, ImGui.GetColorU32(ImGuiCol.Border)); 
        }
        
        public static void Begin(string label, ImGuiCol colorIdx)
        {
            Begin(label, ImGui.GetColorU32(colorIdx)); 
        }

        public static void End()
        {
            End(ImGui.GetContentRegionAvail().X);
        }

        public static void End(float width)
        { 
            ImGui.Unindent(_dentMulitpier * _nestIndex);
            _nestIndex--;
            var pos = ImGui.GetCursorScreenPos();
            
            Vector2 size = new Vector2(width, pos.Y - _startPos[_nestIndex].Y);
            ImDrawListPtr wdl = ImGui.GetWindowDrawList();

            float by = _startPos[_nestIndex].Y + size.Y + _dentMulitpier -_dentMulitpier * _nestIndex;
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
