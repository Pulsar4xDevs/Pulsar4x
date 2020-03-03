using ImGuiNET;
using System.Numerics;
using Vector2 = System.Numerics.Vector2;

namespace Pulsar4X.SDL2UI
{
    public class MainMenuItems : PulsarGuiWindow
    {

        bool _saveGame = false;
        Vector2 buttonSize = new Vector2(184, 24);
        new ImGuiWindowFlags _flags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar;
        private MainMenuItems(){}
        internal static MainMenuItems GetInstance()
        {
            if (!_state.LoadedWindows.ContainsKey(typeof(MainMenuItems)))
            {
                return new MainMenuItems();
            }
            return (MainMenuItems)_state.LoadedWindows[typeof(MainMenuItems)];
        }


        internal override void Display()
        {
            if (IsActive)
            {
                Vector2 size = new Vector2(200, 100);
                Vector2 pos = new Vector2(_state.MainWinSize.X / 2 - size.X / 2, _state.MainWinSize.Y / 2 - size.Y / 2);
                ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
                ImGui.SetNextWindowPos(pos, ImGuiCond.Always);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));
                if (ImGui.Begin("Pulsar4X Main Menu", ref IsActive, _flags))
                {

                    if (ImGui.Button("Start a New Game", buttonSize) || _state.debugnewgame)
                    {
                        //_state.NewGameOptions.IsActive = true;
                        var newgameoptions = NewGameOptions.GetInstance();
                        newgameoptions.IsActive = true;
                        this.IsActive = false;
                    }
                    if (_state.IsGameLoaded)
                    {
                        if (ImGui.Button("Save Current Game", buttonSize))
                            _saveGame = !_saveGame;
                        if (ImGui.Button("Options", buttonSize))
                        {
                            SettingsWindow.GetInstance().IsActive = !SettingsWindow.GetInstance().IsActive;
                            IsActive = false;
                        }
                    }
                    ImGui.Button("Resume a Current Game", buttonSize);
                    ImGui.Button("Connect to a Network Game", buttonSize);
                }


                if (ImGui.Button("SM Mode", buttonSize))
                {
                    var pannel = SMPannel.GetInstance();
                    _state.ActiveWindow = pannel;
                    pannel.IsActive = true;
                    _state.EnableGameMaster();
                    this.IsActive = false;
                }
                //ImGui.GetForegroundDrawList().AddText(new System.Numerics.Vector2(500, 500), 16777215, "FooBarBaz");
                //if (_saveGame)

                ImGui.End();
                ImGui.PopStyleVar();
            }
        }
    }
}
