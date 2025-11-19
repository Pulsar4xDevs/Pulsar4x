using System;
using System.Collections.Generic;
using ImGuiNET;

namespace Pulsar4X.Client.ModFileEditing;

public static class SelectFromListWiget
{
    private static string? _editingID;
    private static int _currentItem;
    private static string[] _items;
    private static int _itemCount;

    public static bool Display(string label, string[] selectFrom, ref int selected, string noselectText = "null")
    {
        bool hasChanged = false;
        string displayText = noselectText;
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
    
    public static bool Display<TEnum>(string label, ref TEnum selected) where TEnum : struct, Enum
    {
        bool hasChanged = false;
        string displayText = selected.ToString();
        string[] items = Enum.GetNames(typeof(TEnum));
        int itemCount = items.Length;
        int currentItem = Array.IndexOf(items, displayText);

        if (label != _editingID)
        {
            ImGui.Text(displayText);
            if (ImGui.IsItemClicked())
            {
                _editingID = label;
                _items = items;
                _itemCount = itemCount;
                _currentItem = currentItem;
            }
        }
        else
        {
            ImGui.Text(displayText);
            ImGui.SameLine();
            if (ImGui.ListBox(label, ref _currentItem, _items, _itemCount))
            {
                selected = Enum.Parse<TEnum>(_items[_currentItem]);
                _editingID = null;
                hasChanged = true;
            }
        }
        return hasChanged;
    }
}

public static class SelectMultipleFromListWidget
{
    private static string? _editingID;
    private static int _currentItem;
    private static string[] _items;
    private static int _itemCount;

    public static bool Display(string label, string[] selectFrom, ref List<bool> selected)
    {
        bool hasChanged = false;
        string displayText = "null";

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
                //selected = _currentItem;
                _editingID = null;
                hasChanged = true;
            }
        }
        return hasChanged;
    }
}