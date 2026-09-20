using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using Pulsar4X.Blueprints;
using Pulsar4X.Modding;

namespace Pulsar4X.Client.ModFileEditing;

public class TechBlueprintUI : BluePrintsUI
{
    private int _selectedIndex = -1;
    public TechBlueprintUI(ModDataStore modDataStore) : base(modDataStore, ModInstruction.DataType.Tech)
    {
        Dictionary<string, TechBlueprint> blueprints = modDataStore.Techs;
        _itemBlueprints = blueprints.Values.ToArray();
        Refresh();
    }

    public override void Refresh()
    {
        _itemNames = new string[_itemBlueprints.Length];
        _isActive = new bool[_itemBlueprints.Length];
        int i = 0;
        foreach (TechBlueprint item in _itemBlueprints)
        {
            _itemNames[i] = item.Name;
            _isActive[i] = false;
            i++;
        }
        var newEmpty = new TechBlueprint();
        newEmpty.Name = "New Blueprint";
        _newEmpty = newEmpty;
    }


    public override void DisplayEditorWindow(int selectedIndex)
    {

        if(!_isActive[selectedIndex])
            return;
        var selectedItem = (TechBlueprint)_itemBlueprints[selectedIndex];
        string name = selectedItem.Name;
        string editStr;
        if (ImGui.Begin($"Tech Editor: {name}###{selectedItem.UniqueID}", ref _isActive[selectedIndex]))
        {
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0,150);
            ImGui.SetColumnWidth(1,500);
            ImGui.Text("Name: ");
            ImGui.NextColumn();

            editStr = selectedItem.Name;
            if (TextEditWidget.Display("##name" + selectedItem.UniqueID, ref editStr))
            {
                selectedItem.Name = editStr;
            }
            ImGui.NextColumn();


            ImGui.Text("Description: ");
            ImGui.NextColumn();
            editStr = selectedItem.Description;
            if (TextEditWidget.Display("##desc" + selectedItem.UniqueID, ref editStr))
            {
                selectedItem.Description = editStr;
            }
            ImGui.NextColumn();


            ImGui.Text("Category: ");
            ImGui.NextColumn();

            _selectedIndex = Array.IndexOf(_techCatTypes, selectedItem.Category);
            if (SelectFromListWiget.Display("##cat" + selectedItem.Category, _techCatTypes, ref _selectedIndex))
            {
                selectedItem.Category = _techCatTypes[_selectedIndex];
                _selectedIndex = -1;
            }
            ImGui.NextColumn();


            ImGui.Text("CostFormula: ");
            ImGui.NextColumn();
            editStr = selectedItem.CostFormula;
            if (FunctionEditWidget.Display("##cf" + selectedItem.UniqueID, ref editStr, _modDataStore, new string[0]))
            {
                selectedItem.CostFormula = editStr;
            }
            ImGui.NextColumn();


            ImGui.Text("DataFormula: ");
            ImGui.NextColumn();
            editStr = selectedItem.DataFormula;
            if (FunctionEditWidget.Display("##df" + selectedItem.UniqueID, ref editStr, _modDataStore, new string[0]))
            {
                selectedItem.DataFormula = editStr;
            }
            ImGui.NextColumn();


            ImGui.Text("MaxLevel: ");
            ImGui.NextColumn();
            int editInt = selectedItem.MaxLevel;
            if (IntEditWidget.Display("##ml" + selectedItem.MaxLevel, ref editInt))
            {
                selectedItem.MaxLevel = editInt;
            }
            ImGui.NextColumn();

            ImGui.Text("Unlocks: ");
            ImGui.NextColumn();
            var editDic = selectedItem.Unlocks;
            if (DictEditWidget.Display("##ul" + selectedItem.Name, ref editDic, _techTypes))
            {

            }


            ImGui.End();
        }
    }
}