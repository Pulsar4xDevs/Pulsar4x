using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.ECSLib;
using SDL2;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.IO;
using System.Runtime.CompilerServices;
using Pulsar4X.ECSLib.ComponentFeatureSets.Damage;
using Pulsar4X.ImGuiNetUI;
using Pulsar4X.ImGuiNetUI.EntityManagement;
using Pulsar4X.SDL2UI;

namespace Pulsar4X.SDL2UI

    
{
    

    public delegate void EntityClickedEventHandler(EntityState entityState, MouseButtons mouseButton);
    public class GlobalUIState
    {
        public bool debugnewgame = true;
        //internal PulsarGuiWindow distanceRulerWindow { get; set; }
        internal static readonly Dictionary<Type, string> namesForMenus = new Dictionary<Type, string>{
            {typeof(PinCameraBlankMenuHelper), "Pin camera"},
            {typeof(WarpOrderWindow), "Warp to a new orbit"},
            {typeof(ChangeCurrentOrbitWindow), "Change current orbit"},
            {typeof(FireControl), "Fire Control" },
            {typeof(RenameWindow), "Rename"},
            {typeof(CargoTransfer), "Cargo"},
            {typeof(ColonyPanel), "Economy"},
            {typeof(GotoSystemBlankMenuHelper), "Go to system"},
            {typeof(SelectPrimaryBlankMenuHelper), "Select as primary"},
            {typeof(PlanetaryWindow), "Planetary window"},
            {typeof(NavWindow), "Nav Window"}
        };
        internal Game Game;
        internal FactionVM FactionUIState;
        internal bool IsGameLoaded { get { return Game != null; } }
        internal Entity Faction { get { return FactionUIState.FactionEntity; } }
        internal bool ShowMetrixWindow;
        internal bool ShowImgDbg;
        internal bool ShowDemoWindow;
        internal bool ShowDamageWindow;
        internal IntPtr rendererPtr;
        internal Guid _lastContextMenuOpenedEntityGuid = Guid.Empty;

        internal GalacticMapRender GalacticMap;

        internal List<UpdateWindowState> UpdateableWindows = new List<UpdateWindowState>();
        internal DateTime LastGameUpdateTime = new DateTime();
        internal StarSystem SelectedSystem { get { return StarSystemStates[SelectedStarSysGuid].StarSystem; } }
        internal DateTime SelectedSystemTime { get { return StarSystemStates[SelectedStarSysGuid].StarSystem.StarSysDateTime; } }
        internal DateTime SelectedSysLastUpdateTime = new DateTime();
        internal Guid SelectedStarSysGuid { get { return GalacticMap.SelectedStarSysGuid; } }
        internal SystemMapRendering SelectedSysMapRender { get { return GalacticMap.SelectedSysMapRender; } }
        internal DateTime PrimarySystemDateTime; //= new DateTime();

        internal EntityContextMenu ContextMenu { get; set; }

        internal Dictionary<Guid, SystemState> StarSystemStates = new Dictionary<Guid, SystemState>();

        internal Camera Camera;// = new Camera();
        internal ImGuiSDL2CSWindow ViewPort;
        internal System.Numerics.Vector2 MainWinSize { get {return ViewPort.Size;}}

        internal Dictionary<Type, PulsarGuiWindow> LoadedWindows = new Dictionary<Type, PulsarGuiWindow>();
        internal Dictionary<String, NonUniquePulsarGuiWindow> LoadedNonUniqueWindows = new Dictionary<String, NonUniquePulsarGuiWindow>();
        internal PulsarGuiWindow ActiveWindow { get; set; }

        internal List<List<UserOrbitSettings>> UserOrbitSettingsMtx = new List<List<UserOrbitSettings>>();
        internal List<float> DrawNameZoomLvl = new List<float>();

        internal Dictionary<string, IntPtr> SDLImageDictionary = new Dictionary<string, IntPtr>();
        internal Dictionary<string, int> GLImageDictionary = new Dictionary<string, int>();

        public event EntityClickedEventHandler EntityClickedEvent;
        
        internal EntityState LastClickedEntity = null;
        
        internal EntityState PrimaryEntity { get; private set; }
        internal ECSLib.Vector3 LastWorldPointClicked_m { get; set; }



        internal SpaceMasterVM SpaceMasterVM;
        internal bool SMenabled = false;

        internal GlobalUIState(ImGuiSDL2CSWindow viewport)
        {
            ViewPort = viewport;
            PulsarGuiWindow._uiState = this;
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

            MainMenuItems.GetInstance().SetActive();

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
            LoadImg("DesOrd", Path.Combine(rf, "DesignOrdnanceIco.bmp"));
            LoadImg("GalMap", Path.Combine(rf, "GalaxyMapIco.bmp"));
            LoadImg("Research", Path.Combine(rf, "ResearchIco.bmp"));
            LoadImg("Power", Path.Combine(rf, "PowerIco.bmp"));
            LoadImg("Ruler", Path.Combine(rf, "RulerIco.bmp"));
            LoadImg("Cargo", Path.Combine(rf, "CargoIco.bmp"));
            LoadImg("Firecon", Path.Combine(rf, "FireconIco.bmp"));
            LoadImg("Industry", Path.Combine(rf, "IndustryIco.bmp"));
            LoadImg("Pin", Path.Combine(rf, "PinIco.bmp"));
            LoadImg("Rename", Path.Combine(rf, "RenameIco.bmp"));
            LoadImg("Select", Path.Combine(rf, "SelectIco.bmp"));
            LoadImg("Tree", Path.Combine(rf, "TreeIco.bmp"));

            //DEBUG Code: (can be deleted);
            DamageTools.LoadFromBitMap(Path.Combine(rf, "ImgTest.bmp"));
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

        private void deactivateAllClosableWindows(){
            foreach(var window in LoadedWindows){
                window.Value.SetActive(false);
            }
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

        internal void SetActiveSystem(Guid activeSysID, bool refresh = false)
        {
            if(activeSysID != SelectedStarSysGuid || refresh){
                deactivateAllClosableWindows();
                var SelectedSys = StarSystemStates[activeSysID].StarSystem;
                PrimarySystemDateTime = SelectedSys.ManagerSubpulses.StarSysDateTime;
                GalacticMap.SelectedStarSysGuid = activeSysID;
                DebugWindow.GetInstance().systemState = StarSystemStates[activeSysID];
                LastClickedEntity = null;
                PrimaryEntity = null;
            }
            
        }

        internal void refreshStarSystemStates(){
            SetFaction(Faction);
            SetActiveSystem(SelectedStarSysGuid ,true);
        }

        internal void EnableGameMaster()
        {
            SMenabled = true;
            StarSystemStates = new Dictionary<Guid, SystemState>();
            if(Game != null)
                foreach (var system in Game.Systems)
                {
                    StarSystemStates[system.Key] = SystemState.GetMasterState(system.Value);
                }
            GalacticMap.SetFaction();
        }

        internal void ToggleGameMaster()
        {
            if(SMenabled == true) 
            {
                SMenabled = false;
            }
            else
            {
                SMenabled = true;
                EnableGameMaster();
            }
        }

        //checks wether any event changed the mouse position after a new mouse click, indicating the user is doing something else with the mouse as he was doing before.
        internal void onFocusMoved(){
            _lastContextMenuOpenedEntityGuid = Guid.Empty;
        }

        //checks wether the planet icon is clicked
        internal void MapClicked(ECSLib.Vector3 worldCoord, MouseButtons button)
        {

            

            if (button == MouseButtons.Primary)
                LastWorldPointClicked_m = worldCoord;

            if (ActiveWindow != null)
                ActiveWindow.MapClicked(worldCoord, button);

            if (LoadedWindows.ContainsKey(typeof(DistanceRuler)))
                LoadedWindows[typeof(DistanceRuler)].MapClicked(worldCoord, button);

            Dictionary<Guid, EntityState> allEntities = null;
            if(StarSystemStates.ContainsKey(SelectedStarSysGuid)){
                allEntities = StarSystemStates[SelectedStarSysGuid].EntityStatesWithNames;
            }
            //gets all entities with a position on the map
            double closestEntityDistInM = double.MaxValue;
            Entity closestEntity = null;
            //iterates over entities. Compares the next one with the previous closest-to-click one, if next one is closer, set that one as the closest, repeat for all entities.
            if(allEntities != null){
            
                foreach(var oneEntityState in allEntities){
                    var oneEntity = oneEntityState.Value.Entity;
                    if(oneEntity.HasDataBlob<PositionDB>()){
                        var thisDistanceInM = Math.Sqrt(Math.Pow(oneEntity.GetDataBlob<PositionDB>().AbsolutePosition_m.X-worldCoord.X, 2) + Math.Pow(oneEntity.GetDataBlob<PositionDB>().AbsolutePosition_m.Y -worldCoord.Y,2));
                        if(thisDistanceInM <= closestEntityDistInM){
                            
                            closestEntityDistInM = thisDistanceInM;
                            closestEntity = oneEntity;
                        
                        
                        }
                    }
                
                }
            }


                
            //checks if there is a closest entity
            if(closestEntity != null){
                if(closestEntity.HasDataBlob<MassVolumeDB>()){
                    int minPixelRadius = 20;
                        
                        
                    //var distanceBetweenMouseAndEntity = Math.Sqrt(Math.Pow(closestEntity.GetDataBlob<PositionDB>().AbsolutePosition_m - worldCoord,2) + Math.Pow(entityPositionInScreenPixels.Y- mousePosInPixels.Y,2));
                    //int distComp = (int)Math.Sqrt(Math.Pow(50,2)/2);

                    if(closestEntityDistInM <= closestEntity.GetDataBlob<MassVolumeDB>().RadiusInM || Camera.WorldDistance_AU(minPixelRadius) >=  Distance.MToAU(closestEntityDistInM)){
                        ImGui.Begin("--crash fixer--(this menu`s whole purpose is preventing a ImGui global state related game crash)");
                           
                        EntityClicked(closestEntity.Guid, SelectedStarSysGuid, button);
                        ImGui.End();
                            
                        if(button == MouseButtons.Alt){
                            _lastContextMenuOpenedEntityGuid = closestEntity.Guid;
                        }
                            
                    }
                }
            }
                

            if (LoadedWindows.ContainsKey(typeof(ToolBarUI)))
                LoadedWindows[typeof(ToolBarUI)].MapClicked(worldCoord, button);
            
            
        }

        internal void EntitySelectedAsPrimary(Guid entityGuid, Guid starSys){
            PrimaryEntity = StarSystemStates[starSys].EntityStatesWithNames[entityGuid];
            if(ActiveWindow != null)
                ActiveWindow.EntitySelectedAsPrimary(PrimaryEntity);
        }

        internal void EntityClicked(Guid entityGuid, Guid starSys, MouseButtons button)
        {
            
            LastClickedEntity = StarSystemStates[starSys].EntityStatesWithNames[entityGuid];

            EntityClickedEvent?.Invoke(LastClickedEntity, button);

            if (ActiveWindow != null)
                ActiveWindow.EntityClicked(StarSystemStates[starSys].EntityStatesWithNames[entityGuid], button);

            SelectedSysMapRender.SelectedEntityExtras = new List<IDrawData>();
            if(LastClickedEntity.DebugOrbitOrder != null)
            {
                SelectedSysMapRender.SelectedEntityExtras.Add(LastClickedEntity.DebugOrbitOrder);
            }

            if(ActiveWindow == null || ActiveWindow.ClickedEntityIsPrimary)
                PrimaryEntity = LastClickedEntity;

        }
    }

    public abstract class UpdateWindowState
    {
        internal static GlobalUIState _uiState;
        public abstract bool GetActive();
        
        public abstract void OnGameTickChange(DateTime newDate);
        public abstract void OnSystemTickChange(DateTime newDate);

        public abstract void OnSelectedSystemChange(StarSystem newStarSys);

        protected UpdateWindowState()
        {
            _uiState.UpdateableWindows.Add(this);
        }

        public void Deconstructor()
        {
            _uiState.UpdateableWindows.Remove(this);
        }

    }

    public abstract class PulsarGuiWindow : UpdateWindowState
    {
        protected ImGuiWindowFlags _flags = ImGuiWindowFlags.None;
        //internal bool IsLoaded;
        internal bool CanActive = false;
        protected bool IsActive = false;
        //internal int StateIndex = -1;
        //protected bool _IsOpen;
        public bool ClickedEntityIsPrimary = true;
        
        public void SetActive(bool ActiveVal = true)
        {
            IsActive = ActiveVal;
        }
        public void ToggleActive()
        {
            IsActive = !IsActive;
        }
        public override bool GetActive()
        {
            return IsActive;
        }


        protected PulsarGuiWindow()
        {
            _uiState.LoadedWindows[this.GetType()] = this;
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

        internal virtual void EntitySelectedAsPrimary(EntityState entity){ }

        internal virtual void MapClicked(ECSLib.Vector3 worldPos_m, MouseButtons button) { }

        public override void OnGameTickChange(DateTime newDate)
        {
        }
        
        public override void OnSystemTickChange(DateTime newDate)
        {
        }
        
        public override void OnSelectedSystemChange(StarSystem newStarSys)
        {
        }
    }

    public abstract class NonUniquePulsarGuiWindow
    {
        protected ImGuiWindowFlags _flags = ImGuiWindowFlags.None;
        internal bool CanActive = false;
        internal bool IsActive = false;
        internal string UniqueName = "test";
        internal static GlobalUIState _state;

        public void SetActive(bool ActiveVal = true)
        {
            IsActive = ActiveVal;
        }
        public void ToggleActive()
        {
            IsActive = !IsActive;
        }
        public bool GetActive()
        {
            return IsActive;
        }

        public void SetName(string Newname)
        {
            UniqueName = Newname;
        }

        public void StartDisplay()
        {
            _state.LoadedNonUniqueWindows[this.UniqueName] = this;
        }


        protected NonUniquePulsarGuiWindow()
        {
                
        }


        internal abstract void Display();

        internal virtual void EntityClicked(EntityState entity, MouseButtons button) { }

        internal virtual void EntitySelectedAsPrimary(EntityState entity) { }

        internal virtual void MapClicked(ECSLib.Vector3 worldPos_m, MouseButtons button) { }

        internal void Destroy()
        {
        }

    }

}
