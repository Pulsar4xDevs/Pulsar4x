using ImGuiNET;
using ImGuiSDL2CS;

namespace Pulsar4X.SDL2UI.ModFileEditing;

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
    
    public static bool Display(string label, ref string text)
    {
        bool hasChanged = false;
        if(string.IsNullOrEmpty(text))
            text = "null";
        if(label != _editingID)
        {
            ImGui.Text(text);
            if(ImGui.IsItemClicked())
            {
                _editingID = label;
                _strInputBuffer = ImGuiSDL2CSHelper.BytesFromString(text);

            }
        }
        else
        {
            if (ImGui.InputText(label, _strInputBuffer, _buffSize, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                text = ImGuiSDL2CSHelper.StringFromBytes(_strInputBuffer);
                _editingID = null;
                hasChanged = true;
            }
        }

        return hasChanged;
    }
}