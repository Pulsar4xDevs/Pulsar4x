using ImGuiNET;

namespace Pulsar4X.Client.ModFileEditing;

public static class IntEditWidget
{
    private static string? _editingID;
    public static bool Display(string label, ref int num, bool exitEditOnFocusLoss = true)
    {
        bool hasChanged = false;
        if (label != _editingID)
        {
            ImGui.Text(num.ToString());
            if (ImGui.IsItemClicked())
            {
                _editingID = label;
                ImGui.SetKeyboardFocusHere(0); // Ensure focus on the input field when editing starts
            }
        }
        else
        {
            int tempNum = num;
            if (ImGui.InputInt(label, ref tempNum, 1, 1))
            {
                num = tempNum; // Update the reference only if input changes
                hasChanged = true;
            }

            // Exit editing mode only on Enter or KeypadEnter
            if (ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter))
            {
                _editingID = null;
            }
            //Exit editing mode if the input loses focus (e.g., clicking elsewhere)
            if (exitEditOnFocusLoss && !ImGui.IsItemActive() && ImGui.IsMouseClicked(0))
            {
                _editingID = null;
            }
        }
        return hasChanged;
    }
}