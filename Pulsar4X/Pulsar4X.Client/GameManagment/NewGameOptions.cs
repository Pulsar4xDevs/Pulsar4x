using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.Engine;
using Pulsar4X.Modding;
using Pulsar4X.Datablobs;

namespace Pulsar4X.SDL2UI
{
    static class Helper
    {
        public static byte[] ToByteArray(this string str)
        {
            return System.Text.Encoding.UTF8.GetBytes(str);
        }
    }

    public class NewGameOptions: PulsarGuiWindow
    {

        enum gameType { Nethost, Standalone }
        int _gameTypeButtonGrp = 0;
        gameType _selectedGameType = gameType.Standalone;
        byte[] _netPortInputBuffer = new byte[8];
        string _netPortString { get { return System.Text.Encoding.UTF8.GetString(_netPortInputBuffer); } }
        int _maxSystems = 5;


        byte[] _nameInputBuffer = ImGuiSDL2CSHelper.BytesFromString("Test Game", 16);
        byte[] _factionInputBuffer = ImGuiSDL2CSHelper.BytesFromString("UEF", 16);
        byte[] _passInputBuffer = ImGuiSDL2CSHelper.BytesFromString("", 16);

        byte[] _smPassInputbuffer = ImGuiSDL2CSHelper.BytesFromString("", 16);

        int _masterSeed = 12345678;
        private NewGameOptions()
        {

        }
        internal static NewGameOptions GetInstance()
        {
            if (!_uiState.LoadedWindows.ContainsKey(typeof(NewGameOptions)))
            {
                return new NewGameOptions();
            }
            return (NewGameOptions)_uiState.LoadedWindows[typeof(NewGameOptions)];
        }

        NewGameSettings gameSettings = new NewGameSettings();

        internal override void Display()
        {
            if (IsActive)
            {
                if (ImGui.Begin("New Game Setup", ref IsActive, _flags))
                {

                    ImGui.InputText("Game Name", _nameInputBuffer, 16);
                    ImGui.InputText("SM Pass", _smPassInputbuffer, 16);


                    ImGui.InputText("Faction Name", _factionInputBuffer, 16);
                    ImGui.InputText("Password", _passInputBuffer, 16);

                    ImGui.InputInt("Max Systems", ref _maxSystems);
                    ImGui.InputInt("Master Seed:", ref _masterSeed);

                    if (ImGui.RadioButton("Host Network Game", ref _gameTypeButtonGrp, 1))
                        _selectedGameType = gameType.Nethost;
                    if (ImGui.RadioButton("Start Standalone Game", ref _gameTypeButtonGrp, 0))
                        _selectedGameType = gameType.Standalone;
                    if (_selectedGameType == gameType.Nethost)
                        ImGui.InputText("Network Port", _netPortInputBuffer, 8);
                    if (ImGui.Button("Create New Game!") || _uiState.debugnewgame)
                    {
                        _uiState.debugnewgame = false;
                        CreateNewGame();
                    }


                    ImGui.End();
                }
                else
                    MainMenuItems.GetInstance().SetActive();

            }

        }

        void CreateNewGame()
        {
            ModLoader modLoader = new ModLoader();
            ModDataStore modDataStore = new ModDataStore();
            modLoader.LoadModManifest("Data/basemod/modInfo.json", modDataStore);

            gameSettings = new NewGameSettings
            {
                GameName = ImGuiSDL2CSHelper.StringFromBytes(_nameInputBuffer),
                MaxSystems = _maxSystems,
                SMPassword = ImGuiSDL2CSHelper.StringFromBytes(_smPassInputbuffer),
                CreatePlayerFaction = true,
                DefaultFactionName = ImGuiSDL2CSHelper.StringFromBytes(_factionInputBuffer),
                DefaultPlayerPassword = ImGuiSDL2CSHelper.StringFromBytes(_passInputBuffer),
                DefaultSolStart = true,
                MasterSeed = _masterSeed
            };

            Pulsar4X.Engine.Game game = new Pulsar4X.Engine.Game(gameSettings, modDataStore);

            // TODO: need to add the implementation for a random start
            var (newGameFaction, systemId) = Pulsar4X.Engine.DefaultStartFactory.LoadFromJson(game, "Data/basemod/defaultStart.json");

            if(newGameFaction == null) return;

            _uiState.Game = game;
            _uiState.SetFaction(newGameFaction, true);
            _uiState.SetActiveSystem(systemId);

            DebugWindow.GetInstance().SetGameEvents();
            IsActive = false;
            //we initialize window instances so that they get always displayed and automatically open after new game is created.
            TimeControl.GetInstance().SetActive();
            ToolBarWindow.GetInstance().SetActive();
            Selector.GetInstance().SetActive();
            //EntityUIWindowSelector.GetInstance().SetActive();
            //EntityInfoPanel.GetInstance().SetActive();
        }
    }
}