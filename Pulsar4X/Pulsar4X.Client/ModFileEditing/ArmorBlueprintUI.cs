using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using Pulsar4X.Blueprints;
using Pulsar4X.Modding;

namespace Pulsar4X.SDL2UI.ModFileEditing;

public class ArmorBlueprintUI : BluePrintsUI
{
    public ArmorBlueprintUI(ModDataStore modDataStore) : base(modDataStore, ModInstruction.DataType.Armor)
    {
        Dictionary<string, ArmorBlueprint> blueprints = _modDataStore.Armor;
        _itemBlueprints = blueprints.Values.ToArray();
        Refresh();
    }

    public sealed override void Refresh()
    {
        _itemNames = new string[_itemBlueprints.Length];
        _isActive = new bool[_itemBlueprints.Length];
        int i = 0;
        foreach (ArmorBlueprint item in _itemBlueprints)
        {
            _itemNames[i] = item.UniqueID;
            _isActive[i] = false;
            i++;
        }
        var newEmpty = new ArmorBlueprint();
        newEmpty.UniqueID = "New Blueprint";
        _newEmpty = newEmpty;
    }
    

    public override void DisplayEditorWindow(int selectedIndex)
    {
        if(!_isActive[selectedIndex])
            return;
        var selectedItem = (ArmorBlueprint)_itemBlueprints[selectedIndex];
        _editStr = selectedItem.UniqueID;
        //string desc = selectedItem.Description;
        if (ImGui.Begin("Tech Category Editor: " + _editStr, ref _isActive[selectedIndex]))
        {
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0,150);
            ImGui.SetColumnWidth(1,500);
            ImGui.Text("Name: ");
            ImGui.NextColumn();
            if (TextEditWidget.Display("##name" + selectedItem.UniqueID, ref _editStr))
            {
                selectedItem.UniqueID = _editStr;
            }
            
            ImGui.NextColumn();
            ImGui.Text("ResourceID: ");
            ImGui.NextColumn();
            _editStr = selectedItem.ResourceID;
            if (TextEditWidget.Display("##resourceid" + selectedItem.UniqueID, ref _editStr))
            {
                selectedItem.ResourceID = _editStr;
            }
            
            
            ImGui.NextColumn();
            ImGui.Text("Density: ");
            ImGui.NextColumn();
            var editDoub = selectedItem.Density;
            if (DoubleEditWidget.Display("##density"+selectedItem.UniqueID, ref editDoub))
            {
                selectedItem.Density = editDoub;
            }

            ImGui.End();
        }
    }
}