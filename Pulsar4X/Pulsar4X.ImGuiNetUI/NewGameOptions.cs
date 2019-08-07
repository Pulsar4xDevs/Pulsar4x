using System;
using Pulsar4X;
using ImGuiNET;

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
        byte[] _nameInputBuffer = System.Text.Encoding.UTF8.GetBytes("Test Game");
        byte[] _factionNameInputBuffer = System.Text.Encoding.UTF8.GetBytes("UEF");
        string _factionNameString { get { return System.Text.Encoding.UTF8.GetString(_factionNameInputBuffer); } }
        byte[] _passInputBuffer = new byte[8];
        string _passString { get { return System.Text.Encoding.UTF8.GetString(_passInputBuffer); } }
        int _masterSeed = 12345678;
        private NewGameOptions() 
        { 

        }
        internal static NewGameOptions GetInstance()
        {
            if (!_state.LoadedWindows.ContainsKey(typeof(NewGameOptions)))
            {
                return new NewGameOptions();
            }
            return (NewGameOptions)_state.LoadedWindows[typeof(NewGameOptions)];
        }

        ECSLib.NewGameSettings gameSettings = new ECSLib.NewGameSettings();

        internal override void Display()
        {
            if (IsActive)
            {
                if (ImGui.Begin("New Game Setup", ref IsActive, _flags))
                {

                    ImGui.InputText("Game Name", _nameInputBuffer, (uint)_nameInputBuffer.Length);
                    ImGui.InputText("Faction Name", _factionNameInputBuffer, 16);
                    ImGui.InputText("Password", _passInputBuffer, 16);
                    ImGui.InputInt("Max Systems", ref _maxSystems);
                    ImGui.InputInt("Master Seed:", ref _masterSeed);
                    if (ImGui.RadioButton("Host Network Game", ref _gameTypeButtonGrp, 1))
                        _selectedGameType = gameType.Nethost;
                    if (ImGui.RadioButton("Start Standalone Game", ref _gameTypeButtonGrp, 0))
                        _selectedGameType = gameType.Standalone;
                    if (_selectedGameType == gameType.Nethost)
                        ImGui.InputText("Network Port", _netPortInputBuffer, 8);
                    if (ImGui.Button("Create New Game!"))
                        CreateNewGame(System.Text.Encoding.UTF8.GetString(_nameInputBuffer));

                    ImGui.End();
                }
                else
                    MainMenuItems.GetInstance().IsActive = true;
                
            }

        }

        void CreateNewGame(string name)
        {

            gameSettings = new ECSLib.NewGameSettings
            {
                GameName = name,
                MaxSystems = _maxSystems,
                SMPassword = "",
                //DataSets = options.SelectedModList.Select(dvi => dvi.Directory),
                CreatePlayerFaction = true,
                DefaultFactionName = "UEF",
                DefaultPlayerPassword = "",
                DefaultSolStart = true,
                MasterSeed = _masterSeed
            };

            _state.Game = new ECSLib.Game(gameSettings);
            //_state.LoadedWindows.Add(new TimeControl(_state));
            //_state.LoadedWindows.Remove(this);
            ECSLib.FactionVM factionVM = new ECSLib.FactionVM(_state.Game);
            _state.FactionUIState = factionVM;

            factionVM.CreateDefaultFaction(_factionNameString, _passString);

            _state.SetFaction(factionVM.FactionEntity);
            //_state.MapRendering.SetSystem(factionVM.KnownSystems[0]);
            //_state.MapRendering.SetSystem(factionVM);
            _state.SetActiveSystem(factionVM.KnownSystems[0].Guid);
            DebugWindow.GetInstance().SetGameEvents();
            IsActive = false;
            TimeControl.GetInstance().IsActive = true;
            ToolBarUI.GetInstance().IsActive = true;
        }
    }
}
