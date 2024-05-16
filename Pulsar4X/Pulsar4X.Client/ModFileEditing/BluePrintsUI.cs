using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Numerics;
using System.Runtime.InteropServices.JavaScript;
using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.Blueprints;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;
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
    private protected string[] _units;
    private protected Blueprint _newEmpty;
    protected BluePrintsUI(ModDataStore modDataStore)
    {
        _modDataStore = modDataStore;
        _cargoTypes = modDataStore.CargoTypes.Keys.ToArray();
        _techCatTypes = modDataStore.TechCategories.Keys.ToArray();
        _techTypes = modDataStore.Techs.Keys.ToArray();
        _industryTypes = modDataStore.IndustryTypes.Keys.ToArray();

        _units = new string[9];
        _units[0] = "";
        _units[1] = "KJ";
        _units[2] = "KW";
        _units[3] = "m^2";
        _units[4] = "nm";
        _units[5] = "kg";
        _units[6] = "m";
        _units[7] = "N";
        _units[8] = "m/s";

    }
    

    public void Display(string label)
    {
        int i = 0;
        if(ImGui.TreeNode(label))
        {
            ImGui.BeginChild(label);
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0,150);
            ImGui.SetColumnWidth(1,500);
            foreach (var item in _itemBlueprints)
            {
                ImGui.Text(_itemNames[i]);
                ImGui.NextColumn();
                if(ImGui.Checkbox("Edit##"+_itemNames[i], ref _isActive[i]));
                {
                }
                DisplayEditorWindow(i);
                ImGui.NextColumn();
                i++;
            }
            NewItem("+##"+label, _newEmpty);
            ImGui.EndChild();
        }
    }

    public abstract void DisplayEditorWindow(int index);

    public void NewItem(string label, Blueprint newBlueprint)
    {
        if (ImGui.Button(label))
        {
            Array.Resize(ref _itemBlueprints, _itemBlueprints.Length + 1);
            _itemBlueprints[^1] = newBlueprint;
            Array.Resize(ref _itemNames, _itemNames.Length + 1);
            _itemNames[^1] = "newBluprint";
            Array.Resize(ref _isActive, _isActive.Length + 1);
            _isActive[^1] = true;
        }
    }
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
            _itemNames[i] = kvp.Value.Name;
            _itemBlueprints[i] = kvp.Value;
            _isActive[i] = false;
            i++;
        }

        var newEmpty = new TechCategoryBlueprint();
        newEmpty.Name = "New Blueprint";
        _newEmpty = newEmpty;
    }
    
    public override void DisplayEditorWindow(int selectedIndex)
    {
        if(!_isActive[selectedIndex])
            return;
        var selectedItem = (TechCategoryBlueprint)_itemBlueprints[selectedIndex];
        string name = selectedItem.Name;
        string desc = selectedItem.Description;
        if (ImGui.Begin("Tech Category Editor: " + name))
        {
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0,150);
            ImGui.SetColumnWidth(1,500);
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
            _itemNames[i] = kvp.Value.Name;
            _itemBlueprints[i] = kvp.Value;
            _isActive[i] = false;
            i++;
        }
        var newEmpty = new TechBlueprint();
        newEmpty.Name = "New Blueprint";
        _newEmpty = newEmpty;
    }
/*
    public override void Display()
    {
        ImGui.Columns(2);
        ImGui.SetColumnWidth(0,150);
        ImGui.SetColumnWidth(1,500);

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
        TechBlueprint newBpt = new TechBlueprint();
        newBpt.Name = "newBluprint";
        NewItem("+##newtbt", newBpt);
    }
    */
    public override void DisplayEditorWindow(int selectedIndex)
    {
        
        if(!_isActive[selectedIndex])
            return;
        var selectedItem = (TechBlueprint)_itemBlueprints[selectedIndex];
        string name = selectedItem.Name;
        string editStr; 
        if (ImGui.Begin("Tech Editor: " + name))
        {
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0,150);
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
    private AttributeBlueprintUI? _attributeBlueprintUI;
    private List<ComponentTemplateAttributeBlueprint> _selectedAttributes;
    public ComponentBluprintUI(ModDataStore modDataStore) : base(modDataStore)
    {
        Dictionary<string, ComponentTemplateBlueprint> blueprints = modDataStore.ComponentTemplates;
        _itemNames = new string[blueprints.Count];
        _itemBlueprints = new Blueprint[blueprints.Count];
        _isActive = new bool[blueprints.Count];
        int i = 0;
        foreach (var kvp in blueprints)
        {
            _itemNames[i] = kvp.Value.Name;
            _itemBlueprints[i] = kvp.Value;
            _isActive[i] = false;
            i++;
        }
        var newEmpty = new ComponentTemplateBlueprint();
        newEmpty.Name = "New Blueprint";
        _newEmpty = newEmpty;
    }
 /*   
    public override void Display()
    {
        ImGui.Columns(2);
        ImGui.SetColumnWidth(0,150);
        ImGui.SetColumnWidth(1,500);

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
        ComponentTemplateBlueprint newBpt = new ComponentTemplateBlueprint();
        newBpt.Name = "newBluprint";
        NewItem("+##newcpbt", newBpt);
    }
*/
    private int editIndex = 0;
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
        if (ImGui.Begin("Tech Category Editor: " + name))
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
            
            _attributeBlueprintUI.Display();

            ImGui.End();
        }
    }
}

public class AttributeBlueprintUI : BluePrintsUI
{
    private protected string _parentID;
    private ComponentTemplateAttributeBlueprint[] _blueprints;
    private string[] _attributeTypeNames;
    private string[] _attributeFullNames;
    public AttributeBlueprintUI(ModDataStore modDataStore, ComponentTemplateBlueprint componentBlueprint) : base(modDataStore)
    {
        _parentID = componentBlueprint.UniqueID;
        _blueprints = componentBlueprint.Attributes.ToArray();
        _itemNames = new string[_blueprints.Length];
        _isActive = new bool[_blueprints.Length];
        int i = 0;
        foreach (var item in _blueprints)
        {
            _itemNames[i] = item.Name;
            _isActive[i] = false;
            i++;
        }
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

            var foo = selectedItem.Units;
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