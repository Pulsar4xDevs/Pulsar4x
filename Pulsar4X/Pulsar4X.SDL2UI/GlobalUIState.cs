using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.ECSLib;
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
        internal bool ShowMetrixWindow;
        internal IntPtr surfacePtr;
        internal IntPtr rendererPtr;

        internal MainMenuItems MainMenu { get; }
        internal NewGameOptions NewGameOptions { get; }
        internal SettingsWindow SettingsWindow { get; }
        internal SystemMapRendering MapRendering { get; set; }
        internal EntityContextMenu ContextMenu { get; set; }
        internal IOrderWindow ActiveOrderWidow { get; set; }
        internal DebugWindow Debug { get; set; }

        internal Camera Camera;// = new Camera();
        internal ImGuiSDL2CSWindow ViewPort;
        internal ImVec2 MainWinSize{get{return ViewPort.Size;}}

        internal List<PulsarGuiWindow> OpenWindows = new List<PulsarGuiWindow>();
        internal PulsarGuiWindow ActiveWindow { get; set; }
        internal UserOrbitSettings UserOrbitSettings = new UserOrbitSettings();
        internal Dictionary<string, int> SDLImageDictionary = new Dictionary<string, int>();
        internal Dictionary<string, int> GLImageDictionary = new Dictionary<string, int>();

        internal EntityState LastClickedEntity;
        internal Vector4 LastWorldPointClicked;

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
            //ContextMenu = new EntityContextMenu(this);
            Debug = new DebugWindow(this);

            OpenWindows.Add(SettingsWindow);

            IntPtr logoPtr = SDL.SDL_LoadBMP("Resources/PulsarLogo.bmp");
            SDLImageDictionary.Add("Logo", SDL.SDL_CreateTextureFromSurface(rendererPtr, logoPtr).ToInt32());

            IntPtr playImgPtr = SDL.SDL_LoadBMP("Resources/Play.bmp");
            SDLImageDictionary.Add("PlayImg", SDL.SDL_CreateTextureFromSurface(rendererPtr, playImgPtr).ToInt32());


            int gltxtrID;
            GL.GenTextures(1, out gltxtrID);
            GL.BindTexture(GL.Enum.GL_TEXTURE_2D, gltxtrID);
            GL.PixelStorei(GL.Enum.GL_UNPACK_ROW_LENGTH, 0);
            GL.TexImage2D(GL.Enum.GL_TEXTURE_2D, 0, (int)GL.Enum.GL_RGBA, 16, 16, 0, GL.Enum.GL_RGBA, GL.Enum.GL_UNSIGNED_BYTE, playImgPtr);

            GLImageDictionary["PlayImg"] = gltxtrID;
            GL.Enable(GL.Enum.GL_TEXTURE_2D);
        }


        internal void MapClicked(Vector4 worldCoord, MouseButtons button)
        {
            if (button == MouseButtons.Primary)
                LastWorldPointClicked = worldCoord;

            if (ActiveWindow != null)
                ActiveWindow.MapClicked(worldCoord, button);
        }
        internal void EntityClicked(Guid entityGuid, MouseButtons button)
        {
            
            if (button == 0)
                LastClickedEntity = MapRendering.IconEntityStates[entityGuid];

            if (ActiveWindow != null)
                ActiveWindow.EntityClicked(MapRendering.IconEntityStates[entityGuid].Entity, button);
        }

    }
}
