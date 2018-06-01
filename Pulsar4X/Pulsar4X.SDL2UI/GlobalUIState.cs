using ImGuiNET;
using ImGuiSDL2CS;
using SDL2;
using System;
using System.Collections.Generic;

namespace Pulsar4X.SDL2UI
{
    public class GlobalUIState
    {
        internal ECSLib.Game Game;
        internal ECSLib.FactionVM FactionUIState;
        internal bool IsGameLoaded { get { return Game != null; } }
        internal ECSLib.Entity Faction { get { return FactionUIState.FactionEntity; } }

        internal IntPtr surfacePtr;
        internal IntPtr rendererPtr;

        internal MainMenuItems MainMenu { get; }
        internal NewGameOptions NewGameOptions { get; }
        internal SettingsWindow SettingsWindow { get; }
        internal SystemMapRendering MapRendering { get; set; }
        internal DebugWindow Debug { get; set; }

        internal Camera Camera;// = new Camera();
        internal ImGuiSDL2CSWindow ViewPort;
        internal ImVec2 MainWinSize{get{return ViewPort.Size;}}

        internal List<PulsarGuiWindow> OpenWindows = new List<PulsarGuiWindow>();
        //internal PulsarGuiWindow ActiveWindow { get; set; }
        internal UserOrbitSettings UserOrbitSettings = new UserOrbitSettings();
        internal Dictionary<string, int> ImageDictionary = new Dictionary<string, int>();

        internal GlobalUIState(ImGuiSDL2CSWindow viewport)
        {
            ViewPort = viewport;

            var windowPtr = viewport.Handle;
            surfacePtr = SDL.SDL_GetWindowSurface(windowPtr);
            rendererPtr = SDL.SDL_GetRenderer(windowPtr);


            Camera = new Camera(viewport);

            MainMenu = new MainMenuItems(this);
            OpenWindows.Add(MainMenu);
            NewGameOptions = new NewGameOptions(this);
            SettingsWindow = new SettingsWindow(this);
            Debug = new DebugWindow(this);
            OpenWindows.Add(SettingsWindow);

            var logo = SDL.SDL_LoadBMP("Resources/PulsarLogo.bmp");
            ImageDictionary.Add("Logo", SDL.SDL_CreateTextureFromSurface(rendererPtr, logo).ToInt32());

            IntPtr playImg = SDL.SDL_LoadBMP("Resources/Play.bmp");
            ImageDictionary.Add("PlayImg", SDL.SDL_CreateTextureFromSurface(rendererPtr, playImg).ToInt32());

        }
    }
}
