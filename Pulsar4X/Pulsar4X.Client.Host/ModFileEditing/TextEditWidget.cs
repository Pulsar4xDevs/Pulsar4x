using ImGuiNET;

namespace Pulsar4X.Client.ModFileEditing;

public static class TextEditWidget
{
    private static uint _buffSize = 128;
    private static byte[] _strInputBuffer = new byte[128];
    private static string? _editingID;

    public static uint BufferSize
    {
        get { return _buffSize ;}
        set
        {
            _buffSize = value;
            _strInputBuffer = new byte[value];
        }
    }

    public static bool Display(string label, ref string text, bool exitEditOnFocusLoss = true)
    {
        bool hasChanged = false;
        bool doneEditing = false;
        if(string.IsNullOrEmpty(text))
            text = "null";
        if(label != _editingID)
        {
            ImGui.Text(text);
            if(ImGui.IsItemClicked())
            {
                _editingID = label;
                _strInputBuffer = Utils.BytesFromString(text);
                ImGui.SetKeyboardFocusHere(0); // Ensure focus on the input field when editing starts
            }
        }
        else
        {
            if (ImGui.InputText(label, _strInputBuffer, _buffSize))
            {
                text = Utils.StringFromBytes(_strInputBuffer);
                hasChanged = true;
            }
            // Exit editing mode only on Enter or KeypadEnter
            if (ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter))
            {
                _editingID = null;
                text = Utils.StringFromBytes(_strInputBuffer);
                doneEditing = true;
            }
            //Exit editing mode if the input loses focus (e.g., clicking elsewhere)
            if (exitEditOnFocusLoss && !ImGui.IsItemActive() && ImGui.IsMouseClicked(0))
            {
                _editingID = null;
                text = Utils.StringFromBytes(_strInputBuffer);
                doneEditing = true;
            }
        }

        return doneEditing;
    }
}