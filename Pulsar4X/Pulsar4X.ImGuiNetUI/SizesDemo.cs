using System;
using System.Numerics;
using ImGuiNET;

namespace Pulsar4X.SDL2UI;

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
            var getWindowContentRegionWidth = ImGui.GetWindowContentRegionMax().X - getWindowContentRegionMin.X;
                
            var getFontSize = ImGui.GetFontSize();

            var getFrameHeight = ImGui.GetFrameHeight();
            var getFrameHeightWithSpacing = ImGui.GetFrameHeightWithSpacing();

            var getTextLineHeight = ImGui.GetTextLineHeight();
            var getTextLineHeightWithSpacing = ImGui.GetTextLineHeightWithSpacing();

            var getColomnWidth = ImGui.GetColumnWidth();

            var getColomnOffset = ImGui.GetColumnOffset(1);
                
            var itemStartPos = new System.Numerics.Vector2();

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