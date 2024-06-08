using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using Pulsar4X.Blueprints;
using Pulsar4X.DataStructures;
using Pulsar4X.Modding;

namespace Pulsar4X.SDL2UI.ModFileEditing;

public class ComponentBluprintUI : BluePrintsUI
{
    private AttributeBlueprintUI? _attributeBlueprintUI;
    private List<ComponentTemplateAttributeBlueprint> _selectedAttributes;
    public ComponentBluprintUI(ModDataStore modDataStore) : base(modDataStore, ModInstruction.DataType.ComponentTemplate)
    {
        Dictionary<string, ComponentTemplateBlueprint> blueprints = modDataStore.ComponentTemplates;
        _itemBlueprints = blueprints.Values.ToArray();
        Refresh();
    }
    public sealed override void Refresh()
    {
        _itemNames = new string[_itemBlueprints.Length];
        _isActive = new bool[_itemBlueprints.Length];
        int i = 0;
        foreach (ComponentTemplateBlueprint item in _itemBlueprints)
        {
            _itemNames[i] = item.Name;
            _isActive[i] = false;
            i++;
        }
        var newEmpty = new ComponentTemplateBlueprint();
        newEmpty.Name = "New Blueprint";
        _newEmpty = newEmpty;
    }
    

    public override void DisplayEditorWindow(int selectedIndex)
    {

        if (!_isActive[selectedIndex])
            return;
        var selectedItem = (ComponentTemplateBlueprint)_itemBlueprints[selectedIndex];
        _selectedAttributes = selectedItem.Attributes;
        
        if(_attributeBlueprintUI == null)
            _attributeBlueprintUI = new AttributeBlueprintUI(_modDataStore, selectedItem);
            
        string name = selectedItem.Name;
        string editStr;
        if (ImGui.Begin("Tech Category Editor: " + name, ref _isActive[selectedIndex]))
        {
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0, 150);
            ImGui.SetColumnWidth(1, 500);
            
            ImGui.Text("Name: ");
            ImGui.NextColumn();
            editStr = selectedItem.Name;
            if (TextEditWidget.Display("##name" + selectedItem.Name, ref editStr))
            {
                selectedItem.Name = editStr;
            }
            ImGui.NextColumn();


            ImGui.Text("ComponentType: ");
            ImGui.NextColumn();
            editStr = selectedItem.ComponentType;
            if (TextEditWidget.Display("##cmpt" + selectedItem.ComponentType, ref editStr))
            {
                selectedItem.Name = editStr;
            }
            ImGui.NextColumn();

            
            ImGui.Text("CargoType: ");
            ImGui.NextColumn();
            _editInt = Array.IndexOf(_cargoTypes, selectedItem.CargoTypeID);
            if (SelectFromListWiget.Display("##cgot" + selectedItem.CargoTypeID, _cargoTypes, ref _editInt))
            {
                selectedItem.Name = _cargoTypes[_editInt];
            }
            ImGui.NextColumn();

            
            ImGui.Text("Fomula: ");
            ImGui.NextColumn();
            var editDicf = selectedItem.Formulas;
            if (DictEditWidget.Display("##fmula", ref editDicf))
            {
                selectedItem.Formulas = editDicf;
            }
            ImGui.NextColumn();
            
            
            ImGui.Text("ResourceCosts: ");
            ImGui.NextColumn();
            var editDic = selectedItem.ResourceCost;
            if (DictEditWidget.Display("##resc", ref editDic))
            {
                selectedItem.ResourceCost = editDic;
            }
            ImGui.NextColumn();
            
            
            ImGui.Text("IndustryType: ");
            ImGui.NextColumn();
            _editInt = Array.IndexOf(_industryTypes, selectedItem.IndustryTypeID);
            if (SelectFromListWiget.Display("##indt" + selectedItem.IndustryTypeID, _industryTypes, ref _editInt))
            {
                selectedItem.IndustryTypeID = _industryTypes[_editInt];
            }
            ImGui.NextColumn();
            
            
            ImGui.Text("MountType: ");
            ImGui.NextColumn();
            _editInt = Array.IndexOf(_mountTypes, selectedItem.MountType);
            if (SelectFromListWiget.Display("##mntt" + selectedItem.UniqueID, _mountTypes, ref _editInt))
            {

                if(Enum.TryParse(typeof(ComponentMountType), _mountTypes[_editInt], out var mtype))
                    selectedItem.MountType = (ComponentMountType)mtype;
            }
            ImGui.NextColumn();
            
            _attributeBlueprintUI.Display();

            ImGui.End();
        }
    }
}