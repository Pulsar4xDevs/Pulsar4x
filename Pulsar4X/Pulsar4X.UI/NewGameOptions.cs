using System;
using Pulsar4X;
using ImGuiNET;

namespace Pulsar4X.VeldridUI
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
        int gameTypeButtonGrp = 0;
        gameType selectedGameType = gameType.Standalone;
        byte[] netPortInputBuffer = new byte[8];
        string netPortString { get { return System.Text.Encoding.UTF8.GetString(netPortInputBuffer); } }

        byte[] nameInputBuffer = System.Text.Encoding.UTF8.GetBytes("Test Game");
        byte[] factionNameInputBuffer = System.Text.Encoding.UTF8.GetBytes("UEF");
        string factionNameString { get { return System.Text.Encoding.UTF8.GetString(factionNameInputBuffer); } }
        byte[] passInputBuffer = new byte[8];
        string passString { get { return System.Text.Encoding.UTF8.GetString(passInputBuffer); } }

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

                    ImGui.InputText("Game Name", nameInputBuffer, (uint)nameInputBuffer.Length);
                    ImGui.InputText("Faction Name", factionNameInputBuffer, 16);
                    ImGui.InputText("Password", passInputBuffer, 16);

                    if (ImGui.RadioButton("Host Network Game", ref gameTypeButtonGrp, 1))
                        selectedGameType = gameType.Nethost;
                    if (ImGui.RadioButton("Start Standalone Game", ref gameTypeButtonGrp, 0))
                        selectedGameType = gameType.Standalone;
                    if (selectedGameType == gameType.Nethost)
                        ImGui.InputText("Network Port", netPortInputBuffer, 8);
                    if (ImGui.Button("Create New Game!"))
                        CreateNewGame(System.Text.Encoding.UTF8.GetString(nameInputBuffer));


                }
                else
                    MainMenuItems.GetInstance().IsActive = true;

                ImGui.End();
            }

        }

        void CreateNewGame(string name)
        {
            
            gameSettings = new ECSLib.NewGameSettings
            {
                GameName = name,
                MaxSystems = 0,
                SMPassword = "",
                //DataSets = options.SelectedModList.Select(dvi => dvi.Directory),
                CreatePlayerFaction = true,
                DefaultFactionName = "UEF",
                DefaultPlayerPassword = "",
                DefaultSolStart = true,
            };

            _state.Game = new ECSLib.Game(gameSettings);
            //_state.LoadedWindows.Add(new TimeControl(_state));
            //_state.LoadedWindows.Remove(this);
            ECSLib.FactionVM factionVM = new ECSLib.FactionVM(_state.Game);
            _state.FactionUIState = factionVM;

            factionVM.CreateDefaultFaction(factionNameString, passString);

            //_state.MapRendering.SetSystem(factionVM.KnownSystems[0]);
            _state.MapRendering.SetSystem(factionVM);
            DebugWindow.GetInstance().SetGameEvents();
            IsActive = false;
            TimeControl.GetInstance().IsActive = true;
        }
    }
}
