using ImGuiNET;
using Pulsar4X.Orbital;
using SDL3;
using System;
using System.Collections.Generic;
using Pulsar4X.Engine;
using System.Linq;
using Pulsar4X.Input;
using Pulsar4X.Messaging;
using System.Threading.Tasks;
using Pulsar4X.DataStructures;
using static Pulsar4X.Client.SystemViewPreferences;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Movement;
using Pulsar4X.Client.Rendering;

namespace Pulsar4X.Client
{
    public delegate void EntityClickedEventHandler(EntityState entityState, MouseButtons mouseButton);

    public delegate void FactionChangedEventHandler(GlobalUIState uIState);
    public delegate void StarSystemChangedEventHandler(GlobalUIState uIState);
    public delegate void StarSystemAddedEventHandler(GlobalUIState uiState, string systemId);

    public class GlobalUIState
    {
        public event FactionChangedEventHandler? OnFactionChanged;
        public event StarSystemChangedEventHandler? OnStarSystemChanged;
        public event StarSystemAddedEventHandler? OnStarSystemAdded;

        public bool debugnewgame = false;
        //internal PulsarGuiWindow distanceRulerWindow { get; set; }
        internal static readonly Dictionary<Type, string> NamesForMenus = new() {
            {typeof(PinCameraBlankMenuHelper), "Pin camera"},
            {typeof(WarpOrderWindow), "Warp to a new orbit"},
            {typeof(ChangeCurrentOrbitWindow), "Change current orbit"},
            {typeof(FireControl), "Fire Control" },
            {typeof(RenameWindow), "Rename"},
            {typeof(CargoTransferWindow), "Cargo"},
            {typeof(ColonyLogisticsDisplay), "Logistics"},
            {typeof(LogiShipWindow), "Logistics"},
            {typeof(ColonyPanel), "Economy"},
            {typeof(GotoSystemBlankMenuHelper), "Go to system"},
            {typeof(SelectPrimaryBlankMenuHelper), "Select as primary"},
            {typeof(PlanetaryWindow), "Planetary window"},
            {typeof(NavWindow), "Nav Window"},
            {typeof(OrdersListWindow), "Orders Window"},
            {typeof(OrderCreationWindow), "Order Creation"}
        };
        internal Engine.Game? Game { get; set;}
        internal bool IsGameLoaded { get { return Game != null; } }
        internal Entity? Faction { get; set; }

        /// <summary>
        /// Gets the faction bit mask for the current faction.
        /// Use this with Masked&lt;T&gt;.For() to retrieve faction-visible data.
        /// Returns 0 if no faction is set.
        /// </summary>
        internal int FactionMask => Faction?.GetDataBlob<FactionInfoDB>().FactionMask ?? 0;

        /// <summary>
        /// The player running this clients faction
        /// </summary>
        internal Entity? PlayerFaction { get; set; }
        internal bool ShowMetrixWindow;
        internal bool ShowImgDbg;
        internal bool ShowDemoWindow;
        internal bool ShowDamageWindow;
        internal IntPtr SDLRendererPtr { get; private set; }
        internal int _lastContextMenuOpenedEntityGuid = -1;
        internal GalacticMapRender? GalacticMap;
        internal SafeList<UpdateWindowState> UpdateableWindows = new ();
        internal DateTime LastGameUpdateTime = new ();
        internal DateTime LastZoomTime = DateTime.MinValue;
        internal bool IsRecentlyZoomed => (DateTime.Now - LastZoomTime).TotalMilliseconds < 150;
        internal StarSystem SelectedSystem => StarSystemStates[SelectedStarSystemId].StarSystem;
        internal SystemState SelectedSystemState => StarSystemStates[SelectedStarSystemId];
        internal DateTime SelectedSystemTime => StarSystemStates[SelectedStarSystemId].StarSystem.StarSysDateTime;
        internal DateTime SelectedSysLastUpdateTime = new ();
        internal string SelectedStarSystemId { get; private set; }
        internal SystemMapRendering? SelectedSysMapRender => GalacticMap == null ? null : GalacticMap.SelectedSysMapRender;
        internal DateTime PrimarySystemDateTime;
        internal EntityContextMenu? ContextMenu { get; set; }
        internal SafeDictionary<string, SystemState> StarSystemStates = new ();
        internal Camera Camera;
        internal SDL3Window ViewPort { get; private set; }

        internal Dictionary<Type, PulsarGuiWindow> LoadedWindows = new ();
        internal Dictionary<String, NonUniquePulsarGuiWindow> LoadedNonUniqueWindows = new ();
        internal PulsarGuiWindow? ActiveWindow { get; set; }
        internal List<List<UserOrbitSettings>> UserOrbitSettingsMtx = new ();
        internal Dictionary<UserOrbitSettings.OrbitBodyType, float> DrawNameZoomLvl = new ();
        internal Dictionary<string, IntPtr> SDLImageDictionary = new ();
        internal Dictionary<string, int> GLImageDictionary = new ();
        public event EntityClickedEventHandler? EntityClickedEvent;
        internal EntityState? LastClickedEntity = null;
        internal EntityState? PrimaryEntity { get; private set; }
        //internal SpaceMasterVM SpaceMasterVM;
        internal bool SMenabled = false;
        internal Dictionary<int, EntityWindow> EntityWindows { get; private set; } = new();
        private string _previousSystemIdBeforeSM = "";

        internal Stack<IHotKeyHandler> HotKeys { get; private set; } = new ();

        internal View? SelectedMapView { get; set; } = null;

        /// <summary>
        /// True when the mouse is hovering over a map overlay (like name icons) that should
        /// allow scroll wheel input to pass through to the map for zooming.
        /// This is set during rendering and checked during the next frame's event handling.
        /// </summary>
        internal bool IsMouseOverMapOverlay = false;
        
        // Game Settings
        internal GameSettings GameSettings { get; set; }

        internal GlobalUIState(SDL3Window viewport)
        {
            ViewPort = viewport;
            PulsarGuiWindow._uiState = this;
            var windowPtr = viewport.Window;

            SDLRendererPtr = SDL.CreateRenderer(windowPtr, "pulsar4x");
            
            // Load game settings
            GameSettings = GameSettings.Load();

            DrawNameZoomLvl.Add(UserOrbitSettings.OrbitBodyType.Star, 2f);
            DrawNameZoomLvl.Add(UserOrbitSettings.OrbitBodyType.Planet, 32f);
            DrawNameZoomLvl.Add(UserOrbitSettings.OrbitBodyType.DwarfPlanet, 64f);
            DrawNameZoomLvl.Add(UserOrbitSettings.OrbitBodyType.Moon, 96f);
            DrawNameZoomLvl.Add(UserOrbitSettings.OrbitBodyType.Asteroid, 96f);
            DrawNameZoomLvl.Add(UserOrbitSettings.OrbitBodyType.Comet, 96f);
            DrawNameZoomLvl.Add(UserOrbitSettings.OrbitBodyType.Colony, 32f);
            DrawNameZoomLvl.Add(UserOrbitSettings.OrbitBodyType.Ship, 64f);
            DrawNameZoomLvl.Add(UserOrbitSettings.OrbitBodyType.Unknown, 16f);

            for (int i = 0; i < (int)UserOrbitSettings.OrbitBodyType.NumberOf; i++)
            {
                UserOrbitSettingsMtx.Add(new List<UserOrbitSettings>());
                for (int j = 0; j < (int)UserOrbitSettings.OrbitTrajectoryType.NumberOf; j++)
                {
                    UserOrbitSettingsMtx[i].Add(new UserOrbitSettings());
                }
            }

            HotKeys.Push(HotKeyFactory.CreateDefault());

            Camera = new Camera(viewport);

            MainMenuItems.GetInstance().SetActive();

            SelectedStarSystemId = "";

            // Need to pre load all textures
            this.Img_Cancel();
            this.Img_Cargo();
            this.Img_DesComponent();
            this.Img_DesignOrdnance();
            this.Img_DesignShip();
            this.Img_Discord();
            this.Img_Down();
            this.Img_Firecon();
            this.Img_GalaxyMap();
            this.Img_Industry();
            this.Img_Logo();
            this.Img_MainMenuLogo();
            this.Img_OneStep();
            this.Img_Pause();
            this.Img_Pin();
            this.Img_Play();
            this.Img_Power();
            this.Img_Rename();
            this.Img_Repeat();
            this.Img_Research();
            this.Img_Ruler();
            this.Img_Select();
            this.Img_Tree();
            this.Img_Up();

            var mainWin = (PulsarMainWindow)ViewPort;
            mainWin.MouseButtonDownOccured += (object sender, SDL.Event e) => {
                OnFocusMoved();

                if (e.Button.Button == 1)
                {
                    Camera.IsGrabbingMap = true;
                    Camera.MouseFrameIncrementX = e.Motion.X;
                    Camera.MouseFrameIncrementY = e.Motion.Y;
                }
            };
            mainWin.MouseButtonUpOccured += (object sender, SDL.Event e) => {
                OnFocusMoved();

                if (e.Button.Button == 1)
                {
                    Camera.IsGrabbingMap = false;
                    MapClicked(Camera.WorldCoordinate_m(e.Motion.X, e.Motion.Y), MouseButtons.Primary);
                }
                else if (e.Button.Button == 3)
                {
                    MapClicked(Camera.WorldCoordinate_m(e.Motion.X, e.Motion.Y), MouseButtons.Alt);
                }
            };
            mainWin.MouseWheelOccured += (object sender, SDL.Event e) => {
                OnFocusMoved();
                LastZoomTime = DateTime.Now;

                if (e.Wheel.Y > 0)
                    Camera.ZoomIn((int)e.Wheel.MouseX, (int)e.Wheel.MouseY);
                else if (e.Wheel.Y < 0)
                    Camera.ZoomOut((int)e.Wheel.MouseX, (int)e.Wheel.MouseY);
            };
            mainWin.MouseMoveOccured += (object sender, SDL.Event e) => {
                if (Camera.IsGrabbingMap)
                {
                    Camera.WorldOffset_m(
                            (int)(Camera.MouseFrameIncrementX - e.Motion.X),
                            (int)(Camera.MouseFrameIncrementY - e.Motion.Y));
                    Camera.MouseFrameIncrementX = e.Motion.X;
                    Camera.MouseFrameIncrementY = e.Motion.Y;
                }
            };
        }

        private void DeactivateAllClosableWindows()
        {
            foreach(var window in LoadedWindows)
            {
                window.Value.SetActive(false);
            }
        }

        /// <summary>
        /// Clears all cached UI state to prepare for a new game.
        /// This must be called before setting up a new game to prevent stale
        /// references from the previous game
        /// </summary>
        internal void ClearGameState()
        {
            LoadedWindows.Clear();
            LoadedNonUniqueWindows.Clear();
            EntityWindows.Clear();
            StarSystemStates.Clear();
            LastClickedEntity = null;
            PrimaryEntity = null;
            Faction = null;
            PlayerFaction = null;
            SelectedStarSystemId = "";
            _lastContextMenuOpenedEntityGuid = -1;
            ContextMenu = null;
            ActiveWindow = null;
        }

        internal void SetFaction(Entity factionEntity, bool setAsPlayer = false)
        {
            if(Game == null) throw new NullReferenceException("Game is null");

            if(setAsPlayer)
                PlayerFaction = factionEntity;

            Faction = factionEntity;
            FactionInfoDB factionInfo = factionEntity.GetDataBlob<FactionInfoDB>();
            StarSystemStates = new SafeDictionary<string, SystemState>();
            foreach (var guid in factionInfo.KnownSystems)
            {
                var system = Game.Systems.FirstOrDefault(s => s.ID.Equals(guid));
                if(system == null) continue;
                StarSystemStates[guid] = new SystemState(system, factionEntity.Id);
            }

            // Unsubscribe to any previous message listeners
            MessagePublisher.Instance.Unsubscribe(MessageTypes.StarSystemRevealed, OnSystemRevealed);

            // Subscribe to new listeners with current faction
            MessagePublisher.Instance.Subscribe(MessageTypes.StarSystemRevealed, OnSystemRevealed, msg => msg.FactionId == Faction.Id);

            OnFactionChanged?.Invoke(this);
        }

        internal async Task OnSystemRevealed(Message message)
        {
            if(Game == null || Faction == null) throw new NullReferenceException("Game or Faction is null");

            await Task.Run(() => {
                if(message.SystemId != null)
                {
                    if(!StarSystemStates.ContainsKey(message.SystemId)){
                        var system = Game.Systems.FirstOrDefault(s => s.ID.Equals(message.SystemId));
                        if(system == null)
                        {
                            Console.WriteLine($"ERROR: {message.SystemId} was revealed but not found in the game systems.");
                            return;
                        }
                        StarSystemStates[message.SystemId] = new SystemState(system, Faction.Id);
                    }
                    OnStarSystemAdded?.Invoke(this, message.SystemId);
                }
            });
        }

        internal void SetActiveSystem(string activeSysID, bool refresh = false)
        {
            if(Game == null || Faction == null) throw new NullReferenceException("Game or Faction is null");

            if(!activeSysID.Equals(SelectedStarSystemId) || refresh){
                // Save camera state for the outgoing system
                if(!string.IsNullOrEmpty(SelectedStarSystemId) && StarSystemStates.ContainsKey(SelectedStarSystemId))
                {
                    StarSystemStates[SelectedStarSystemId].SavedCameraState = Camera.SaveState();
                }

                if(!StarSystemStates.ContainsKey(activeSysID)){
                    StarSystemStates[activeSysID] = new SystemState(Game.Systems.First(s => s.ID.Equals(activeSysID)), Faction.Id);
                }

                SelectedStarSystemId = activeSysID;

                var selectedSystemState = StarSystemStates[activeSysID];
                var SelectedSys = selectedSystemState.StarSystem;
                PrimarySystemDateTime = SelectedSys.ManagerSubpulses.StarSysDateTime;
                LastClickedEntity = null;
                PrimaryEntity = null;

                // Restore camera state from the incoming system
                if (selectedSystemState.SavedCameraState.HasValue)
                {
                    Camera.RestoreState(selectedSystemState.SavedCameraState.Value, SelectedSys);
                }
                else
                {
                    // First visit: center on primary star at default zoom
                    Camera.PinToEntity(null);
                    Camera.ZoomLevel = 200;
                    var starEntity = SelectedSys.GetFirstEntityWithDataBlob<StarInfoDB>();
                    if (starEntity != null)
                        Camera.CenterOnEntity(starEntity);
                    else
                        Camera._camWorldPos_m = new Orbital.Vector3();
                }

                OnStarSystemChanged?.Invoke(this);
            }

        }

        internal void EnableGameMaster()
        {
            if(Game == null) throw new NullReferenceException("Game is null");
            SMenabled = true;
            // Store the current system ID before switching to GameMaster
            _previousSystemIdBeforeSM = SelectedStarSystemId;
            SetFaction(Game.GameMasterFaction);
        }

        internal void DisableGameMaster()
        {
            if(PlayerFaction == null) throw new NullReferenceException("PlayerFaction is null");
            SMenabled = false;
            SetFaction(PlayerFaction);

            // Restore the previous system if the player has access to it
            if(!string.IsNullOrEmpty(_previousSystemIdBeforeSM) && StarSystemStates.ContainsKey(_previousSystemIdBeforeSM))
            {
                SetActiveSystem(_previousSystemIdBeforeSM);
            }
            else if(StarSystemStates.Count > 0)
            {
                // If the previous system is not available, switch to the first known system
                SetActiveSystem(StarSystemStates.Keys.First());
            }
        }

        internal void ToggleGameMaster()
        {
            SMenabled = !SMenabled;
            if(SMenabled)
                EnableGameMaster();
            else
                DisableGameMaster();
        }

        //checks wether any event changed the mouse position after a new mouse click, indicating the user is doing something else with the mouse as he was doing before.
        internal void OnFocusMoved()
        {
            _lastContextMenuOpenedEntityGuid = -1;
        }

        //checks wether the planet icon is clicked
        internal void MapClicked(Orbital.Vector3 worldCoord, MouseButtons button)
        {
            SafeDictionary<int, EntityState> allEntities = new ();
            if(StarSystemStates.ContainsKey(SelectedStarSystemId))
                allEntities = StarSystemStates[SelectedStarSystemId].EntityStatesWithNames;

            //gets all entities with a position on the map
            double closestEntityDistInM = double.MaxValue;
            EntityState? closestEntity = null;
            //iterates over entities. Compares the next one with the previous closest-to-click one, if next one is closer, set that one as the closest, repeat for all entities.
            if(allEntities != null)
            {
                foreach(var oneEntityState in allEntities)
                {
                    var oneEntity = oneEntityState.Value;
                    if(oneEntity.HasDataBlob<PositionDB>()){
                        var thisDistanceInM = Math.Sqrt(Math.Pow(oneEntity.GetDataBlob<PositionDB>().AbsolutePosition.X-worldCoord.X, 2) + Math.Pow(oneEntity.GetDataBlob<PositionDB>().AbsolutePosition.Y -worldCoord.Y,2));
                        if(thisDistanceInM <= closestEntityDistInM)
                        {
                            closestEntityDistInM = thisDistanceInM;
                            closestEntity = oneEntity;
                        }
                    }
                }
            }

            //checks if there is a closest entity
            if(closestEntity != null)
            {
                if(closestEntity.HasDataBlob<MassVolumeDB>())
                {
                    int minPixelRadius = 20;

                    //var distanceBetweenMouseAndEntity = Math.Sqrt(Math.Pow(closestEntity.GetDataBlob<PositionDB>().AbsolutePosition_m - worldCoord,2) + Math.Pow(entityPositionInScreenPixels.Y- mousePosInPixels.Y,2));
                    //int distComp = (int)Math.Sqrt(Math.Pow(50,2)/2);

                    if(closestEntityDistInM <= closestEntity.GetDataBlob<MassVolumeDB>().RadiusInM || Camera.WorldDistance_AU(minPixelRadius) >=  Distance.MToAU(closestEntityDistInM)){
                        EntityClicked(closestEntity.Id, SelectedStarSystemId, button);

                        if(button == MouseButtons.Alt){
                            _lastContextMenuOpenedEntityGuid = closestEntity.Id;
                        }
                    }
                }
            }
        }

        internal void EntitySelectedAsPrimary(int entityGuid, string starSys)
        {
            PrimaryEntity = StarSystemStates[starSys].EntityStatesWithNames[entityGuid];
            ActiveWindow?.EntitySelectedAsPrimary(PrimaryEntity);
        }

        internal void EntityClicked(int entityGuid, string starSys, MouseButtons button)
        {
            if(SelectedSysMapRender == null) throw new NullReferenceException("SelectedSysMapRender is null");

            var entityState = StarSystemStates[starSys].EntityStatesWithNames[entityGuid];
            LastClickedEntity = entityState;

            ActiveWindow?.EntityClicked(entityState, button);

            SelectedSysMapRender.SelectedEntityExtras = new List<IDrawData>();
            if(LastClickedEntity.DebugOrbitOrder != null)
            {
                SelectedSysMapRender.SelectedEntityExtras.Add(LastClickedEntity.DebugOrbitOrder);
            }

            if (LastClickedEntity.TryGetDataBlob(out NavSequenceDB? navDB))
            {
                ManuverNodesDraw2 nodeDraw = new ManuverNodesDraw2(LastClickedEntity);
                SelectedSysMapRender.SelectedEntityExtras.Add(nodeDraw);
            }

            if(ActiveWindow == null || ActiveWindow.GetActive() == false || ActiveWindow.ClickedEntityIsPrimary)
                PrimaryEntity = LastClickedEntity;

            EntityClickedEvent?.Invoke(LastClickedEntity, button);

            if(button == MouseButtons.Primary)
            {
                if(!EntityWindows.ContainsKey(entityGuid))
                {
                    EntityWindows.Add(entityGuid, new EntityWindow(entityState));
                }
                EntityWindows[entityGuid].ToggleActive();

                if(!ViewPort.IsCtrlPressed)
                {
                    foreach(var (id, window) in EntityWindows)
                    {
                        if(id == entityGuid) continue;

                        window.SetActive(false);
                    }
                }
            }
        }

        internal void EntityClicked(EntityState entityState, MouseButtons button)
        {
            if(entityState.StarSystemId == null) throw new NullReferenceException("StarSystemId is null");
            EntityClicked(entityState.Id, entityState.StarSystemId, button);
        }
    }

}
