using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.ECSLib;
using SDL2;
using System;
using System.Collections.Generic;
using System.Numerics;
namespace Pulsar4X.SDL2UI
{
    public class GlobalUIState
    {
        internal Game Game;
        internal FactionVM FactionUIState;
        internal bool IsGameLoaded { get { return Game != null; } }
        internal Entity Faction { get { return FactionUIState.FactionEntity; } }
        internal bool ShowMetrixWindow;
        internal IntPtr surfacePtr;
        internal IntPtr rendererPtr;

        //internal MainMenuItems MainMenu { get; }
        //internal NewGameOptions NewGameOptions { get; }
        //internal SettingsWindow SettingsWindow { get; }
        internal SystemMapRendering MapRendering { get; set; }
        internal EntityContextMenu ContextMenu { get; set; }
        //internal IOrderWindow ActiveOrderWidow { get; set; }
        //internal DebugWindow Debug { get; set; }

        internal Camera Camera;// = new Camera();
        internal ImGuiSDL2CSWindow ViewPort;
        internal Vector2 MainWinSize{get{return ViewPort.Size;}}

        internal Dictionary<Type, PulsarGuiWindow> LoadedWindows = new Dictionary<Type, PulsarGuiWindow>();
        internal PulsarGuiWindow ActiveWindow { get; set; }




        internal UserOrbitSettings UserOrbitSettings = new UserOrbitSettings();
        internal Dictionary<string, int> SDLImageDictionary = new Dictionary<string, int>();
        internal Dictionary<string, int> GLImageDictionary = new Dictionary<string, int>();

        internal EntityState LastClickedEntity;
        internal ECSLib.Vector4 LastWorldPointClicked;

        internal DateTime CurrentSystemDateTime; //= new DateTime();

        internal SpaceMasterVM SpaceMasterVM;

        internal GlobalUIState(ImGuiSDL2CSWindow viewport)
        {
            ViewPort = viewport;
            PulsarGuiWindow._state = this;
            var windowPtr = viewport.Handle;
            surfacePtr = SDL.SDL_GetWindowSurface(windowPtr);
            rendererPtr = SDL.SDL_GetRenderer(windowPtr);


            Camera = new Camera(viewport);

            MainMenuItems.GetInstance().IsActive = true;

            //LoadedWindows.Add(MainMenu);
            //NewGameOptions = new NewGameOptions(this);
            //SettingsWindow = new SettingsWindow(this);
            //ContextMenu = new EntityContextMenu(this);
            //Debug = new DebugWindow(this);

            //LoadedWindows.Add(SettingsWindow);

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


        internal void MapClicked(ECSLib.Vector4 worldCoord, MouseButtons button)
        {
            if (button == MouseButtons.Primary)
                LastWorldPointClicked = worldCoord;

            if (ActiveWindow != null)
                ActiveWindow.MapClicked(worldCoord, button);
        }
        internal void EntityClicked(Guid entityGuid, MouseButtons button)
        {
            
            if (button == MouseButtons.Primary)
                LastClickedEntity = MapRendering.IconEntityStates[entityGuid];

            if (ActiveWindow != null)
                ActiveWindow.EntityClicked(MapRendering.IconEntityStates[entityGuid], button);
        }

    }

    public abstract class PulsarGuiWindow
    {
        protected ImGuiWindowFlags _flags = ImGuiWindowFlags.None;
        //internal bool IsLoaded;
        internal bool IsActive = false;
        //internal int StateIndex = -1;
        //protected bool _IsOpen;
        internal static GlobalUIState _state;

       

        protected PulsarGuiWindow()
        {
             _state.LoadedWindows[this.GetType()] = this;
        }


        /*An example of how the constructor should be for a derived class. 
         * 
        private  DerivedClass (GlobalUIState state):base(state)
        {
            any other DerivedClass specific constrctor stuff here.
        }
        internal static DerivedClass GetInstance(GlobalUIState state)
        {
            if (!state.LoadedWindows.ContainsKey(typeof(DerivedClass)))
            {
                return new DerivedClass(state);
            }
            return (DerivedClass)state.LoadedWindows[typeof(DerivedClass)];
        }
        */

        internal abstract void Display();

        internal virtual void EntityClicked(EntityState entity, MouseButtons button) { }

        internal virtual void MapClicked(ECSLib.Vector4 worldPos, MouseButtons button) { }

        internal void Destroy()
        {
            /*
            IsLoaded = false;
            var lastItem = _state.LoadedWindows[_state.LoadedWindows.Count - 1];
            if (lastItem.StateIndex != _state.LoadedWindows.Count - 1)
                throw new Exception("index error in window count");
            _state.LoadedWindows.RemoveAt(lastItem.StateIndex);
            _state.LoadedWindows[StateIndex] = lastItem;
            */
        }

    }

}
