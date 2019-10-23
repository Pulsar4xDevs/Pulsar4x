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
        internal bool ShowDamageWindow;
        internal IntPtr rendererPtr;


        internal GalacticMapRender GalacticMap;

        internal StarSystem SelectedSystem { get { return StarSystemStates[SelectedStarSysGuid].StarSystem; } }
        internal Guid SelectedStarSysGuid { get { return GalacticMap.SelectedStarSysGuid; } }
        internal SystemMapRendering SelectedSysMapRender { get { return GalacticMap.SelectedSysMapRender; } }
        internal DateTime PrimarySystemDateTime; //= new DateTime();

        internal EntityContextMenu ContextMenu { get; set; }

        internal Dictionary<Guid, SystemState> StarSystemStates = new Dictionary<Guid, SystemState>();

        internal Camera Camera;// = new Camera();
        internal ImGuiSDL2CSWindow ViewPort;
        internal System.Numerics.Vector2 MainWinSize { get {return ViewPort.Size;}}

        internal Dictionary<Type, PulsarGuiWindow> LoadedWindows = new Dictionary<Type, PulsarGuiWindow>();
        internal PulsarGuiWindow ActiveWindow { get; set; }

        internal List<List<UserOrbitSettings>> UserOrbitSettingsMtx = new List<List<UserOrbitSettings>>();
        internal List<float> DrawNameZoomLvl = new List<float>();

        internal Dictionary<string, IntPtr> SDLImageDictionary = new Dictionary<string, IntPtr>();
        internal Dictionary<string, int> GLImageDictionary = new Dictionary<string, int>();

        public event EntityClickedEventHandler EntityClickedEvent;
        internal EntityState LastClickedEntity;
        internal ECSLib.Vector3 LastWorldPointClicked;



        internal SpaceMasterVM SpaceMasterVM;

        internal GlobalUIState(ImGuiSDL2CSWindow viewport)
        {
            ViewPort = viewport;
            PulsarGuiWindow._state = this;
            var windowPtr = viewport.Handle;
            SDL.SDL_SetHint(SDL.SDL_HINT_RENDER_DRIVER, "opengl");
            //var surfacePtr = SDL.SDL_GetWindowSurface(windowPtr);
            rendererPtr = SDL.SDL_CreateRenderer(windowPtr, -1, SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED);
            


            for (int i = 0; i < (int)UserOrbitSettings.OrbitBodyType.NumberOf; i++)
            {
                UserOrbitSettingsMtx.Add(new List<UserOrbitSettings>());
                DrawNameZoomLvl.Add(100f);
                for (int j = 0; j < (int)UserOrbitSettings.OrbitTrajectoryType.NumberOf; j++)
                {
                    UserOrbitSettingsMtx[i].Add(new UserOrbitSettings());
                }
            }


            Camera = new Camera(viewport);

            MainMenuItems.GetInstance().IsActive = true;

            string rf = "Resources";
            LoadImg("Logo", Path.Combine( rf,"PulsarLogo.bmp"));
            LoadImg("PlayImg", Path.Combine( rf,"Play.bmp"));
            LoadImg("PauseImg", Path.Combine( rf,"Pause.bmp"));
            LoadImg("OneStepImg", Path.Combine( rf,"OneStep.bmp"));
            LoadImg("UpImg", Path.Combine( rf,"UpArrow.bmp"));
            LoadImg("DnImg", Path.Combine( rf,"DnArrow.bmp"));
            LoadImg("RepeatImg", Path.Combine( rf,"RepeatIco.bmp"));
            LoadImg("CancelImg", Path.Combine( rf,"CancelIco.bmp"));
            LoadImg("DesComp", Path.Combine(rf, "DesignComponentIco.bmp"));
            LoadImg("DesShip", Path.Combine(rf, "DesignShipIco.bmp"));
            LoadImg("Research", Path.Combine(rf, "ResearchIco.bmp"));
            LoadImg("PowerImg", Path.Combine(rf, "PowerIco.bmp"));
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
            var SelectedSystem = StarSystemStates[activeSysID].StarSystem;
            PrimarySystemDateTime = SelectedSystem.ManagerSubpulses.StarSysDateTime;
            GalacticMap.SelectedStarSysGuid = activeSysID;
        }

        internal void EnableGameMaster()
        {
            StarSystemStates = new Dictionary<Guid, SystemState>();
            if(Game != null)
                foreach (var system in Game.Systems)
                {
                    StarSystemStates[system.Key] = SystemState.GetMasterState(system.Value);
                }
            GalacticMap.SetFaction();
        }

        internal void MapClicked(ECSLib.Vector3 worldCoord, MouseButtons button)
        {
            if (button == MouseButtons.Primary)
                LastWorldPointClicked = worldCoord;

            if (ActiveWindow != null)
                ActiveWindow.MapClicked(worldCoord, button);
        }
        internal void EntityClicked(Guid entityGuid, Guid starSys, MouseButtons button)
        {
        
            LastClickedEntity = StarSystemStates[starSys].EntityStatesWithNames[entityGuid];

            EntityClickedEvent?.Invoke(LastClickedEntity, button);

            if (ActiveWindow != null)
                ActiveWindow.EntityClicked(StarSystemStates[starSys].EntityStatesWithNames[entityGuid], button);
            OnEntitySelected();
        }

        void OnEntitySelected()
        {
            SelectedSysMapRender.SelectedEntityExtras = new List<IDrawData>();
            if(LastClickedEntity.DebugOrbitOrder != null)
            {
                SelectedSysMapRender.SelectedEntityExtras.Add(LastClickedEntity.DebugOrbitOrder);
            }
        }

    }


    public abstract class PulsarGuiWindow
    {
        protected ImGuiWindowFlags _flags = ImGuiWindowFlags.None;
        //internal bool IsLoaded;
        internal bool CanActive = false;
        internal bool IsActive = false;
        //internal int StateIndex = -1;
        //protected bool _IsOpen;
        internal static GlobalUIState _state;
        public void SetActive()
        {
            IsActive = true;
        }
       

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

        internal virtual void MapClicked(ECSLib.Vector3 worldPos, MouseButtons button) { }

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
