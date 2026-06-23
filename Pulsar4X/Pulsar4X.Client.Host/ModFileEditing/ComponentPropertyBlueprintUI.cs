using System;
using System.Linq;
using ImGuiNET;
using Pulsar4X.Blueprints;
using Pulsar4X.DataStructures;
using Pulsar4X.Interfaces;
using Pulsar4X.Modding;

namespace Pulsar4X.Client.ModFileEditing;

public class ComponentPropertyBlueprintUI : BluePrintsUI
{
    public string ParentID {get; private set;}
    private ComponentTemplatePropertyBlueprint[] _blueprints;
    private string[] _attributeTypeNames;
    private string[] _attributeFullNames;
    public ComponentPropertyBlueprintUI(ModDataStore modDataStore, ComponentTemplateBlueprint componentBlueprint) : base(modDataStore, ModInstruction.DataType.ComponentTemplate )
    {
        ParentID = componentBlueprint.UniqueID;
        if(componentBlueprint.Properties != null)
            _blueprints = componentBlueprint.Properties.ToArray();
        else
            _blueprints = new ComponentTemplatePropertyBlueprint[1];
        
        Refresh();
    }



    public sealed override void Refresh()
    {
        _itemNames = new string[_blueprints.Length];
        _isActive = new bool[_blueprints.Length];
        int i = 0;
        foreach (ComponentTemplatePropertyBlueprint item in _blueprints)
        {
            if (item is null)
                _itemNames[i] = "?";
            else
                _itemNames[i] = item.Name;
            _isActive[i] = false;
            i++;
        }
        //var newEmpty = new ComponentTemplateAttributeBlueprint();
        //newEmpty.Name = "New Blueprint";
        //_newEmpty = newEmpty;

        var type = typeof(IComponentDesignAttribute);
        var attributeTypes = AppDomain.CurrentDomain.GetAssemblies()
                                      .SelectMany(s => s.GetTypes())
                                      .Where(p => type.IsAssignableFrom(p));
        _attributeTypeNames = new string[attributeTypes.Count()];
        _attributeFullNames = new string[attributeTypes.Count()];
        i = 0;
        foreach (var item in attributeTypes)
        {
            _attributeTypeNames[i] = item.Name;
            _attributeFullNames[i] = item.FullName;
            i++;
        }
    }

    public void Display()
    {
        ImGui.Columns(2);
        ImGui.SetColumnWidth(0,150);
        //ImGui.SetColumnWidth(1,400);

        
        for (int i = 0; i < _blueprints.Length; i++)
        {
            ImGui.Text("Properties: ");
            ImGui.NextColumn();
            DisplayEditorWindow(i);
        }

        if (ImGui.Button("Add New"))
        {
            var newItem = new ComponentTemplatePropertyBlueprint();
            
            var newblueprints = new ComponentTemplatePropertyBlueprint[_blueprints.Length + 1];
            Array.Copy(_blueprints, newblueprints, _blueprints.Length);
            newblueprints[_blueprints.Length] = newItem;
            _blueprints = newblueprints;
        }
        ImGui.Columns(1);
    }

    public override void DisplayEditorWindow(int selectedIndex)
    {
        var selectedItem = _blueprints[selectedIndex];
        if (selectedItem is null)
        {
            selectedItem = new ComponentTemplatePropertyBlueprint();
            selectedItem.Name = "newProperty";
        }
        
        string editStr;
        ImGui.NextColumn();
        ImGui.Text("Name: ");
        ImGui.NextColumn();
        editStr = selectedItem.Name;
        if (TextEditWidget.Display("##name" + selectedItem.Name, ref editStr))
        {
            selectedItem.Name = editStr;
        }
        ImGui.NextColumn();
        
        ImGui.Text("DescriptionFormula: ");
        ImGui.NextColumn();
        editStr = selectedItem.DescriptionFormula;
        if (FunctionEditWidget.Display("##descf"  + selectedItem.Name, ref editStr, _modDataStore, _itemNames))
        {
            selectedItem.DescriptionFormula = editStr;
        }
        ImGui.NextColumn();
        
        ImGui.Text("GUIHint: ");
        ImGui.NextColumn();
        GuiHint _hint = selectedItem.GuiHint;
        if (SelectFromListWiget.Display<GuiHint>("##ghint" + selectedItem.Name, ref _hint))
        {
            selectedItem.GuiHint = _hint;
        }
        ImGui.NextColumn();
        
        
        ImGui.Text("Units: ");
        ImGui.NextColumn();
        var editIndex = Array.IndexOf(_units, selectedItem.Units);
        if (SelectFromListWiget.Display("##units" + selectedItem.Name, _units, ref editIndex))
        {
            selectedItem.Units = _units[editIndex];
        }
        ImGui.NextColumn();

        if (selectedItem.GuiHint.HasFlag(GuiHint.GuiSelectionMaxMin) || selectedItem.GuiHint.HasFlag(GuiHint.GuiSelectionMaxMinInt))
        {
            ImGui.Text("MaxFormula: ");
            ImGui.NextColumn();
            editStr = selectedItem.MaxFormula;
            if (FunctionEditWidget.Display("##maxf" + selectedItem.Name, ref editStr, _modDataStore, _itemNames))
            {
                selectedItem.MaxFormula = editStr;
            }
            ImGui.NextColumn();
            
            ImGui.Text("MinFormula: ");
            ImGui.NextColumn();
            editStr = selectedItem.MinFormula;
            if (FunctionEditWidget.Display("##minf" + selectedItem.Name, ref editStr, _modDataStore, _itemNames))
            {
                selectedItem.MinFormula = editStr;
            }
            ImGui.NextColumn();
            
            ImGui.Text("StepFormula: ");
            ImGui.NextColumn();
            editStr = selectedItem.StepFormula;
            if (FunctionEditWidget.Display("##stpf" + selectedItem.Name, ref editStr, _modDataStore, _itemNames))
            {
                selectedItem.StepFormula = editStr;
            }
            ImGui.NextColumn();
        }

        ImGui.Text("PropertyFormula: ");
        ImGui.NextColumn();
        editStr = selectedItem.PropertyFormula;
        if (FunctionEditWidget.Display("##atbf"  + selectedItem.Name, ref editStr, _modDataStore, _itemNames))
        {
            selectedItem.PropertyFormula = editStr;
        }
        ImGui.NextColumn();

        if (selectedItem.GuiHint.HasFlag(GuiHint.None))
        {
            ImGui.Text("AttributeType: ");
            ImGui.NextColumn();
            editIndex = Array.IndexOf(_attributeFullNames, selectedItem.AttributeType);
            if (SelectFromListWiget.Display("##atbtype" + selectedItem.Name, _attributeTypeNames, ref editIndex))
            {
                selectedItem.AttributeType = _attributeFullNames[editIndex];
            }

            ImGui.NextColumn();
        }
    }
    public bool Equals(ComponentTemplateBlueprint componentBlueprint)
    {
        if (ParentID == componentBlueprint.UniqueID)
            return true;
        return false;
    }
}