using System;
using ImGuiNET;
using System.IO;
using Pulsar4X.Client.Interface.Menus;
using Pulsar4X.SDL2UI.ModFileEditing;

namespace Pulsar4X.SDL2UI
{
    public class MainMenuItems : PulsarGuiWindow
    {

        bool _saveGame = false;
        System.Numerics.Vector2 _buttonSize = new System.Numerics.Vector2(400, 24);
        new ImGuiWindowFlags _flags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar;
        private MainMenuItems(){}
        internal static MainMenuItems GetInstance()
        {
            if (!_uiState.LoadedWindows.ContainsKey(typeof(MainMenuItems)))
            {
                return new MainMenuItems();
            }
            return (MainMenuItems)_uiState.LoadedWindows[typeof(MainMenuItems)];
        }


        internal override void Display()
        {
            if (IsActive)
            {
                System.Numerics.Vector2 size = new System.Numerics.Vector2(412, 300);
                System.Numerics.Vector2 pos = new System.Numerics.Vector2(_uiState.MainWinSize.X / 2 - size.X / 2, _uiState.MainWinSize.Y / 2 - size.Y / 2);
                ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
                ImGui.SetNextWindowPos(pos, ImGuiCond.Always);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(10, 10));
                if (ImGui.Begin("Pulsar4X Main Menu", ref IsActive, _flags))
                {
                    ImGui.Image(_uiState.Img_MainMenuLogo(), new System.Numerics.Vector2(400, 200));

                    if (ImGui.Button("Start a New Game", _buttonSize) || _uiState.debugnewgame)
                    {
                        //_uiState.NewGameOptions.IsActive = true;
                        var newgameoptions = NewGameMenu.GetInstance();
                        newgameoptions.SetActive(true);
                        this.IsActive = false;
                    }
                    if (_uiState.IsGameLoaded)
                    {
                        if (ImGui.Button("Save Game...", _buttonSize))
                        {
                            _saveGame = !_saveGame;

                            // Set the save name equal to the game name by default (player can change it in the dialog)
                            SaveGame.GetInstance().UpdateSaveName(_uiState.Game.Name);
                            SaveGame.GetInstance().ToggleActive();
                            SetActive(false);
                        }

                        if (ImGui.Button("Options", _buttonSize))
                        {
                            SettingsWindow.GetInstance().ToggleActive();
                            this.SetActive(false);
                        }
                        if (ImGui.Button("Editor", _buttonSize))
                        {
                            ModFileEditor.GetInstance().ToggleActive();
                            this.SetActive(false);
                        }

                        if(ImGui.Button("Preferences", _buttonSize))
                        {
                            SystemViewPreferences.GetInstance().ToggleActive();
                            this.SetActive(false);
                        }

                        if (ImGui.Button("SM Mode", _buttonSize))
                        {
                            var pannel = SMWindow.GetInstance();
                            _uiState.ActiveWindow = pannel;
                            pannel.SetActive();
                            _uiState.ToggleGameMaster();
                            this.IsActive = false;
                        }
                    }

                    var disabled = !DoAnySavesExist();
                    if(disabled)
                        ImGui.BeginDisabled();
                    if (ImGui.Button("Resume a Current Game", _buttonSize))
                    {
                        LoadGame.GetInstance().LoadLatest();
                        SetActive(false);
                    }
                    if(disabled)
                        ImGui.EndDisabled();
                    if (ImGui.Button("Load Game...", _buttonSize))
                    {
                        LoadGame.GetInstance().ToggleActive();
                        SetActive(false);
                    }
                    //ImGui.Button("Connect to a Network Game", buttonSize);
                }

                if(ImGui.Button("Exit to Desktop", _buttonSize))
                {
                    _uiState.ViewPort.IsAlive = false;
                }

                ImGui.End();
                ImGui.PopStyleVar();
            }
        }

        public override void OnGameTickChange(DateTime newDate)
        {
        }

        public override void OnSystemTickChange(DateTime newDate)
        {
        }

        private bool DoAnySavesExist()
        {
            var path = Path.Combine(PulsarMainWindow.GetAppDataPath(), PulsarMainWindow.SavesPath);
            var saveFiles = Directory.GetFiles(path, "*.sav");

            return saveFiles.Length > 0;
        }
    }
}
