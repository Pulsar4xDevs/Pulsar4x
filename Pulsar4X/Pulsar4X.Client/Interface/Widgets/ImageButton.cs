using System;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Client;

namespace Pulsar4X.Client.Interface.Widgets;

public static class ImageButton
{
    public struct ButtonColors
    {
        public uint Normal;
        public uint Hovered;
        public uint Active;
        public uint Text;
        public uint Border;

        // Initialize with current ImGui style colors
        public static ButtonColors FromCurrentStyle()
        {
            return new ButtonColors
            {
                Normal = ImGui.GetColorU32(ImGuiCol.Button),
                Hovered = ImGui.GetColorU32(ImGuiCol.ButtonHovered),
                Active = ImGui.GetColorU32(ImGuiCol.ButtonActive),
                Text = ImGui.GetColorU32(ImGuiCol.Text),
                Border = ImGui.GetColorU32(ImGuiCol.Border)
            };
        }
    }

    public static bool Begin(IntPtr textureId, string label, Vector2 imageSize, Vector2 buttonSize)
    {
        bool clicked = false;
        var currentColors = ButtonColors.FromCurrentStyle();
        var style = ImGui.GetStyle();

        if(ImGui.BeginChild(label, buttonSize, ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar))
        {
            float textWidth = ImGui.CalcTextSize(label).X;
            float totalWidth = imageSize.X + textWidth + 5; // spacing 5 pixels between image and text
            float startX = (buttonSize.X - totalWidth) * 0.5f;

            // Create an invisible button to handle the click event
            clicked = ImGui.InvisibleButton(label + "##customInvisibleButton", buttonSize);

            var drawList = ImGui.GetWindowDrawList();
            var rectMin = ImGui.GetItemRectMin();
            var rectMax = ImGui.GetItemRectMax();

            /// Draw the background based on state
            uint bgColor;
            if (ImGui.IsItemActive())
            {
                bgColor = currentColors.Active;
            }
            else if (ImGui.IsItemHovered())
            {
                bgColor = currentColors.Hovered;
            }
            else
            {
                bgColor = currentColors.Normal;
            }

            // Draw filled background with rounding
            drawList.AddRectFilled(
                rectMin,
                rectMax,
                bgColor,
                style.FrameRounding);

            // Draw border if thickness > 0
            if (style.FrameBorderSize > 0)
            {
                drawList.AddRect(
                    rectMin,
                    rectMax,
                    currentColors.Border,
                    style.FrameRounding,
                    ImDrawFlags.None,
                    style.FrameBorderSize);
            }

            // Draw the image & text
            float cursorY = ImGui.GetCursorPosY() - buttonSize.Y + imageSize.Y * 0.25f;
            ImGui.SetCursorPos(new Vector2(startX, cursorY));
            ImGui.Image(textureId.ToTextureRef(), imageSize);
            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Text, currentColors.Text);
            ImGui.Text(label);
            ImGui.PopStyleColor();
        }
        ImGui.EndChild();

        return clicked;
    }
}