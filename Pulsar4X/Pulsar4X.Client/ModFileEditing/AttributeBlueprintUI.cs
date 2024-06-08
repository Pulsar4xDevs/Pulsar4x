using System;
using System.Linq;
using ImGuiNET;
using Pulsar4X.Blueprints;
using Pulsar4X.Interfaces;
using Pulsar4X.Modding;

namespace Pulsar4X.SDL2UI.ModFileEditing;

public class AttributeBlueprintUI : BluePrintsUI
{
    private protected string _parentID;
    private ComponentTemplateAttributeBlueprint[] _blueprints;
    private string[] _attributeTypeNames;
    private string[] _attributeFullNames;
    public AttributeBlueprintUI(ModDataStore modDataStore, ComponentTemplateBlueprint componentBlueprint) : base(modDataStore, ModInstruction.DataType.ComponentTemplate )
    {
        _parentID = componentBlueprint.UniqueID;
        if(componentBlueprint.Attributes != null)
            _blueprints = componentBlueprint.Attributes.ToArray();
        else
            _blueprints = new ComponentTemplateAttributeBlueprint[1];
        
        Refresh();
        

    }
    


    public sealed override void Refresh()
    {
        _itemNames = new string[_blueprints.Length];
        _isActive = new bool[_blueprints.Length];
        int i = 0;
        foreach (ComponentTemplateAttributeBlueprint item in _blueprints)
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
        ImGui.SetColumnWidth(1,400);

        int i = 0;
        foreach (var item in _blueprints)
        {
            ImGui.Text("Attrubutes: ");
            ImGui.NextColumn();
            DisplayEditorWindow(i);
            i++;
        }
        ImGui.Columns(0);
    }

    public override void DisplayEditorWindow(int selectedIndex)
    {

        var selectedItem = _blueprints[selectedIndex];
        if (selectedItem is null)
        {
            selectedItem = new ComponentTemplateAttributeBlueprint();
            selectedItem.Name = "newAttribute";
        }
        
        string name = selectedItem.Name;
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
            
        ImGui.Text("Units: ");
        ImGui.NextColumn();
        var editIndex = Array.IndexOf(_units, selectedItem.Units);
        if (SelectFromListWiget.Display("##indt" + selectedItem.Units, _units, ref editIndex))
        {
            selectedItem.Units = _units[editIndex];
        }
        ImGui.NextColumn();

        ImGui.Text("MaxFormula: ");
        ImGui.NextColumn();
        editStr = selectedItem.MaxFormula;
        if (TextEditWidget.Display("##maxf" + selectedItem.MaxFormula, ref editStr))
        {
            selectedItem.MaxFormula = editStr;
        }
        ImGui.NextColumn();

        ImGui.Text("AttributeFormula: ");
        ImGui.NextColumn();
        editStr = selectedItem.AttributeFormula;
        if (TextEditWidget.Display("##atbf", ref editStr))
        {
            selectedItem.AttributeFormula = editStr;
        }
        ImGui.NextColumn();

        ImGui.Text("DescriptionFormula: ");
        ImGui.NextColumn();
        editStr = selectedItem.DescriptionFormula;
        if (TextEditWidget.Display("##descf", ref editStr))
        {
            selectedItem.DescriptionFormula = editStr;
        }
        ImGui.NextColumn();

           
        ImGui.Text("AttributeType: ");
        ImGui.NextColumn();
        editIndex = Array.IndexOf(_attributeFullNames, selectedItem.AttributeType);
        if (SelectFromListWiget.Display("##indt" + selectedItem.AttributeType, _attributeTypeNames, ref editIndex))
        {
            selectedItem.AttributeType = _attributeFullNames[editIndex];
        }
        ImGui.NextColumn();
    }
    public bool Equals(ComponentTemplateBlueprint componentBlueprint)
    {
        if (_parentID == componentBlueprint.UniqueID)
            return true;
        return false;
    }
}