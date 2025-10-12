using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Blueprints;
using Pulsar4X.Modding;

namespace Pulsar4X.Client.ModFileEditing;

public class ShipDesignBlueprintUI : BluePrintsUI
{
    string[] _armorBlueprints;
    public ShipDesignBlueprintUI(ModDataStore modDataStore) : base(modDataStore, ModInstruction.DataType.ShipDesign)
    {
        Dictionary<string, ShipDesignBlueprint> blueprints = _modDataStore.ShipDesigns;
        _itemBlueprints = blueprints.Values.ToArray();
        Refresh();
    }

    public override void Refresh()
    {
        _itemNames = new string[_itemBlueprints.Length];
        _isActive = new bool[_itemBlueprints.Length];
        _armorBlueprints = new string[_modDataStore.Armor.Count];
        int i = 0;
        foreach (var kvp in _modDataStore.Armor)
        {
            _armorBlueprints[i]=kvp.Value.UniqueID;
            i++;
        }
        i = 0;
        foreach (ShipDesignBlueprint item in _itemBlueprints)
        {
            _itemNames[i] = item.Name;
            _isActive[i] = false;
            i++;
        }
        var newEmpty = new ShipDesignBlueprint();
        newEmpty.Name = "New Blueprint";
        _newEmpty = newEmpty;

    }

    public override void DisplayEditorWindow(int selectedIndex)
    {
        if (!_isActive[selectedIndex])
            return;
        var selectedItem = (ShipDesignBlueprint)_itemBlueprints[selectedIndex];

        string name = selectedItem.Name;
        string editStr;
        ImGui.SetNextWindowSize(new Vector2(1500, 900));
        if (ImGui.Begin("Ship Editor: " + name, ref _isActive[selectedIndex]))
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
            ImGui.Text("Armor:");
            ImGui.NextColumn();
            ImGui.Text("Thickness:");
            ImGui.NextColumn();
            int thinkness = (int)selectedItem.Armor.Thickness;
            _editInt = Array.IndexOf(_armorBlueprints, selectedItem.Armor.Id);
            if (SelectFromListWiget.Display("##armor", _armorBlueprints, ref _editInt))
            {
                selectedItem.Armor = new ShipDesignBlueprint.ShipArmorBlueprint() 
                    { Id = _armorBlueprints[_editInt], Thickness = (uint)thinkness, };
            }
            ImGui.NextColumn();
            if (IntEditWidget.Display("##thinkness", ref thinkness, int.MaxValue, (int)uint.MinValue))
            {
                selectedItem.Armor = new ShipDesignBlueprint.ShipArmorBlueprint() 
                    { Id = _armorBlueprints[_editInt], Thickness = (uint)thinkness, };
            }
            
            
            
            
            
        }
        ImGui.End();
    }
}