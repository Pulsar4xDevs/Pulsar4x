using System;
using Pulsar4X;
using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.ECSLib;
using Pulsar4X.Modding;

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

        ECSLib.NewGameSettings gameSettings = new ECSLib.NewGameSettings();

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

            gameSettings = new ECSLib.NewGameSettings
            {
                GameName = ImGuiSDL2CSHelper.StringFromBytes(_nameInputBuffer),
                MaxSystems = _maxSystems,
                SMPassword = ImGuiSDL2CSHelper.StringFromBytes(_smPassInputbuffer),
                //DataSets = options.SelectedModList.Select(dvi => dvi.Directory),
                CreatePlayerFaction = true,
                DefaultFactionName = ImGuiSDL2CSHelper.StringFromBytes(_factionInputBuffer),
                DefaultPlayerPassword = ImGuiSDL2CSHelper.StringFromBytes(_passInputBuffer),
                DefaultSolStart = true,
                MasterSeed = _masterSeed
            };

            Pulsar4X.Engine.NewGameSettings newGameSettings = new Engine.NewGameSettings()
            {
                GameName = ImGuiSDL2CSHelper.StringFromBytes(_nameInputBuffer),
                MaxSystems = _maxSystems,
                SMPassword = ImGuiSDL2CSHelper.StringFromBytes(_smPassInputbuffer),
                //DataSets = options.SelectedModList.Select(dvi => dvi.Directory),
                CreatePlayerFaction = true,
                DefaultFactionName = ImGuiSDL2CSHelper.StringFromBytes(_factionInputBuffer),
                DefaultPlayerPassword = ImGuiSDL2CSHelper.StringFromBytes(_passInputBuffer),
                DefaultSolStart = true,
                MasterSeed = _masterSeed
            };

            Pulsar4X.Engine.Game game = new Pulsar4X.Engine.Game(newGameSettings, modDataStore);

            var factionName = ImGuiSDL2CSHelper.StringFromBytes(_factionInputBuffer);
            var factionPasswd = ImGuiSDL2CSHelper.StringFromBytes(_passInputBuffer);

            var newGameFaction = Pulsar4X.Engine.DefaultStartFactory.DefaultHumans(game, factionName);

            //TODO: Tidyup: new Game(gameSettings) doesn't currently create a default faction as per the settings.
            //this should probilby be fixed, either we create it there or we... dont.
            _uiState.Game = new ECSLib.Game(gameSettings);



            var factionEntity = DefaultStartFactory.DefaultHumans(StaticRefLib.Game, factionName);
            AuthProcessor.StorePasswordAsHash(StaticRefLib.Game, factionEntity, factionPasswd);
            _uiState.SetFaction(factionEntity);
            _uiState.SetActiveSystem(factionEntity.GetDataBlob<FactionInfoDB>().KnownSystems[0]);

            DebugWindow.GetInstance().SetGameEvents();
            IsActive = false;
            //we initialize window instances so that they get always displayed and automatically open after new game is created.
            TimeControl.GetInstance().SetActive();
            ToolBarWindow.GetInstance().SetActive();
            //EntityUIWindowSelector.GetInstance().SetActive();
            //EntityInfoPanel.GetInstance().SetActive();
        }
    }
}