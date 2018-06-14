using ImGuiNET;

namespace Pulsar4X.SDL2UI
{
    public class MainMenuItems : PulsarGuiWindow
    {

        bool _saveGame = false;
        ImVec2 buttonSize = new ImVec2(184, 24);
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
                ImVec2 size = new ImVec2(200, 100);
                ImVec2 pos = new ImVec2(_state.MainWinSize.x / 2 - size.x / 2, _state.MainWinSize.y / 2 - size.y / 2);
                ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
                ImGui.SetNextWindowPos(pos, ImGuiCond.Always);

                if (ImGui.Begin("Pulsar4X Main Menu", ref IsActive, _flags))
                {

                    if (ImGui.Button("Start a New Game", buttonSize))
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
                /*
                if (!_state.LoadedWindows.Contains(SMPannel.GetInstance(_state)))
                    if (ImGui.Button("SM Mode", buttonSize))
                    {
                        var pannel = SMPannel.GetInstance(_state);
                        _state.LoadedWindows.Add(pannel);
                        _state.ActiveWindow = pannel;
                        pannel.IsActive = true;
                        this.IsActive = false;

                    }
                    */
                //if (_saveGame)

                ImGui.End();
            }
        }
    }
}
