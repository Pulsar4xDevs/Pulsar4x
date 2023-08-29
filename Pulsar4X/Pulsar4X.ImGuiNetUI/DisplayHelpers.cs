using ImGuiNET;

namespace Pulsar4X.SDL2UI
{
    public static class DisplayHelpers
    {
        public static void Header(string text, string tooltip = null)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
            ImGui.Text(text);
            if(!string.IsNullOrEmpty(tooltip))
            {
                ImGui.SameLine();
                ImGui.Text("[?]");
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip(tooltip);
            }
            ImGui.PopStyleColor();
            ImGui.Separator();
        }
    }
}