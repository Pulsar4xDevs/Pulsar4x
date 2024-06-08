using ImGuiNET;

namespace Pulsar4X.SDL2UI.ModFileEditing;

public static class IntEditWidget
{
    private static string? _editingID;
    
    public static bool Display(string label, ref int num)
    {
        bool hasChanged = false;
        if(label != _editingID)
        {
            ImGui.Text(num.ToString());
            if(ImGui.IsItemClicked())
            {
                _editingID = label;
            }
        }
        else
        {
            if (ImGui.InputInt(label, ref num, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                _editingID = null;
                hasChanged = true;
            }
        }

        return hasChanged;
    }
}