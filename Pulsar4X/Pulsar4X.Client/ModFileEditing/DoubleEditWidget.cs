using ImGuiNET;

namespace Pulsar4X.SDL2UI.ModFileEditing;

public static class DoubleEditWidget
{
    private static string? _editingID;
    
    public static bool Display(string label, ref double num, string format = "")
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
            if (ImGui.InputDouble(label, ref num, 1, 1, format, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                _editingID = null;
                hasChanged = true;
            }
        }

        return hasChanged;
    }
}