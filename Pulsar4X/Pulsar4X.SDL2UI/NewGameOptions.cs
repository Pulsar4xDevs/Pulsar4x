using System;
using Pulsar4X;
using ImGuiNET;

namespace Pulsar4X.SDL2UI
{
    public class NewGameOptions: PulsarGuiWindow
    {
        GlobalUIState _state;
        enum gameType { Nethost, Standalone }
        int gameTypeButtonGrp = 0;
        gameType selectedGameType = gameType.Standalone;
        byte[] netPortInputBuffer = new byte[8];
        string netPortString { get { return System.Text.Encoding.UTF8.GetString(netPortInputBuffer); } }
        byte[] nameInputBuffer = new byte[16];

        internal NewGameOptions(GlobalUIState state)
        { _state = state; }



        ECSLib.NewGameSettings gameSettings = new ECSLib.NewGameSettings();
        internal override void Display()
        {
            ImGui.InputText("Game Name", nameInputBuffer, 16);
            if (ImGui.RadioButton("Host Network Game", ref gameTypeButtonGrp, 1))
                selectedGameType = gameType.Nethost;
            if (ImGui.RadioButton("Start Standalone Game", ref gameTypeButtonGrp, 0))
                selectedGameType = gameType.Standalone;
            if (selectedGameType == gameType.Nethost)
                ImGui.InputText("Network Port", netPortInputBuffer, 8);
            if (ImGui.Button("Create New Game!"))
                DoNewGame(System.Text.Encoding.UTF8.GetString(nameInputBuffer));



        }

        void DoNewGame(string name)
        {
            
            var gameSettings = new Pulsar4X.ECSLib.NewGameSettings
            {
                GameName = "name",
                MaxSystems = 100,
                SMPassword = "",
                //DataSets = options.SelectedModList.Select(dvi => dvi.Directory),
                CreatePlayerFaction = true,
                DefaultFactionName = "UEF",
                DefaultPlayerPassword = "",
                DefaultSolStart = true,
            };
            _state.Game = new ECSLib.Game(gameSettings);


        }

    }



}
