using ImGuiNET;

namespace Pulsar4X.SDL2UI.ModFileEditing;

public static class SelectFromListWiget
{
    private static string? _editingID;
    private static int _currentItem;
    private static string[] _items;
    private static int _itemCount;
    
    public static bool Display(string label, string[] selectFrom, ref int selected)
    {
        bool hasChanged = false;
        string displayText = "null";
        if(selected > -1)   
            displayText = selectFrom[selected];
        if (label != _editingID)
        {
            ImGui.Text(displayText);
            if(ImGui.IsItemClicked())
            {
                _editingID = label;
                _items = selectFrom;
                _itemCount = _items.Length;
            }
        }
        else
        {
            ImGui.Text(displayText);
            ImGui.SameLine();
            if (ImGui.ListBox(label, ref _currentItem, _items, _itemCount))
            {
                selected = _currentItem;
                _editingID = null;
                hasChanged = true;
            }
        }
        return hasChanged;
    }
}