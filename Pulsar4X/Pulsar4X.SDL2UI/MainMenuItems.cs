using ImGuiNET;

namespace Pulsar4X.SDL2UI
{
    public class MainMenuItems : PulsarGuiWindow
    {

        bool _saveGame = false;
        GlobalUIState _state;

        ImVec2 buttonSize = new ImVec2(184, 24);
        internal MainMenuItems(GlobalUIState state)
        {
           
            IsActive = true;
            _state = state;
            _flags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar;
        }

        protected override void DisplayActual()
        {
            ImVec2 size = new ImVec2(200, 100);
            ImVec2 pos = new ImVec2(_state.MainWinSize.x / 2 - size.x / 2, _state.MainWinSize.y / 2 - size.y / 2);

            ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(pos, ImGuiCond.Always);

            ImGui.Begin("Pulsar4X Main Menu", ref IsActive, _flags);

            if (ImGui.Button("Start a New Game", buttonSize))
            {
                _state.NewGameOptions.IsActive = true;
                _state.OpenWindows.Add(_state.NewGameOptions);
                _state.OpenWindows.Remove(this);
                this.IsActive = false;
            }
            if (_state.IsGameLoaded)
                if (ImGui.Button("Save Current Game", buttonSize))
                    _saveGame = !_saveGame;
            ImGui.Button("Resume a Current Game", buttonSize);
            ImGui.Button("Connect to a Network Game", buttonSize);
            //if (_saveGame)

            ImGui.End();
        }
    }
}
