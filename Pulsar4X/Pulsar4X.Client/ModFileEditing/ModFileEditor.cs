using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Blueprints;
using Pulsar4X.DataStructures;

namespace Pulsar4X.SDL2UI.ModFileEditing;

public class ModFileEditor : PulsarGuiWindow
{
    
    private TechBlueprintUI _techBlueprintUI;
    private TechCatBlueprintUI _techCatBlueprintUI;
    private ComponentBluprintUI _componentBluprintUI;
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

        _techCatBlueprintUI = new TechCatBlueprintUI(_uiState.Game.TechCategories);
        _techBlueprintUI = new TechBlueprintUI(_uiState.Game.StartingGameData.Techs);
        _componentBluprintUI = new ComponentBluprintUI(_uiState.Game.StartingGameData.ComponentTemplates);
    }

    
    internal override void Display()
    {
        
        if (IsActive)
        {
            if (ImGui.Begin("Debug GUI Window", ref IsActive))
            {
                _techCatBlueprintUI.Display();
                ImGui.NewLine();
                _techBlueprintUI.Display();
                ImGui.NewLine();
                _componentBluprintUI.Display();
                
            }

            ImGui.End();
        }
    }
}