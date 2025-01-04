using System;
using System.Numerics;
using ImGuiNET;

namespace Pulsar4X.Client.Interface.Widgets;

public static class ImageButton
{
    public static bool Render(IntPtr textureId, string label, Vector2 imageSize, Vector2 buttonSize)
    {
        bool clicked = false;

        if(ImGui.BeginChild(label, buttonSize, false, ImGuiWindowFlags.NoScrollbar))
        {
            float textWidth = ImGui.CalcTextSize(label).X;
            float totalWidth = imageSize.X + textWidth + 5; // spacing 5 pixels between image and text
            float startX = (buttonSize.X - totalWidth) * 0.5f;

            // Create an invisible button to handle the click event
            clicked = ImGui.InvisibleButton(label + "##customInvisibleButton", buttonSize);

            // Draw the background if hovered/acitve
            if (ImGui.IsItemHovered() || ImGui.IsItemActive())
            {
                ImGui.GetWindowDrawList().AddRectFilled(
                    ImGui.GetItemRectMin(),
                    ImGui.GetItemRectMax(),
                    ImGui.GetColorU32(ImGui.IsItemActive() ? ImGuiCol.ButtonActive : ImGuiCol.ButtonHovered));
            }
            else
            {
                ImGui.GetWindowDrawList().AddRectFilled(
                    ImGui.GetItemRectMin(),
                    ImGui.GetItemRectMax(),
                    ImGui.GetColorU32(ImGuiCol.Button));
            }

            // Draw the image & text
            float cursorY = ImGui.GetCursorPosY() - buttonSize.Y + imageSize.Y * 0.25f;
            ImGui.SetCursorPos(new Vector2(startX, cursorY));
            ImGui.Image(textureId, imageSize);
            ImGui.SameLine();
            //ImGui.SetCursorPosY(cursorY + (buttonSize.Y - ImGui.GetTextLineHeight()) * 0.5f);
            ImGui.Text(label);
            ImGui.EndChild();
        }

        return clicked;
    }
}