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
        GlobalUIState _state;
        enum gameType { Nethost, Standalone }
        int gameTypeButtonGrp = 0;
        gameType selectedGameType = gameType.Standalone;
        byte[] netPortInputBuffer = new byte[8];
        string netPortString { get { return System.Text.Encoding.UTF8.GetString(netPortInputBuffer); } }

        byte[] nameInputBuffer = System.Text.Encoding.UTF8.GetBytes("Test Game");


        internal NewGameOptions(GlobalUIState state)
        { _state = state;
        }



        ECSLib.NewGameSettings gameSettings = new ECSLib.NewGameSettings();
        internal override void Display()
        {
            ImGui.Begin("New Game Setup");
            ImGui.InputText("Game Name", nameInputBuffer, (uint)nameInputBuffer.Length);

            if (ImGui.RadioButton("Host Network Game", ref gameTypeButtonGrp, 1))
                selectedGameType = gameType.Nethost;
            if (ImGui.RadioButton("Start Standalone Game", ref gameTypeButtonGrp, 0))
                selectedGameType = gameType.Standalone;
            if (selectedGameType == gameType.Nethost)
                ImGui.InputText("Network Port", netPortInputBuffer, 8);
            if (ImGui.Button("Create New Game!"))
                CreateNewGame(System.Text.Encoding.UTF8.GetString(nameInputBuffer));

            ImGui.End();

        }

        void CreateNewGame(string name)
        {
            
            gameSettings = new ECSLib.NewGameSettings
            {
                GameName = name,
                MaxSystems = 100,
                SMPassword = "",
                //DataSets = options.SelectedModList.Select(dvi => dvi.Directory),
                CreatePlayerFaction = true,
                DefaultFactionName = "UEF",
                DefaultPlayerPassword = "",
                DefaultSolStart = true,
            };

            _state.Game = new ECSLib.Game(gameSettings);
            _state.ActiveWindows.Add(new TimeControl(_state));
            ECSLib.UIStateVM uIStateVM = new ECSLib.UIStateVM(_state.Game);

            ECSLib.Entity factionEntity = ECSLib.DefaultStartFactory.DefaultHumans(_state.Game, gameSettings.DefaultFactionName);
            ECSLib.AuthProcessor.StorePasswordAsHash(_state.Game, factionEntity, gameSettings.DefaultPlayerPassword);
            uIStateVM.FactionEntity = factionEntity;
            
        }
    }
}
