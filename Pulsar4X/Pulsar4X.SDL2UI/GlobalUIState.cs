using ImGuiNET;
using ImGuiSDL2CS;
using System.Collections.Generic;

namespace Pulsar4X.SDL2UI
{
    internal class GlobalUIState
    {
        internal ECSLib.Game Game;
        internal ECSLib.FactionVM FactionUIState;
        internal bool IsGameLoaded { get { return Game != null; } }
        internal ECSLib.Entity Faction { get { return FactionUIState.FactionEntity; } }

        internal MainMenuItems MainMenu { get; }
        internal NewGameOptions NewGameOptions { get; }
        internal SystemMapRendering MapRendering { get; set; }
        internal Camera Camera;// = new Camera();
        internal ImGuiSDL2CSWindow ViewPort;
        internal ImVec2 MainWinSize{get{return ViewPort.Size;}}

        internal List<PulsarGuiWindow> OpenWindows = new List<PulsarGuiWindow>();
        //internal PulsarGuiWindow ActiveWindow { get; set; }

        internal GlobalUIState(ImGuiSDL2CSWindow viewport)
        {
            ViewPort = viewport;
            Camera = new Camera(viewport);

            MainMenu = new MainMenuItems(this);
            OpenWindows.Add(MainMenu);
            NewGameOptions = new NewGameOptions(this);
        }
    }
}
