using System;
using System.Collections;
using System.Numerics;
using System.Reflection;
using ImGuiNET;
using Pulsar4X.ECSLib;
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
        private static float _lhHeight;
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
            _lhHeight = ImGui.GetContentRegionAvail().Y;
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
        public static void End(Vector2 sizeRight)
        {
            var winpos = ImGui.GetCursorPos();
            
            var rgnSize = ImGui.GetContentRegionAvail();
            ImGui.NextColumn();
            ImGui.Columns(1); 
            var scpos = ImGui.GetCursorScreenPos();
            ImGui.Unindent(_dentMulitpier);
            
            _xright = _xcentr + sizeRight.X;

            float boty = Math.Max(_ybot, _ytop + sizeRight.Y); //is the list bigger, or the items drawn after it.

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


    public static class EntityInspector
    {
        private static int _selectedDB = -1;
        //private static float _totalHeight;
        private static int _numLines;
        private static float _heightMultiplyer = ImGui.GetTextLineHeightWithSpacing();
        public static void Display(Entity entity)
        {
            var dblist = entity.DataBlobs;
            string[] stArray = new string[dblist.Count];
            for (int i = 0; i < dblist.Count; i++)
            {
                var db = dblist[i];
                stArray[i] = db.GetType().ToString();

            }
            BorderListOptions.Begin("DataBlobs:", stArray, ref _selectedDB, 300f);

            var p0 = ImGui.GetCursorPos();

            if (_selectedDB >= dblist.Count)
                _selectedDB = -1;
            
            if(_selectedDB >= 0)
                DBDisplay(dblist[_selectedDB]);

            var p1 = ImGui.GetCursorPos();
            var size = new Vector2(ImGui.GetContentRegionAvail().X, p1.Y - p0.Y );
            
            BorderListOptions.End(size);
        }

        public static void DBDisplay(BaseDataBlob dataBlob)
        {
            Type dbType = dataBlob.GetType();

            MemberInfo[] memberInfos = dbType.GetMembers();
            
            var _totalHeight = _numLines * _heightMultiplyer;
            _numLines = memberInfos.Length;
            var size = new Vector2(ImGui.GetContentRegionAvail().X, _totalHeight);
            
            ImGui.BeginChild("InnerColomns", size);
            
            ImGui.Columns(2);

            RecursiveReflection(dataBlob);
            

            ImGui.Columns(0);
            
            ImGui.EndChild();

        }

        static void RecursiveReflection(object obj)
        {
            
            Type objType = obj.GetType();

            MemberInfo[] memberInfos = objType.GetMembers();
            foreach (var memberInfo in memberInfos)
            {
                if (typeof(FieldInfo).IsAssignableFrom(memberInfo.GetType()) || typeof(PropertyInfo).IsAssignableFrom(memberInfo.GetType()))
                {
                    MemberTypes membertype = memberInfo.MemberType;
                    object value = GetValue(memberInfo, obj);
                    if(value == null)
                        continue;
                    if (typeof(IList).IsAssignableFrom(value.GetType()))
                    {
                        var items = (IList)GetValue(memberInfo, obj);
                        int itemsCount = items.Count;
                        
                        if (ImGui.TreeNode(memberInfo.Name))
                        {
                            ImGui.NextColumn();
                            ImGui.Text("Count: " + itemsCount);
                            ImGui.NextColumn();
                            _numLines += itemsCount;
                            foreach (var item in items)
                            {
                                RecursiveReflection(item);
                            }
                            ImGui.TreePop();
                        }
                        else
                        {
                            ImGui.NextColumn();
                            ImGui.Text("Count: " + itemsCount);
                            ImGui.NextColumn();
                        }
                    }
                    else if (typeof(IDictionary).IsAssignableFrom(value.GetType()))
                    {
                        var items = (IDictionary)GetValue(memberInfo, obj);
                        int itemsCount = items.Count;
                        
                        if (ImGui.TreeNode(memberInfo.Name))
                        {
                            ImGui.NextColumn();
                            ImGui.Text("Count: " + itemsCount);
                            ImGui.NextColumn();
                            _numLines += itemsCount;
                            foreach (var item in items)
                            {
                                RecursiveReflection(item);
                            }
                            ImGui.TreePop();
                        }
                        else
                        {
                            ImGui.NextColumn();
                            ImGui.Text("Count: " + itemsCount);
                            ImGui.NextColumn();
                        }
                    }
                    else
                    {
                        ImGui.Text(memberInfo.Name);
                        ImGui.NextColumn();
                        //object value = memberInfo.GetValue(obj);
                        if (value != null)
                            ImGui.Text(value.ToString());
                        else ImGui.Text("null");
                        ImGui.NextColumn();
                    }
                }
            }
        }
        
        static object GetValue(this MemberInfo memberInfo, object forObject)
        {
            switch (memberInfo.MemberType)
            {
                case MemberTypes.Field:
                    return ((FieldInfo)memberInfo).GetValue(forObject);
                case MemberTypes.Property:
                    return ((PropertyInfo)memberInfo).GetValue(forObject);
                    
            }
            return "";
        }
    }

    public static class BorderGroup
    {
        private static Vector2[] _startPos = new Vector2[8];
        private static Vector2[] _labelSize = new Vector2[8];
        private static uint[] _colour = new uint[8];
        private static byte _nestIndex = 0;
        private static float _dentMulitpier = 3;
        private static Vector2[] _size = new Vector2[8];

        public static Vector2 GetSize => _size[_nestIndex];

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
            
            _size[_nestIndex] = new Vector2(width, pos.Y - _startPos[_nestIndex].Y);
            ImDrawListPtr wdl = ImGui.GetWindowDrawList();

            float by = _startPos[_nestIndex].Y + _size[_nestIndex].Y + _dentMulitpier -_dentMulitpier * _nestIndex;
            float rx = _startPos[_nestIndex].X + _size[_nestIndex].X - _dentMulitpier * _nestIndex;
            
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
    
    public static class SizesDemo
    {
        enum FrameOfReference : byte
        {
            Screen,
            Window,
        }
        static ImDrawListPtr _wdl = ImGui.GetForegroundDrawList();
        static Vector2 _windowPos = new Vector2();
        static UInt32 _lineColour = ImGui.GetColorU32(new Vector4(1, 0, 0, 1));
        static UInt32 _pointColour = ImGui.GetColorU32(new Vector4(1, 1, 0, 1));
        
        public static void Display()
        {

            if (ImGui.Begin("Size Demo"))
            {
                
                var getCursorScreenPos1st = ImGui.GetCursorScreenPos();
                var getCursorPos1st = ImGui.GetCursorPos();
                ImGui.Columns(2, "", true);

                
                
                var getCursorStartPos = ImGui.GetCursorStartPos();
                _windowPos = ImGui.GetWindowPos();
                
                
                var getContentRegionMax = ImGui.GetContentRegionMax();
                var getContentRegionAvail = ImGui.GetContentRegionAvail();
                
                var getWindowSize = ImGui.GetWindowSize();
                var getWindowContentRegionMax = ImGui.GetWindowContentRegionMax();
                var getWindowContentRegionMin = ImGui.GetWindowContentRegionMin();
                var getWindowContentRegionWidth = ImGui.GetWindowContentRegionWidth();
                
                var getFontSize = ImGui.GetFontSize();

                var getFrameHeight = ImGui.GetFrameHeight();
                var getFrameHeightWithSpacing = ImGui.GetFrameHeightWithSpacing();

                var getTextLineHeight = ImGui.GetTextLineHeight();
                var getTextLineHeightWithSpacing = ImGui.GetTextLineHeightWithSpacing();

                var getColomnWidth = ImGui.GetColumnWidth();

                var getColomnOffset = ImGui.GetColumnOffset(1);
                
                var itemStartPos = new Vector2();
                

                var cursorScreenStartPos = _windowPos + getCursorStartPos;
                
                DoPoint("GetCursorStartPos", getCursorStartPos, FrameOfReference.Window);
                DoPoint("GetCursorPos (1st call in window)", getCursorPos1st, FrameOfReference.Window);
                
                DoPoint("GetWindowPos", _windowPos, FrameOfReference.Screen);
                DoPoint("GetCursorScreenPos (1st call in window)", getCursorScreenPos1st, FrameOfReference.Screen);
                
                DoPoint("WindowPos + CursorStartPos", _windowPos + getCursorStartPos, FrameOfReference.Screen);
                
                var getCursorScreenPos = ImGui.GetCursorScreenPos();
                DoPoint("GetCursorScreenPos (before this item)", getCursorScreenPos, FrameOfReference.Screen);
                
                var getCursorPos = ImGui.GetCursorPos();
                DoPoint("GetCursorPos (before this item)", getCursorPos, FrameOfReference.Window);
                
                
                DoRectangle("GetWindowSize", _windowPos, getWindowSize);

                var windowContentRegionStart = new Vector2(cursorScreenStartPos.X, _windowPos.Y);//this seems a bit obtuse
                DoRectangle("GetWindowContentRegionMax", windowContentRegionStart, getWindowContentRegionMax);
                DoRectangle("GetWindowContentRegionMin", cursorScreenStartPos, getWindowContentRegionMin);               
                
                DoRectangle("GetContentRegionMax", _windowPos ,getContentRegionMax);
                DoRectangle("GetContentRegionAvail", cursorScreenStartPos, getContentRegionAvail);
                

                
                itemStartPos = ImGui.GetCursorScreenPos();
                DoHLine("GetWindowContentRegionWidth", cursorScreenStartPos, getWindowContentRegionWidth);
                
                
                var colomnWidthstart = new Vector2(_windowPos.X, cursorScreenStartPos.Y);
                DoHLine("GetColomnWidth", colomnWidthstart, getColomnWidth);

                DoHLine("GetColomnOffset (colomn[1])", colomnWidthstart, getColomnOffset);
                
                itemStartPos = ImGui.GetCursorScreenPos();
                DoVLine("GetFontSize", itemStartPos, getFontSize);

                itemStartPos = ImGui.GetCursorScreenPos();
                DoVLine("GetTextLineHeight", itemStartPos, getTextLineHeight);
                
                itemStartPos = ImGui.GetCursorScreenPos();
                DoVLine("GetTextLineHeightWithSpacing", itemStartPos, getTextLineHeightWithSpacing);
                
                itemStartPos = ImGui.GetCursorScreenPos();
                DoVLine("GetFrameHeight", itemStartPos, getFrameHeight);
                
                itemStartPos = ImGui.GetCursorScreenPos();
                DoVLine("GetFrameHeightWithSpacing", itemStartPos, getFrameHeightWithSpacing);
                
                
                //we have to code the following one in full because we need to call GetItemRectSize after Imgui.Text()
                //so we can't just send it off to DoRectangle();
                itemStartPos = ImGui.GetCursorScreenPos();
                ImGui.Text("GetItemRectSize");
                var getItemRectSize = ImGui.GetItemRectSize();
                if (ImGui.IsItemHovered())
                {
                    var endRect = itemStartPos + getItemRectSize;
                    _wdl.AddRect(itemStartPos, endRect, _lineColour);
                    DrawCrosshair(itemStartPos, 3);
                }
                ImGui.NextColumn();
                ImGui.Text(getItemRectSize.ToString());
                ImGui.NextColumn();
                
                
                
                
                //we have to code the following one in full because we need to call GetItemRectSize after Imgui.Text()
                //so we can't just send it off to DoRectangle();
                itemStartPos = ImGui.GetCursorScreenPos();
                ImGui.Text("GetCursorScreenPos before & after");
                var itemEndPos = ImGui.GetCursorScreenPos();
                var height = itemEndPos.Y - itemStartPos.Y;
                if (ImGui.IsItemHovered())
                {
                    _wdl.AddLine(itemStartPos, itemEndPos, _lineColour);
                    DrawCrosshair(itemStartPos, 3);

                }
                ImGui.NextColumn();
                ImGui.Text(height.ToString());
                ImGui.NextColumn();
                
                
                
                ImGui.Columns(0);
                ImGui.NewLine();
                ImGui.NewLine();
                ImGui.Text("Note: DrawList.AddRect takes two positions, not position and size");
                
                ImGui.End();
                
                
                
            }

            void DoPoint(string name, Vector2 point, FrameOfReference foR)
            {
                ImGui.Text(name);
                if (ImGui.IsItemHovered())
                {
                    if(foR == FrameOfReference.Window)
                        DrawCrosshair(_windowPos + point, 3);
                    else
                        DrawCrosshair(point, 3);
                }
                ImGui.NextColumn();
                ImGui.Text(point.ToString());
                ImGui.SameLine();
                if(foR == FrameOfReference.Window)
                    ImGui.Text("Frame of Reference: Window");
                else
                    ImGui.Text("Frame of Reference: Screen");
                ImGui.NextColumn();
            }

            void DoRectangle(string name, Vector2 start, Vector2 size)
            {
                ImGui.Text(name);
                if (ImGui.IsItemHovered())
                {
                    var endRect = start + size;
                    _wdl.AddRect(start, endRect, _lineColour);
                    DrawCrosshair(start, 3);
                }
                ImGui.NextColumn();
                ImGui.Text(size.ToString());
                ImGui.NextColumn();
            }

            void DoHLine(string name, Vector2 start, float width)
            {
                ImGui.Text(name);
                if (ImGui.IsItemHovered())
                {
                    var endPos = start;
                    endPos.X += width;
                    _wdl.AddLine(start, endPos, _lineColour);
                    DrawCrosshair(start, 3);

                }
                ImGui.NextColumn();
                ImGui.Text(width.ToString());
                ImGui.NextColumn();
            }
            
            void DoVLine(string name, Vector2 start, float height)
            {
                ImGui.Text(name);
                if (ImGui.IsItemHovered())
                {
                    var endPos = start;
                    endPos.Y += height;
                    _wdl.AddLine(start, endPos, _lineColour);
                    DrawCrosshair(start, 3);

                }
                ImGui.NextColumn();
                ImGui.Text(height.ToString());
                ImGui.NextColumn();
            }

            void DrawCrosshair(Vector2 atPos, float radius)
            {
                var p1 = new Vector2(atPos.X - radius, atPos.Y);
                var p2 = new Vector2(atPos.X + radius, atPos.Y);
                var p3 = new Vector2(atPos.X, atPos.Y - radius);
                var p4 = new Vector2(atPos.X, atPos.Y + radius);
                _wdl.AddLine(p1, p2, _pointColour);
                _wdl.AddLine(p3, p4, _pointColour);
            }


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
