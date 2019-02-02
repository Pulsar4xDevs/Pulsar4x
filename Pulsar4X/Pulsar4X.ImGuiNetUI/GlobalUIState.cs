using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.ECSLib;
using SDL2;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.IO;
namespace Pulsar4X.SDL2UI
{
    public delegate void EntityClickedEventHandler(EntityState entityState, MouseButtons mouseButton);
    public class GlobalUIState
    {
        internal Game Game;
        internal FactionVM FactionUIState;
        internal bool IsGameLoaded { get { return Game != null; } }
        internal Entity Faction { get { return FactionUIState.FactionEntity; } }
        internal bool ShowMetrixWindow;
        internal bool ShowImgDbg;
        internal bool ShowDemoWindow;
        internal IntPtr surfacePtr;
        internal IntPtr rendererPtr;

        //internal MainMenuItems MainMenu { get; }
        //internal NewGameOptions NewGameOptions { get; }
        //internal SettingsWindow SettingsWindow { get; }
        internal GalacticMapRender GalacticMap;

        internal StarSystem PrimarySystem;
        internal SystemMapRendering PrimaryMapRender { get { return GalacticMap.PrimarySysMap; } }
        internal DateTime PrimarySystemDateTime; //= new DateTime();

        internal EntityContextMenu ContextMenu { get; set; }
        //internal IOrderWindow ActiveOrderWidow { get; set; }
        //internal DebugWindow Debug { get; set; }
        internal Dictionary<Guid, SystemState> StarSystemStates = new Dictionary<Guid, SystemState>();

        internal Camera Camera;// = new Camera();
        internal ImGuiSDL2CSWindow ViewPort;
        internal Vector2 MainWinSize{get{return ViewPort.Size;}}

        internal Dictionary<Type, PulsarGuiWindow> LoadedWindows = new Dictionary<Type, PulsarGuiWindow>();
        internal PulsarGuiWindow ActiveWindow { get; set; }


        internal UserOrbitSettings UserOrbitSettings = new UserOrbitSettings();
        internal Dictionary<string, IntPtr> SDLImageDictionary = new Dictionary<string, IntPtr>();
        internal Dictionary<string, int> GLImageDictionary = new Dictionary<string, int>();

        public event EntityClickedEventHandler EntityClickedEvent;
        internal EntityState LastClickedEntity;
        internal ECSLib.Vector4 LastWorldPointClicked;



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


            LoadImg("Logo", "Resources/PulsarLogo.bmp");
            LoadImg("PlayImg", "Resources/Play.bmp");
            LoadImg("PauseImg", "Resources/Pause.bmp");
            LoadImg("OneStepImg", "Resources/OneStep.bmp");
            LoadImg("UpImg", "Resources/UpArrow.bmp");
            LoadImg("DnImg", "Resources/DnArrow.bmp");
            LoadImg("RepeatImg", "Resources/RepeatIco.bmp");
            LoadImg("CancelImg", "Resources/CancelIco.bmp");

            /*
            int gltxtrID;
            GL.GenTextures(1, out gltxtrID);
            GL.BindTexture(GL.Enum.GL_TEXTURE_2D, gltxtrID);
            GL.PixelStorei(GL.Enum.GL_UNPACK_ROW_LENGTH, 0);
            GL.TexImage2D(GL.Enum.GL_TEXTURE_2D, 0, (int)GL.Enum.GL_RGBA, 16, 16, 0, GL.Enum.GL_RGBA, GL.Enum.GL_UNSIGNED_BYTE, playImgPtr);

            GLImageDictionary["PlayImg"] = gltxtrID;
            GL.Enable(GL.Enum.GL_TEXTURE_2D);
            */
        }

        internal void LoadImg(string name, string path)
        {
            IntPtr sdlSurface = SDL.SDL_LoadBMP(path);
            IntPtr sdltexture = SDL.SDL_CreateTextureFromSurface(rendererPtr, sdlSurface);
            SDLImageDictionary.Add(name, sdltexture);
        }

        internal void SetFaction(Entity factionEntity)
        {
            FactionInfoDB factionInfo = factionEntity.GetDataBlob<FactionInfoDB>();
            StarSystemStates = new Dictionary<Guid, SystemState>();
            foreach (var guid in factionInfo.KnownSystems)
            {
                StarSystemStates[guid] = new SystemState(Game.Systems[guid], factionEntity);
            }
            GalacticMap.SetFaction();
        }

        internal void SetActiveSystem(Guid activeSysID)
        {
            PrimarySystem = StarSystemStates[activeSysID].StarSystem;
            PrimarySystemDateTime = PrimarySystem.ManagerSubpulses.SystemLocalDateTime;
            GalacticMap.PrimarySysMap = GalacticMap.RenderedMaps[activeSysID];
        }

        internal void EnableGameMaster()
        {
            StarSystemStates = new Dictionary<Guid, SystemState>();
            foreach (var system in Game.Systems)
            {
                StarSystemStates[system.Key] = SystemState.GetMasterState(system.Value);
            }
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
        
            LastClickedEntity = StarSystemStates[PrimarySystem.Guid].EntityStates[entityGuid];

            EntityClickedEvent?.Invoke(LastClickedEntity, button);

            if (ActiveWindow != null)
                ActiveWindow.EntityClicked(StarSystemStates[PrimarySystem.Guid].EntityStates[entityGuid], button);
            OnEntitySelected();
        }

        void OnEntitySelected()
        {
            PrimaryMapRender.SelectedEntityExtras = new List<IDrawData>();
            if(LastClickedEntity.DebugOrbitOrder != null)
            {
                PrimaryMapRender.SelectedEntityExtras.Add(LastClickedEntity.DebugOrbitOrder);
            }
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
