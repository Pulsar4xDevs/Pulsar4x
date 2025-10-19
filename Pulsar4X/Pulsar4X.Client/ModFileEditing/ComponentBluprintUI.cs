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
    private AttributeBlueprintUI? _attributeBlueprintUI;
    private List<ComponentTemplatePropertyBlueprint> _selectedAttributes;
    public ComponentBluprintUI(ModDataStore modDataStore) : base(modDataStore, ModInstruction.DataType.ComponentTemplate)
    {
        _itemBlueprints = _componentBlueprints;
        Refresh();
    }
    public sealed override void Refresh()
    {
        _itemNames = new string[_itemBlueprints.Length];
        _isActive = new bool[_itemBlueprints.Length];
        int i = 0;
        foreach (ComponentDesignBlueprint item in _itemBlueprints)
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
        _selectedAttributes = selectedItem.Properties;

        if(_attributeBlueprintUI == null)
            _attributeBlueprintUI = new AttributeBlueprintUI(_modDataStore, selectedItem);

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
                if (TextEditWidget.Display("##name" + selectedItem.Name, ref editStr))
                {
                    selectedItem.Name = editStr;
                }
                
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("ComponentType: ");
                ImGui.TableNextColumn();
                editStr = selectedItem.ComponentType;
                if (TextEditWidget.Display("##cmpt" + selectedItem.ComponentType, ref editStr))
                {
                    selectedItem.Name = editStr;
                }

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("CargoType: ");
                ImGui.TableNextColumn();
                _editInt = Array.IndexOf(_cargoTypes, selectedItem.CargoTypeID);
                if (SelectFromListWiget.Display("##cgot" + selectedItem.CargoTypeID, _cargoTypes, ref _editInt))
                {
                    selectedItem.Name = _cargoTypes[_editInt];
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
                _editInt = Array.IndexOf(_mountTypes, selectedItem.MountType);
                if (SelectFromListWiget.Display("##mntt" + selectedItem.UniqueID, _mountTypes, ref _editInt))
                {

                    if (Enum.TryParse(typeof(ComponentMountType), _mountTypes[_editInt], out var mtype))
                        selectedItem.MountType = (ComponentMountType)mtype;
                }

                ImGui.EndTable();
                _attributeBlueprintUI.Display();

            }

            ImGui.End();
        }
    }
}