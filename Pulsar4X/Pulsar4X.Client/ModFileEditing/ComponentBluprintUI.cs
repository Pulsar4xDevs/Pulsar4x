using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
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
    private string[] _propertyNames;
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
        newEmpty.UniqueID = newEmpty.Name;
        newEmpty.Properties = new List<ComponentTemplatePropertyBlueprint>();
        var formula = new Dictionary<string, string>();
        formula.Add("Description", "componentDescription");
        formula.Add("Mass", "1");
        formula.Add("Volume", "[Mass]");
        formula.Add("HTK", "[Mass]");
        formula.Add("CrewReq", "[Mass] * 0.5");
        formula.Add("ResearchCost", "[Mass]");
        formula.Add("CreditCost","[Mass]");
        formula.Add("BuildPointCost","[Mass]");

        newEmpty.Formulas = formula;
        newEmpty.ResourceCost = new Dictionary<string, string>();
        _newEmpty = newEmpty;
    }


    public override void DisplayEditorWindow(int selectedIndex)
    {

        if (!_isActive[selectedIndex])
            return;
        var selectedItem = (ComponentTemplateBlueprint)_itemBlueprints[selectedIndex];
        _selectedProperties = selectedItem.Properties;
        _propertyNames = new string[_selectedProperties.Count];
        for (int i = 0; i < _selectedProperties.Count; i++)
        {
            _propertyNames[i] = _selectedProperties[i].Name;
        }

        if(_propertyBlueprintUI == null || _propertyBlueprintUI.ParentID != selectedItem.UniqueID)
            _propertyBlueprintUI = new ComponentPropertyBlueprintUI(_modDataStore, selectedItem);

        string name = selectedItem.Name;
        string editStr;
        ImGui.SetNextWindowSize(new Vector2(1500,  900));
        if (ImGui.Begin("Component Editor: " + name, ref _isActive[selectedIndex]))
        {
            if (ImGui.BeginTable("table", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable))
            {
                ImGui.TableSetupColumn("Field", ImGuiTableColumnFlags.WidthFixed, 150f);
                ImGui.TableSetupColumn("Value");
                ImGui.TableHeadersRow(); // Optional header row
                
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("ID: ");
                ImGui.TableNextColumn();
                editStr = selectedItem.UniqueID;
                if (TextEditWidget.Display("##id" + selectedItem.UniqueID, ref editStr))
                {
                    selectedItem.UniqueID = editStr;
                    Refresh();
                }
                
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
                ImGui.Text("Formula: ");
                ImGui.TableNextColumn();
                var editDicf = selectedItem.Formulas;
                if (DictEditWidget.Display("##fmula", ref editDicf, _modDataStore, selectedItem))
                {
                    selectedItem.Formulas = editDicf;
                }

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Resource Costs");
                ImGui.TableNextColumn();
                ResourceList(selectedItem);
                

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

    public void ResourceList(ComponentTemplateBlueprint selectedItem)
    {
        
        
        ImGui.BeginChild("resources", _childSize);
        ImGui.BeginTable("resouceTable", 2, ImGuiTableFlags.Resizable);
        ImGui.TableSetupColumn("ResourceID", ImGuiTableColumnFlags.WidthFixed, 150f);
        ImGui.TableSetupColumn("Amount Formula");
        ImGui.TableHeadersRow(); // Optional header row
                
       
        
        var editDicRC = selectedItem.ResourceCost.ToDictionary();
        bool hasChanged = false;
        foreach (var resKVP in selectedItem.ResourceCost)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            //ImGui.Text(resKVP.Key);
            int resIndex = Array.IndexOf(_resources, resKVP.Key);
            if (SelectFromListWiget.Display("##reskey" + resKVP.Key, _resources, ref resIndex))
            {
                string newRes = _resources[resIndex];
                if (!editDicRC.ContainsKey(newRes))
                {
                    editDicRC.Add(newRes, "1");
                    hasChanged = true;
                }//else do nothing, we already have that resource in the dictionary.
            }
            
            ImGui.TableNextColumn();
            _editStr = resKVP.Value;
            if (FunctionEditWidget.Display("##rescost" + resKVP.Key, ref _editStr, _modDataStore, _propertyNames))
            {
                editDicRC[resKVP.Key] = _editStr;
                hasChanged = true;
            }
            //ImGui.TableNextColumn();
            //ImGui.TableNextRow();
        }
        ImGui.TableNextColumn();
        
        int newresIndex = -1;
        if (SelectFromListWiget.Display("reskeynew" , _resources, ref  newresIndex, "Add New Resource"))
        {
            string newRes = _resources[newresIndex];
            if (!editDicRC.ContainsKey(newRes))
            {
                editDicRC.Add(newRes, "1");
                hasChanged = true;
            }
        }
        
        ImGui.EndTable();
        if(hasChanged)
            selectedItem.ResourceCost = editDicRC;
        
        
        ImGui.EndChild();
        /*
        var editDicRC = selectedItem.ResourceCost;
        ImGui.Text("ResourceCosts: ");
        if (ImGui.Button("Add Resource"))
        {
            editDicRC.Add("selectResource", "amountFormula");
        }
        ImGui.TableNextColumn();
        if (DictEditWidget.Display("##resc", ref editDicRC, _modDataStore, selectedItem))
        {
            selectedItem.ResourceCost = editDicRC;
        }*/
        
    }
}