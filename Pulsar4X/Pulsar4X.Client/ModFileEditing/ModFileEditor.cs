using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Blueprints;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Modding;

namespace Pulsar4X.SDL2UI.ModFileEditing;

public class ModFileEditor : PulsarGuiWindow
{
    
    private TechBlueprintUI _techBlueprintUI;
    private TechCatBlueprintUI _techCatBlueprintUI;
    private ComponentBluprintUI _componentBluprintUI;
    //private CargoTypeBlueprintUI _cargoTypeBlueprintUI;
    private AttributeBlueprintUI _attributeBlueprintUI;


    
    
    private ModFileEditor()
    {

    }
    internal static ModFileEditor GetInstance()
    {
        ModFileEditor instance;
        if (!_uiState.LoadedWindows.ContainsKey(typeof(ModFileEditor)))
        {
            instance = new ModFileEditor();
            instance.refresh();
        }
        else
        {
            instance = (ModFileEditor)_uiState.LoadedWindows[typeof(ModFileEditor)];
        }
        return instance;
    }

    void refresh()
    {
        ModLoader modLoader = new ModLoader();
        ModDataStore modDataStore = new ModDataStore();
        modLoader.LoadModManifest("Data/basemod/modInfo.json", modDataStore);
        
        _techCatBlueprintUI = new TechCatBlueprintUI(modDataStore);
        _techBlueprintUI = new TechBlueprintUI(modDataStore);
        _componentBluprintUI = new ComponentBluprintUI(modDataStore);
        //_cargoTypeBlueprintUI = new CargoTypeBlueprint(modDataStore);
        //_attributeBlueprintUI = new AttributeBlueprintUI(modDataStore)

    }

    
    internal override void Display()
    {
        
        if (IsActive)
        {
            if (ImGui.Begin("Debug GUI Window", ref IsActive))
            {
                _techCatBlueprintUI.Display("Tech Categorys");
                ImGui.NewLine();
                _techBlueprintUI.Display("Techs");
                ImGui.NewLine();
                _componentBluprintUI.Display("Components");
                ImGui.NewLine();
                //_cargoTypeBlueprintUI.Display("Cargo Types");
                
            }

            ImGui.End();
        }
    }
}