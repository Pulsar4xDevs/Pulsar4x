using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Blueprints;
using Pulsar4X.DataStructures;
using Pulsar4X.Modding;

namespace Pulsar4X.Client.ModFileEditing;

public class ComponentBluprintUI : BluePrintsUI
{
    private ComponentPropertyBlueprintUI? _propertyBlueprintUI;
    private List<ComponentTemplatePropertyBlueprint> _selectedProperties;
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
        newEmpty.Properties = new List<ComponentTemplatePropertyBlueprint>();
        _newEmpty = newEmpty;
    }


    public override void DisplayEditorWindow(int selectedIndex)
    {

        if (!_isActive[selectedIndex])
            return;
        var selectedItem = (ComponentTemplateBlueprint)_itemBlueprints[selectedIndex];
        _selectedProperties = selectedItem.Properties;

        if(_propertyBlueprintUI == null || _propertyBlueprintUI.ParentID != selectedItem.UniqueID)
            _propertyBlueprintUI = new ComponentPropertyBlueprintUI(_modDataStore, selectedItem);

        string name = selectedItem.Name;
        string editStr;
        ImGui.SetNextWindowSize(new Vector2(1500,  900));
        if (ImGui.Begin("Component Editor: " + name, ref _isActive[selectedIndex]))
        {
            if (ImGui.BeginTable("MaterialsTable", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable))
            {
                ImGui.TableSetupColumn("Field", ImGuiTableColumnFlags.WidthFixed, 150f);
                ImGui.TableSetupColumn("Value");
                ImGui.TableHeadersRow(); // Optional header row
                
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Name: ");
                ImGui.TableNextColumn();
                editStr = selectedItem.Name;
                if (TextEditWidget.Display("##name" + selectedItem.UniqueID, ref editStr))
                {
                    selectedItem.Name = editStr;
                }
                
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("ComponentType: ");
                ImGui.TableNextColumn();
                editStr = selectedItem.ComponentType;
                if (TextEditWidget.Display("##cmpt" + selectedItem.UniqueID, ref editStr))
                {
                    selectedItem.ComponentType = editStr;
                }

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("CargoType: ");
                ImGui.TableNextColumn();
                _editInt = Array.IndexOf(_cargoTypes, selectedItem.CargoTypeID);
                if (SelectFromListWiget.Display("##cgot" + selectedItem.UniqueID, _cargoTypes, ref _editInt))
                {
                    selectedItem.CargoTypeID = _cargoTypes[_editInt];
                }

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Fomula: ");
                ImGui.TableNextColumn();
                var editDicf = selectedItem.Formulas;
                if (DictEditWidget.Display("##fmula", ref editDicf, _modDataStore, selectedItem))
                {
                    selectedItem.Formulas = editDicf;
                }

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("ResourceCosts: ");
                ImGui.TableNextColumn();
                var editDicRC = selectedItem.ResourceCost;
                if (DictEditWidget.Display("##resc", ref editDicRC, _modDataStore, selectedItem))
                {
                    selectedItem.ResourceCost = editDicRC;
                }

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("IndustryType: ");
                ImGui.TableNextColumn();
                _editInt = Array.IndexOf(_industryTypes, selectedItem.IndustryTypeID);
                if (SelectFromListWiget.Display("##indt" + selectedItem.IndustryTypeID, _industryTypes, ref _editInt))
                {
                    selectedItem.IndustryTypeID = _industryTypes[_editInt];
                }
                
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("MountType: ");
                ImGui.TableNextColumn();
                
                _editInt = Array.IndexOf(_mountTypes, selectedItem.MountType.ToString());
                ComponentMountType _mtype = selectedItem.MountType;
                if (SelectFromListWiget.Display("##mntt" + selectedItem.UniqueID, ref _mtype))
                {
                    selectedItem.MountType = _mtype;
                }

                ImGui.EndTable();
                _propertyBlueprintUI.Display();

            }

            ImGui.End();
        }
    }
}