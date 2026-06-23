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
    string[] _componentBlueprintIDs;
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
        
        _componentBlueprintIDs = new string[_componentBlueprints.Length];
        for (int index = 0; index < _componentBlueprints.Length; index++)
        {
            _componentBlueprintIDs[index] = _componentBlueprints[index].UniqueID;
        }

        var newEmpty = new ShipDesignBlueprint();
        newEmpty.Name = "New Blueprint";
        newEmpty.Components = new List<ShipDesignBlueprint.ShipComponentBlueprint>();
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
        if (ImGui.Begin("Ship Design Editor: " + name, ref _isActive[selectedIndex]))
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
            ImGui.Columns(1);

            if (ImGui.BeginTable("Armor", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable))
            {
                ImGui.TableSetupColumn("Thckness", ImGuiTableColumnFlags.WidthFixed, 40f);
                ImGui.TableSetupColumn("Type");

                ImGui.TableHeadersRow(); // Optional header row

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                _editInt = Array.IndexOf(_armorBlueprints, selectedItem.Armor.Id);
                int thickness = (int)selectedItem.Armor.Thickness;
                if (IntEditWidget.Display("##thinkness", ref thickness, int.MaxValue, (int)uint.MinValue))
                {
                    selectedItem.Armor = new ShipDesignBlueprint.ShipArmorBlueprint() { Id = _armorBlueprints[_editInt], Thickness = (uint)thickness, };
                }
                ImGui.TableNextColumn();
                
                if (SelectFromListWiget.Display("##armor", _armorBlueprints, ref _editInt))
                {
                    selectedItem.Armor = new ShipDesignBlueprint.ShipArmorBlueprint() { Id = _armorBlueprints[_editInt], Thickness = (uint)thickness, };
                }


            }
            ImGui.EndTable();

            if (ImGui.BeginTable("Components", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable))
            {
                ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 40f);
                ImGui.TableSetupColumn("Count", ImGuiTableColumnFlags.WidthFixed, 120f);
                ImGui.TableSetupColumn("Design");
                
                ImGui.TableHeadersRow(); // Optional header row
                
                ImGui.TableNextRow();
                
                int index = 0;
                for (index = 0; index < selectedItem.Components.Count; index++)
                {
                    ShipDesignBlueprint.ShipComponentBlueprint component = selectedItem.Components[index];
                    string id = component.Id;
                    int amount = (int)component.Amount;
                    
                    ImGui.TableNextColumn();
                    if (ImGui.Button("x##" + index))
                    {
                        selectedItem.Components.RemoveAt(index);
                    }
                    ImGui.TableNextColumn();

                    
                    _editInt = (int)component.Amount;
                    if (IntEditWidget.Display("##compCount" + index, ref _editInt))
                    {
                        amount = _editInt;
                        selectedItem.Components[index] = new ShipDesignBlueprint.ShipComponentBlueprint() { Id = id, Amount = (uint)amount };
                    }
                    ImGui.TableNextColumn();
                    
                    _editInt = Array.IndexOf(_componentBlueprintIDs, id);
                    if (SelectFromListWiget.Display("##comp" + index, _componentBlueprintIDs, ref _editInt))
                    {
                        id = _componentBlueprintIDs[_editInt];
                        selectedItem.Components[index] = new ShipDesignBlueprint.ShipComponentBlueprint() { Id = id, Amount = (uint)amount };
                    }
                    ImGui.TableNextRow();
                }
                ImGui.TableNextColumn();
                ImGui.TableNextColumn();
                ImGui.TableNextColumn();
                _editInt = Array.IndexOf(_componentBlueprintIDs, index);
                
                if (SelectFromListWiget.Display("##comp" + index, _componentBlueprintIDs, ref _editInt))
                {
                    string id = _componentBlueprintIDs[_editInt];
                    selectedItem.Components.Add( new ShipDesignBlueprint.ShipComponentBlueprint() { Id = id, Amount = (uint)1 });
                }
            }
            ImGui.EndTable();
        }
        ImGui.End();
    }
}