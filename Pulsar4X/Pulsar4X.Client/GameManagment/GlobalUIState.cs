using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.Orbital;
using SDL2;
using System;
using System.Collections.Generic;
using System.IO;
using Pulsar4X.ImGuiNetUI;
using Pulsar4X.ImGuiNetUI.EntityManagement;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Damage;
using Pulsar4X.Datablobs;
using System.Linq;
using Pulsar4X.Input;
using System.Net.Http;
using Pulsar4X.Messaging;
using System.Threading.Tasks;
using Pulsar4X.DataStructures;

namespace Pulsar4X.SDL2UI
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

        public bool debugnewgame = true;
        //internal PulsarGuiWindow distanceRulerWindow { get; set; }
        internal static readonly Dictionary<Type, string> NamesForMenus = new() {
            {typeof(PinCameraBlankMenuHelper), "Pin camera"},
            {typeof(WarpOrderWindow), "Warp to a new orbit"},
            {typeof(ChangeCurrentOrbitWindow), "Change current orbit"},
            {typeof(FireControl), "Fire Control" },
            {typeof(RenameWindow), "Rename"},
            {typeof(CargoTransfer), "Cargo"},
            {typeof(ColonyLogisticsDisplay), "Logistics"},
            {typeof(LogiShipWindow), "Logistics"},
            {typeof(ColonyPanel), "Economy"},
            {typeof(GotoSystemBlankMenuHelper), "Go to system"},
            {typeof(SelectPrimaryBlankMenuHelper), "Select as primary"},
            {typeof(PlanetaryWindow), "Planetary window"},
            {typeof(NavWindow), "Nav Window"},
            {typeof(OrdersListUI), "Orders Window"},
            {typeof(OrderCreationUI), "Order Creation"}
        };
        internal Engine.Game Game;
        //internal FactionVM FactionUIState;
        internal bool IsGameLoaded { get { return Game != null; } }
        internal Entity Faction { get; set; }

        /// <summary>
        /// The player running this clients faction
        /// </summary>
        internal Entity PlayerFaction { get; set; }
        internal bool ShowMetrixWindow;
        internal bool ShowImgDbg;
        internal bool ShowDemoWindow;
        internal bool ShowDamageWindow;
        internal IntPtr rendererPtr;
        internal int _lastContextMenuOpenedEntityGuid = -1;
        internal GalacticMapRender GalacticMap;
        internal SafeList<UpdateWindowState> UpdateableWindows = new ();
        internal DateTime LastGameUpdateTime = new ();
        internal StarSystem SelectedSystem { get { return StarSystemStates[SelectedStarSysGuid].StarSystem; } }
        internal DateTime SelectedSystemTime { get { return StarSystemStates[SelectedStarSysGuid].StarSystem.StarSysDateTime; } }
        internal DateTime SelectedSysLastUpdateTime = new ();
        internal string SelectedStarSysGuid { get; private set; }
        internal SystemMapRendering? SelectedSysMapRender { get { return GalacticMap.SelectedSysMapRender; } }
        internal DateTime PrimarySystemDateTime;
        internal EntityContextMenu ContextMenu { get; set; }
        internal SafeDictionary<string, SystemState> StarSystemStates = new ();
        internal Camera Camera;
        internal ImGuiSDL2CSWindow ViewPort;
        internal System.Numerics.Vector2 MainWinSize { get {return ViewPort.Size;}}

        internal Dictionary<Type, PulsarGuiWindow> LoadedWindows = new ();
        internal Dictionary<String, NonUniquePulsarGuiWindow> LoadedNonUniqueWindows = new ();
        internal PulsarGuiWindow ActiveWindow { get; set; }
        internal List<List<UserOrbitSettings>> UserOrbitSettingsMtx = new ();
        internal Dictionary<UserOrbitSettings.OrbitBodyType, float> DrawNameZoomLvl = new ();
        internal Dictionary<string, IntPtr> SDLImageDictionary = new ();
        internal Dictionary<string, int> GLImageDictionary = new ();
        public event EntityClickedEventHandler EntityClickedEvent;
        internal EntityState? LastClickedEntity = null;
        internal EntityState PrimaryEntity { get; private set; }
        internal Orbital.Vector3 LastWorldPointClicked_m { get; set; }
        //internal SpaceMasterVM SpaceMasterVM;
        internal bool SMenabled = false;
        internal Dictionary<int, EntityWindow> EntityWindows { get; private set; } = new();

        internal Stack<IHotKeyHandler> HotKeys { get; private set; } = new ();

        internal GlobalUIState(ImGuiSDL2CSWindow viewport)
        {
            ViewPort = viewport;
            PulsarGuiWindow._uiState = this;
            var windowPtr = viewport.Handle;
            SDL.SDL_SetHint(SDL.SDL_HINT_RENDER_DRIVER, "opengl");
            //var surfacePtr = SDL.SDL_GetWindowSurface(windowPtr);
            rendererPtr = SDL.SDL_CreateRenderer(windowPtr, -1, SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED);

            DrawNameZoomLvl.Add(UserOrbitSettings.OrbitBodyType.Star, 2f);
            DrawNameZoomLvl.Add(UserOrbitSettings.OrbitBodyType.Planet, 32f);
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

            //DEBUG Code: (can be deleted);
            DamageTools.LoadFromBitMap(Path.Combine("Resources", "ImgTest.bmp"));
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

        private void DeactivateAllClosableWindows()
        {
            foreach(var window in LoadedWindows)
            {
                window.Value.SetActive(false);
            }
        }

        internal void SetFaction(Entity factionEntity, bool setAsPlayer = false)
        {
            if(setAsPlayer)
                PlayerFaction = factionEntity;

            Faction = factionEntity;
            FactionInfoDB factionInfo = factionEntity.GetDataBlob<FactionInfoDB>();
            StarSystemStates = new SafeDictionary<string, SystemState>();
            foreach (var guid in factionInfo.KnownSystems)
            {
                var system = Game.Systems.First(s => s.Guid.Equals(guid));
                StarSystemStates[guid] = new SystemState(system, factionEntity);
            }

            // Unsubscribe to any previous message listeners
            MessagePublisher.Instance.Unsubscribe(MessageTypes.StarSystemRevealed, OnSystemRevealed);

            // Subscribe to new listeners with current faction
            MessagePublisher.Instance.Subscribe(MessageTypes.StarSystemRevealed, OnSystemRevealed, msg => msg.FactionId == Faction.Id);

            OnFactionChanged?.Invoke(this);
        }

        internal async Task OnSystemRevealed(Message message)
        {
            await Task.Run(() => {
                if(message.SystemId != null)
                {
                    if(!StarSystemStates.ContainsKey(message.SystemId)){
                        StarSystemStates[message.SystemId] = new SystemState(Game.Systems.First(s => s.Guid.Equals(message.SystemId)), Faction);
                    }
                    OnStarSystemAdded?.Invoke(this, message.SystemId);
                }
            });
        }

        internal void SetActiveSystem(string activeSysID, bool refresh = false)
        {
            if(!activeSysID.Equals(SelectedStarSysGuid) || refresh){
                if(!StarSystemStates.ContainsKey(activeSysID)){
                    StarSystemStates[activeSysID] = new SystemState(Game.Systems.First(s => s.Guid.Equals(activeSysID)), Faction);
                }

                SelectedStarSysGuid = activeSysID;

                var SelectedSys = StarSystemStates[activeSysID].StarSystem;
                PrimarySystemDateTime = SelectedSys.ManagerSubpulses.StarSysDateTime;
                DebugWindow.GetInstance().SystemState = StarSystemStates[activeSysID]; // Fix this, should be handled internally in the window
                LastClickedEntity = null;
                PrimaryEntity = null;

                OnStarSystemChanged?.Invoke(this);
            }

        }

        internal void RefreshStarSystemStates()
        {
            SetFaction(Faction);
            SetActiveSystem(SelectedStarSysGuid, true);
        }

        internal void EnableGameMaster()
        {
            SMenabled = true;
            SetFaction(Game.GameMasterFaction);
        }

        internal void DisableGameMaster()
        {
            SMenabled = false;
            SetFaction(PlayerFaction);
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
            if (button == MouseButtons.Primary)
                LastWorldPointClicked_m = worldCoord;

            ActiveWindow?.MapClicked(worldCoord, button);

            if (LoadedWindows.ContainsKey(typeof(DistanceRuler)))
                LoadedWindows[typeof(DistanceRuler)].MapClicked(worldCoord, button);

            SafeDictionary<int, EntityState> allEntities = null;
            if(StarSystemStates.ContainsKey(SelectedStarSysGuid))
                allEntities = StarSystemStates[SelectedStarSysGuid].EntityStatesWithNames;

            //gets all entities with a position on the map
            double closestEntityDistInM = double.MaxValue;
            Entity closestEntity = null;
            //iterates over entities. Compares the next one with the previous closest-to-click one, if next one is closer, set that one as the closest, repeat for all entities.
            if(allEntities != null)
            {
                foreach(var oneEntityState in allEntities)
                {
                    var oneEntity = oneEntityState.Value.Entity;
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
                        ImGui.Begin("--crash fixer--(this menu`s whole purpose is preventing a ImGui global state related game crash)");

                        EntityClicked(closestEntity.Id, SelectedStarSysGuid, button);
                        ImGui.End();

                        if(button == MouseButtons.Alt){
                            _lastContextMenuOpenedEntityGuid = closestEntity.Id;
                        }
                    }
                }
            }

            if (LoadedWindows.ContainsKey(typeof(ToolBarWindow)))
                LoadedWindows[typeof(ToolBarWindow)].MapClicked(worldCoord, button);
        }

        internal void EntitySelectedAsPrimary(int entityGuid, string starSys)
        {
            PrimaryEntity = StarSystemStates[starSys].EntityStatesWithNames[entityGuid];
            ActiveWindow?.EntitySelectedAsPrimary(PrimaryEntity);
        }

        internal void EntityClicked(int entityGuid, string starSys, MouseButtons button)
        {
            var entityState = StarSystemStates[starSys].EntityStatesWithNames[entityGuid];
            LastClickedEntity = entityState;

            ActiveWindow?.EntityClicked(entityState, button);

            SelectedSysMapRender.SelectedEntityExtras = new List<IDrawData>();
            if(LastClickedEntity.DebugOrbitOrder != null)
            {
                SelectedSysMapRender.SelectedEntityExtras.Add(LastClickedEntity.DebugOrbitOrder);
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

                if(!ImGui.GetIO().KeyCtrl)
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
            EntityClicked(entityState.Entity.Id, entityState.StarSysGuid, button);
        }
    }

}
