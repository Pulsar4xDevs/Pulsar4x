using System;
using ImGuiNET;
using System.IO;
using Pulsar4X.Client.Interface.Menus;
using System.Numerics;
using Pulsar4X.Client.Interface.Widgets;
using System.Diagnostics;
using Pulsar4X.Client.ModFileEditing;

namespace Pulsar4X.Client
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
            if(!IsActive) return;

            System.Numerics.Vector2 size = new System.Numerics.Vector2(412, 300);
            System.Numerics.Vector2 pos = new System.Numerics.Vector2(
                    _uiState.ViewPort.Size.Width / 2 - size.X / 2,
                    _uiState.ViewPort.Size.Height / 2 - size.Y / 2);

            ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(pos, ImGuiCond.Always);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(10, 10));
            if (Window.Begin("Pulsar4X Main Menu", ref IsActive, _flags))
            {
                ImGui.Image(_uiState.Img_MainMenuLogo().ToTextureRef(), new System.Numerics.Vector2(400, 200));

                if (ImGui.Button("New Game...", _buttonSize) || _uiState.debugnewgame)
                {
                    //_uiState.NewGameOptions.IsActive = true;
                    var newgameoptions = NewGameMenu.GetInstance();
                    newgameoptions.SetActive(true);
                    this.IsActive = false;
                }
                if (ImGui.Button("Quickstart", _buttonSize))
                {
                    NewGameMenu.QuickstartGame();
                    this.IsActive = false;
                }
                if (_uiState.IsGameLoaded)
                {
                    if (ImGui.Button("Save Game...", _buttonSize))
                    {
                        _saveGame = !_saveGame;

                        // Set the save name equal to the corporation name by default (player can change it in the dialog)
                        string corpName = _uiState.Faction?.GetFactionName() ?? "Unknown";
                        string dateTime = _uiState.SelectedSystemTime.ToString("yyyy-MM-dd_HH-mm-ss");
                        string unsanitizedName = $"{corpName} - {dateTime}";

                        // Remove any invalid filename characters
                        char[] invalidChars = Path.GetInvalidFileNameChars();
                        string saveName = string.Join("_", unsanitizedName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

                        SaveGame.GetInstance().UpdateSaveName(saveName);
                        SaveGame.GetInstance().ToggleActive();
                        SetActive(false);
                    }

                    if (ImGui.Button("Settings", _buttonSize))
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
                if (ImGui.Button("Resume Last Save", _buttonSize))
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
                

                if(ImageButton.Begin(_uiState.Img_Discord(), "Discord", new Vector2(16, 12), _buttonSize))
                {
                    try
                    {
                        ProcessStartInfo psi = new ProcessStartInfo
                        {
                            FileName = "https://discord.gg/3uwCQSn",
                            UseShellExecute = true
                        };
                        Process.Start(psi);
                    }
                    catch (Exception ex)
                    {
                        // Handle any errors
                        Console.WriteLine($"Error opening URL: {ex.Message}");
                    }
                }

                if(ImGui.Button("Exit to Desktop", _buttonSize))
                {
                    _uiState.ViewPort.IsAlive = false;
                }
            }

            Window.End();
            ImGui.PopStyleVar();
        }

        public override void OnGameTickChange(DateTime newDate)
        {
        }

        public override void OnSystemTickChange(DateTime newDate)
        {
        }

        private bool DoAnySavesExist()
        {
            var appDataDirectory = PulsarMainWindow.GetAppDataPath();

            if(string.IsNullOrEmpty(appDataDirectory))
            {
                return false;
            }

            var path = Path.Combine(appDataDirectory, PulsarMainWindow.SavesPath);
            var saveFiles = Directory.GetFiles(path, "*.sav");

            return saveFiles.Length > 0;
        }
    }
}
