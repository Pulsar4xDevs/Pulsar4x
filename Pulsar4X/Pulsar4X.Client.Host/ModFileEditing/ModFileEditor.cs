using System.IO;
using ImGuiNET;
using Pulsar4X.Blueprints;
using Pulsar4X.Modding;

namespace Pulsar4X.Client.ModFileEditing;

public class ModFileEditor : PulsarGuiWindow
{
    private ModInfoUI _modInfoUI;
    private TechBlueprintUI _techBlueprintUI;
    private TechCatBlueprintUI _techCatBlueprintUI;
    private ComponentBluprintUI _componentBluprintUI;
    private CargoTypeBlueprintUI _cargoTypeBlueprintUI;
    private ComponentPropertyBlueprintUI _componentPropertyBlueprintUI;
    private ArmorBlueprintUI _armorBlueprintUI;
    private ProcessedMaterialsUI _processedMaterialsUI;
    private MineralBlueprintUI _mineralsBlueprintUI;
    private ShipDesignBlueprintUI _shipDesignBlueprintUI;

    private ModFileEditor()
    {

    }
    internal static ModFileEditor GetInstance()
    {
        ModFileEditor instance;
        if (!_uiState.LoadedWindows.ContainsKey(typeof(ModFileEditor)))
        {
            instance = new ModFileEditor();
            ModLoader modLoader = new ModLoader();
            ModDataStore modDataStore = new ModDataStore();
            string? appDataDirectory = PulsarMainWindow.GetAppDataPath();
            string modPath = Path.Combine(appDataDirectory, PulsarMainWindow.ModsPath, "basemod/modInfo.json");
            modLoader.LoadModManifest(modPath, modDataStore);
            instance.Refresh(modDataStore);
        }
        else
        {
            instance = (ModFileEditor)_uiState.LoadedWindows[typeof(ModFileEditor)];
        }
        return instance;
    }

    public void Refresh(ModDataStore modDataStore)
    {
        _modInfoUI = new ModInfoUI(modDataStore);
        _techCatBlueprintUI = new TechCatBlueprintUI(modDataStore);
        _techBlueprintUI = new TechBlueprintUI(modDataStore);
        _componentBluprintUI = new ComponentBluprintUI(modDataStore);
        _cargoTypeBlueprintUI = new CargoTypeBlueprintUI(modDataStore);

        _armorBlueprintUI = new ArmorBlueprintUI(modDataStore);
        _processedMaterialsUI = new ProcessedMaterialsUI(modDataStore);
        _mineralsBlueprintUI = new MineralBlueprintUI(modDataStore);
        _shipDesignBlueprintUI = new ShipDesignBlueprintUI(modDataStore);
    }


    internal override void Display()
    {

        if (IsActive)
        {
            if (ImGui.Begin("Editor", ref IsActive))
            {
                _modInfoUI.Display("Mod Info");
                ImGui.NewLine();
                _techCatBlueprintUI.Display("Tech Categorys");
                ImGui.NewLine();
                _techBlueprintUI.Display("Techs");
                ImGui.NewLine();
                _componentBluprintUI.Display("Components");
                ImGui.NewLine();
                _cargoTypeBlueprintUI.Display("Cargo Types");
                ImGui.NewLine();
                _armorBlueprintUI.Display("Armor");
                ImGui.NewLine();
                _processedMaterialsUI.Display("Processed Materials");
                ImGui.NewLine();
                _mineralsBlueprintUI.Display("Minerals");
                ImGui.NewLine();
                _shipDesignBlueprintUI.Display("Ship Designs");
                ImGui.NewLine();
            }

            ImGui.End();
        }
    }
}