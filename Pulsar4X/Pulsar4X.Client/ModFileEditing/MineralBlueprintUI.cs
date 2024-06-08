using System;
using System.Linq;
using ImGuiNET;
using Pulsar4X.Blueprints;
using Pulsar4X.Modding;

namespace Pulsar4X.SDL2UI.ModFileEditing;

public class MineralBlueprintUI : BluePrintsUI
{
    public MineralBlueprintUI(ModDataStore modDataStore) : base(modDataStore, ModInstruction.DataType.Mineral)
    {
        var blueprints = modDataStore.Minerals;
        _itemBlueprints = blueprints.Values.ToArray();
        Refresh();
    }

    public override void Refresh()
    {
        _itemNames = new string[_itemBlueprints.Length];
        _isActive = new bool[_itemBlueprints.Length];
        int i = 0;
        foreach (MineralBlueprint item in _itemBlueprints)
        {
            _itemNames[i] = item.Name;
            _isActive[i] = false;
            i++;
        }
        var newEmpty = new MineralBlueprint();
        newEmpty.Name = "New Blueprint";
        _newEmpty = newEmpty;
    }


    public override void DisplayEditorWindow(int selectedIndex)
    {
        if(!_isActive[selectedIndex])
            return;
        var selectedItem = (MineralBlueprint)_itemBlueprints[selectedIndex];

        if (ImGui.Begin("Processed Materials Editor: " + selectedItem.Name, ref _isActive[selectedIndex]))
        {
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0, 150);
            ImGui.SetColumnWidth(1, 500);
            ImGui.Text("Name: ");
            ImGui.NextColumn();
            
            
            _editStr = selectedItem.Name;
            if (TextEditWidget.Display("##name" + selectedItem.UniqueID, ref _editStr))
            {
                selectedItem.Name = _editStr;
            }
            ImGui.NextColumn();
            
            
            ImGui.Text("Description: ");
            ImGui.NextColumn();
            _editStr = selectedItem.Description;
            if (TextEditWidget.Display("##desc" + selectedItem.UniqueID, ref _editStr))
            {
                selectedItem.Name = _editStr;
            }
            ImGui.NextColumn();
            
            ImGui.Text("Cargo Type: ");
            ImGui.NextColumn();
            _editInt = Array.IndexOf(_cargoTypes, selectedItem.CargoTypeID);
            if (SelectFromListWiget.Display("##ctype" + selectedItem.UniqueID, _cargoTypes, ref _editInt))
            {
                selectedItem.CargoTypeID = _cargoTypes[_editInt];
                _editInt = -1;
            }
            ImGui.NextColumn();
                        
            ImGui.Text("Mass: ");
            ImGui.NextColumn();
            _editInt = (int)selectedItem.MassPerUnit;
            if (IntEditWidget.Display("##mass" + selectedItem.UniqueID, ref _editInt))
            {
                selectedItem.MassPerUnit = _editInt;
            }
            ImGui.NextColumn();
            
            ImGui.Text("Volume: ");
            ImGui.NextColumn();
            var editDouble= selectedItem.VolumePerUnit;
            if (DoubleEditWidget.Display("##vol" + selectedItem.UniqueID, ref editDouble))
            {
                selectedItem.VolumePerUnit = editDouble;
            }
            ImGui.NextColumn();
            
            ImGui.Text("Abundance: ");
            ImGui.NextColumn();
            var editDict = selectedItem.Abundance;
            if (DictEditWidget.Display("##Abundance" + selectedItem.UniqueID, ref editDict))
            {
                selectedItem.Abundance = editDict;
            }
            ImGui.NextColumn();
            
            ImGui.End();
        }
    }
}