using System.Linq;
using ImGuiNET;
using Pulsar4X.Blueprints;
using Pulsar4X.Modding;

namespace Pulsar4X.SDL2UI.ModFileEditing;

public class CargoTypeBlueprintUI : BluePrintsUI
{
    
    
    public CargoTypeBlueprintUI(ModDataStore modDataStore) : base(modDataStore, ModInstruction.DataType.CargoType)
    {
        var blueprints = modDataStore.CargoTypes;
        _itemBlueprints = blueprints.Values.ToArray();
        
        Refresh();
    }

    public override void Refresh()
    {
        _itemNames = new string[_itemBlueprints.Length];
        _isActive = new bool[_itemBlueprints.Length];
        int i = 0;
        foreach (CargoTypeBlueprint item in _itemBlueprints)
        {
            _itemNames[i] = item.Name;
            _isActive[i] = false;
            i++;
        }
        var newEmpty = new CargoTypeBlueprint();
        newEmpty.Name = "New Blueprint";
        _newEmpty = newEmpty;
    }
    

    public override void DisplayEditorWindow(int selectedIndex)
    {

        if (!_isActive[selectedIndex])
            return;
        var selectedItem = (CargoTypeBlueprint)_itemBlueprints[selectedIndex];

        if (ImGui.Begin("Cargo Type Editor: " + selectedItem.Name, ref _isActive[selectedIndex]))
        {
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0, 150);
            ImGui.SetColumnWidth(1, 500);

            ImGui.Text("UniqueID: ");
            ImGui.NextColumn();

            _editStr = selectedItem.UniqueID;
            if (TextEditWidget.Display("##name" + selectedItem.UniqueID, ref _editStr))
            {
                selectedItem.UniqueID = _editStr;
            }

            ImGui.NextColumn();

            ImGui.Text("Name: ");
            ImGui.NextColumn();

            _editStr = selectedItem.Name;
            if (TextEditWidget.Display("##name" + selectedItem.Name, ref _editStr))
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
            ImGui.End();
        }
    }
}