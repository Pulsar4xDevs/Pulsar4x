using ImGuiNET;

namespace Pulsar4X.Client.ModFileEditing;

public static class DoubleEditWidget
{
    private static string? _editingID;

    public static bool Display(string label, ref double num, string format = "%.2f", bool exitEditOnFocusLoss = true)
    {
        bool hasChanged = false;
        if(label != _editingID)
        {
            ImGui.Text(num.ToString());
            if(ImGui.IsItemClicked())
            {
                _editingID = label;
                ImGui.SetKeyboardFocusHere(0); // Ensure focus on the input field when editing starts
            }
        }
        else
        {
            double tempNum = num;
            if (ImGui.InputDouble(label, ref tempNum, 1, 1, format))
            {
                num = tempNum;
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

public static class FloatEditWidget
{
    private static string? _editingID;

    public static bool Display(string label, ref float num, string format = "%.2f", bool exitEditOnFocusLoss = true)
    {
        bool hasChanged = false;
        if(label != _editingID)
        {
            ImGui.Text(num.ToString());
            if(ImGui.IsItemClicked())
            {
                _editingID = label;
                ImGui.SetKeyboardFocusHere(0); // Ensure focus on the input field when editing starts
            }
        }
        else
        {
            float tempNum = num;
            if (ImGui.InputFloat(label, ref tempNum, 1.0f, 1.0f, format))
            {
                num = tempNum;
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