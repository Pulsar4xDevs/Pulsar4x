using System;
using System.Linq;
using ImGuiNET;
using Pulsar4X.Blueprints;
using Pulsar4X.DataStructures;
using Pulsar4X.Modding;

namespace Pulsar4X.SDL2UI.ModFileEditing;

public class ProcessedMateralsUI : BluePrintsUI
{
    private int _selectedIndex = -1;
    public ProcessedMateralsUI(ModDataStore modDataStore) : base(modDataStore, ModInstruction.DataType.ProcessedMaterial)
    {
        var blueprints = modDataStore.ProcessedMaterials;
        _itemBlueprints = blueprints.Values.ToArray();
        Refresh();
    }

    public sealed override void Refresh()
    {
        _itemNames = new string[_itemBlueprints.Length];
        _isActive = new bool[_itemBlueprints.Length];
        int i = 0;
        foreach (ProcessedMaterialBlueprint item in _itemBlueprints)
        {
            _itemNames[i] = item.Name;
            _isActive[i] = false;
            i++;
        }
        var newEmpty = new ProcessedMaterialBlueprint();
        newEmpty.Name = "New Blueprint";
        _newEmpty = newEmpty;
    }
    

    public override void DisplayEditorWindow(int selectedIndex)
    {
        
        if(!_isActive[selectedIndex])
            return;
        var selectedItem = (ProcessedMaterialBlueprint)_itemBlueprints[selectedIndex];
        
        if (ImGui.Begin("Processed Materials Editor: " + selectedItem.Name, ref _isActive[selectedIndex]))
        {
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0,150);
            ImGui.SetColumnWidth(1,500);
            ImGui.Text("Name: ");
            ImGui.NextColumn();

            _editStr = selectedItem.Name;
            if (TextEditWidget.Display("##name" + selectedItem.UniqueID, ref _editStr))
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
            
            
            ImGui.Text("IndustryType: ");
            ImGui.NextColumn();
            _selectedIndex = Array.IndexOf(_industryTypes, selectedItem.IndustryTypeID);
            if (SelectFromListWiget.Display("##itype" + selectedItem.UniqueID, _industryTypes, ref _selectedIndex))
            {
                selectedItem.IndustryTypeID = _industryTypes[_selectedIndex];
                _selectedIndex = -1;
            }
            ImGui.NextColumn();
            
            
            ImGui.Text("CostFormula: ");
            ImGui.NextColumn();
            var edtdic = selectedItem.ResourceCosts;
            if (DictEditWidget.Display("##cf" + selectedItem.UniqueID, ref edtdic))
            {
                selectedItem.ResourceCosts = edtdic;
            }
            ImGui.NextColumn();
            
            
            ImGui.Text("Formula: ");
            ImGui.NextColumn();
            var editDict2 = selectedItem.Formulas;
            if (DictEditWidget.Display("##formula" + selectedItem.UniqueID, ref editDict2))
            {
                selectedItem.Formulas = editDict2;
            }
            ImGui.NextColumn();
            
            
            ImGui.Text("Output Amount: ");
            ImGui.NextColumn();
            int editInt = selectedItem.OutputAmount;
            if (IntEditWidget.Display("##out" + selectedItem.UniqueID, ref editInt))
            {
                selectedItem.OutputAmount = (ushort)editInt;
            }
            ImGui.NextColumn();
            
            ImGui.Text("Industry Point Cost: ");
            ImGui.NextColumn();
            _editInt = (int)selectedItem.IndustryPointCosts;
            if (IntEditWidget.Display("##ip" + selectedItem.UniqueID, ref editInt))
            {
                selectedItem.IndustryPointCosts = _editInt;
            }
            ImGui.NextColumn();
            
            ImGui.Text("Wealth Point Cost: ");
            ImGui.NextColumn();
            _editInt = selectedItem.WealthCost;
            if (IntEditWidget.Display("##wealth" + selectedItem.UniqueID, ref editInt))
            {
                selectedItem.WealthCost = (ushort)_editInt;
            }
            ImGui.NextColumn();

            ImGui.Text("Constructable Hint: ");
            ImGui.NextColumn();
            _editInt = Array.IndexOf(_constrGuiHints, selectedItem.GuiHints);
            if (SelectFromListWiget.Display("##mntt" + selectedItem.UniqueID, _constrGuiHints, ref _editInt))
            {
                if(Enum.TryParse(typeof(ConstructableGuiHints), _constrGuiHints[_editInt], out var mtype))
                    selectedItem.GuiHints = (ConstructableGuiHints)mtype;
            }
            ImGui.NextColumn();
            
            ImGui.Text("Cargo Type: ");
            ImGui.NextColumn();
            _selectedIndex = Array.IndexOf(_cargoTypes, selectedItem.CargoTypeID);
            if (SelectFromListWiget.Display("##ctype" + selectedItem.UniqueID, _cargoTypes, ref _selectedIndex))
            {
                selectedItem.CargoTypeID = _cargoTypes[_selectedIndex];
                _selectedIndex = -1;
            }
            ImGui.NextColumn();
            
            ImGui.Text("Volume: ");
            ImGui.NextColumn();
            var editDouble= selectedItem.VolumePerUnit;
            if (DoubleEditWidget.Display("##vol" + selectedItem.UniqueID, ref editDouble))
            {
                selectedItem.VolumePerUnit = editDouble;
            }
            ImGui.NextColumn();
            
            ImGui.Text("Mass: ");
            ImGui.NextColumn();
            _editInt = (int)selectedItem.MassPerUnit;
            if (IntEditWidget.Display("##mass" + selectedItem.UniqueID, ref _editInt))
            {
                selectedItem.MassPerUnit = _editInt;
            }
            ImGui.NextColumn();
            
            ImGui.End();
        }
    }
}