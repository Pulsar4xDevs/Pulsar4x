using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.JavaScript;
using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.Blueprints;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Modding;

namespace Pulsar4X.SDL2UI.ModFileEditing;

public abstract class BluePrintsUI
{
    private protected int _selecteditem = 0;
    private protected string[] _itemNames;
    private protected Blueprint[] _itemBlueprints;
    private protected bool[] _isActive;
    private protected ModDataStore _modDataStore;
    private protected string[] _cargoTypes;
    private protected string[] _techCatTypes;
    private protected string[] _techTypes;
    private protected string[] _industryTypes;
    protected BluePrintsUI(ModDataStore modDataStore)
    {
        _modDataStore = modDataStore;
        _cargoTypes = modDataStore.CargoTypes.Keys.ToArray();
        _techCatTypes = modDataStore.TechCategories.Keys.ToArray();
        _techTypes = modDataStore.Techs.Keys.ToArray();
        _industryTypes = modDataStore.IndustryTypes.Keys.ToArray();
    }
    
    public abstract void Display();
}

public class TechCatBlueprintUI : BluePrintsUI
{
    
    public TechCatBlueprintUI(ModDataStore modDataStore) : base(modDataStore)
    {
        Dictionary<string, TechCategoryBlueprint> blueprints = _modDataStore.TechCategories;
        
        _itemNames = new string[blueprints.Count];
        _itemBlueprints = new Blueprint[blueprints.Count];
        _isActive = new bool[blueprints.Count];
        int i = 0;
        foreach (var kvp in blueprints)
        {
            _itemNames[i] = kvp.Key;
            _itemBlueprints[i] = kvp.Value;
            _isActive[i] = false;
            i++;
        }
    }
    
    public override void Display()
    {
        //BorderListOptions.Begin("Tech Blueprints", _itemNames, ref _selecteditem, 200);
        //ImGui.BeginChild("");
        ImGui.Columns(2);
        ImGui.SetColumnWidth(0,200);
        ImGui.SetColumnWidth(1,100);

        int i = 0;
        foreach (TechCategoryBlueprint item in _itemBlueprints)
        {
            ImGui.Text(item.Name);
            ImGui.NextColumn();
            if(ImGui.Checkbox("Edit##"+_itemNames[i], ref _isActive[i]));
            {
            }
            DisplayEditorWindow(i);
            ImGui.NextColumn();
            i++;
        }
        
    }
    private void DisplayEditorWindow(int selectedIndex)
    {
        if(!_isActive[selectedIndex])
            return;
        var selectedItem = (TechCategoryBlueprint)_itemBlueprints[selectedIndex];
        string name = selectedItem.Name;
        string desc = selectedItem.Description;
        if (ImGui.Begin("Tech Category Editor: " + name))
        {
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0,100);
            ImGui.SetColumnWidth(1,300);
            ImGui.Text("Name: ");
            ImGui.NextColumn();
            if (TextEditWidget.Display("##name" + selectedItem.Name, ref name))
            {
                selectedItem.Name = name;
            }
            
            ImGui.NextColumn();
            ImGui.Text("Description: ");
            ImGui.NextColumn();
            if (TextEditWidget.Display("##description" + selectedItem.Name, ref desc))
            {
                selectedItem.Description = desc;
            }


            ImGui.End();
        }
    }
}

public class TechBlueprintUI : BluePrintsUI
{
    private int _selectedIndex = -1;
    public TechBlueprintUI(ModDataStore modDataStore) : base(modDataStore)
    {
        var blueprints = modDataStore.Techs;
        
        _itemNames = new string[blueprints.Count];
        _itemBlueprints = new Blueprint[blueprints.Count];
        _isActive = new bool[blueprints.Count];
        int i = 0;
        foreach (var kvp in blueprints)
        {
            _itemNames[i] = kvp.Key;
            _itemBlueprints[i] = kvp.Value;
            _isActive[i] = false;
            i++;
        }
    }

    public override void Display()
    {
        ImGui.Columns(2);
        ImGui.SetColumnWidth(0,200);
        ImGui.SetColumnWidth(1,100);

        int i = 0;
        foreach (TechBlueprint item in _itemBlueprints)
        {
            ImGui.Text(item.Name);
            ImGui.NextColumn();
            if(ImGui.Checkbox("Edit##"+_itemNames[i], ref _isActive[i]));
            {
            }
            DisplayEditorWindow(i);
            ImGui.NextColumn();
            i++;
        }
    }
    
    public void DisplayEditorWindow(int selectedIndex)
    {
        
        if(!_isActive[selectedIndex])
            return;
        var selectedItem = (TechBlueprint)_itemBlueprints[selectedIndex];
        string name = selectedItem.Name;
        string editStr; 
        if (ImGui.Begin("Tech Editor: " + name))
        {
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0,100);
            ImGui.SetColumnWidth(1,500);
            ImGui.Text("Name: ");
            ImGui.NextColumn();

            editStr = selectedItem.Name;
            if (TextEditWidget.Display("##name" + selectedItem.Name, ref editStr))
            {
                selectedItem.Name = editStr;
            }
            ImGui.NextColumn();
            
            
            ImGui.Text("Description: ");
            ImGui.NextColumn();
            editStr = selectedItem.Description;
            if (TextEditWidget.Display("##desc" + selectedItem.Description, ref editStr))
            {
                selectedItem.Name = editStr;
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
            if (TextEditWidget.Display("##cf" + selectedItem.CostFormula, ref editStr))
            {
                selectedItem.CostFormula = editStr;
            }
            ImGui.NextColumn();
            
            
            ImGui.Text("DataFormula: ");
            ImGui.NextColumn();
            editStr = selectedItem.DataFormula;
            if (TextEditWidget.Display("##df" + selectedItem.DataFormula, ref editStr))
            {
                selectedItem.DataFormula = editStr;
            }
            ImGui.NextColumn();
            
            
            ImGui.Text("MaxLevel: ");
            ImGui.NextColumn();
            int editInt = selectedItem.MaxLevel;
            if (IntEditWidget.Display("##ml" + selectedItem.MaxLevel.ToString(), ref editInt))
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


public class ComponentBluprintUI : BluePrintsUI
{
    
    public ComponentBluprintUI(ModDataStore modDataStore) : base(modDataStore)
    {
        Dictionary<string, ComponentTemplateBlueprint> blueprints = modDataStore.ComponentTemplates;
        _itemNames = new string[blueprints.Count];
        _itemBlueprints = new Blueprint[blueprints.Count];
        _isActive = new bool[blueprints.Count];
        int i = 0;
        foreach (var kvp in blueprints)
        {
            _itemNames[i] = kvp.Key;
            _itemBlueprints[i] = kvp.Value;
            _isActive[i] = false;
            i++;
        }
    }
    
    public override void Display()
    {
        ImGui.Columns(2);
        ImGui.SetColumnWidth(0,200);
        ImGui.SetColumnWidth(1,100);

        int i = 0;
        foreach (ComponentTemplateBlueprint item in _itemBlueprints)
        {
            ImGui.Text(item.Name);
            ImGui.NextColumn();
            if(ImGui.Checkbox("Edit##"+_itemNames[i], ref _isActive[i]));
            {
            }
            DisplayEditorWindow(i);
            ImGui.NextColumn();
            i++;
        }
    }

    private int editIndex = 0;
    public void DisplayEditorWindow(int selectedIndex)
    {

        if (!_isActive[selectedIndex])
            return;
        var selectedItem = (ComponentTemplateBlueprint)_itemBlueprints[selectedIndex];
        string name = selectedItem.Name;
        string editStr;
        if (ImGui.Begin("Tech Category Editor: " + name))
        {
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0, 100);
            ImGui.SetColumnWidth(1, 300);
            
            
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
            editIndex = Array.IndexOf(_cargoTypes, selectedItem.CargoTypeID);
            if (SelectFromListWiget.Display("##cgot" + selectedItem.CargoTypeID, _cargoTypes, ref editIndex))
            {
                selectedItem.Name = _cargoTypes[editIndex];
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
            editIndex = Array.IndexOf(_industryTypes, selectedItem.IndustryTypeID);
            if (SelectFromListWiget.Display("##indt" + selectedItem.IndustryTypeID, _industryTypes, ref editIndex))
            {
                selectedItem.IndustryTypeID = _industryTypes[editIndex];
            }
            ImGui.NextColumn();
            
            
            ImGui.Text("MountType: ");
            ImGui.NextColumn();
            string[] mountTypes = Enum.GetNames(typeof(ComponentMountType));
            editIndex = Array.IndexOf(_industryTypes, selectedItem.MountType);
            if (SelectFromListWiget.Display("##mntt" + selectedItem.IndustryTypeID, mountTypes, ref editIndex))
            {

                if(Enum.TryParse(typeof(ComponentMountType), mountTypes[editIndex], out var mtype))
                    selectedItem.MountType = (ComponentMountType)mtype;
            }
            ImGui.NextColumn();
            
            /*

            ImGui.Text("Attributes: ");
            ImGui.SameLine();
            ImGui.Text(selected.Attributes.ToString());

            */
            ImGui.End();
        }
    }
} 