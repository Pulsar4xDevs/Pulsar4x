using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.ECSLib;
using Pulsar4X.Orbital;
using SDL2;
using System;
using System.Collections.Generic;
using System.IO;
using Pulsar4X.ECSLib.ComponentFeatureSets.Damage;
using Pulsar4X.ImGuiNetUI;
using Pulsar4X.ImGuiNetUI.EntityManagement;

namespace Pulsar4X.SDL2UI
{
    public delegate void EntityClickedEventHandler(EntityState entityState, MouseButtons mouseButton);
    public class GlobalUIState
    {
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
        internal Game Game;
        //internal FactionVM FactionUIState;
        internal bool IsGameLoaded { get { return Game != null; } }
        internal Entity Faction { get; set; }
        internal bool ShowMetrixWindow;
        internal bool ShowImgDbg;
        internal bool ShowDemoWindow;
        internal bool ShowDamageWindow;
        internal IntPtr rendererPtr;
        internal Guid _lastContextMenuOpenedEntityGuid = Guid.Empty;
        internal GalacticMapRender GalacticMap;
        internal List<UpdateWindowState> UpdateableWindows = new ();
        internal DateTime LastGameUpdateTime = new ();
        internal StarSystem SelectedSystem { get { return StarSystemStates[SelectedStarSysGuid].StarSystem; } }
        internal DateTime SelectedSystemTime { get { return StarSystemStates[SelectedStarSysGuid].StarSystem.StarSysDateTime; } }
        internal DateTime SelectedSysLastUpdateTime = new ();
        internal Guid SelectedStarSysGuid { get { return GalacticMap.SelectedStarSysGuid; } }
        internal SystemMapRendering SelectedSysMapRender { get { return GalacticMap.SelectedSysMapRender; } }
        internal DateTime PrimarySystemDateTime;
        internal EntityContextMenu ContextMenu { get; set; }
        internal Dictionary<Guid, SystemState> StarSystemStates = new ();
        internal Camera Camera;
        internal ImGuiSDL2CSWindow ViewPort;
        internal System.Numerics.Vector2 MainWinSize { get {return ViewPort.Size;}}

        internal Dictionary<Type, PulsarGuiWindow> LoadedWindows = new ();
        internal Dictionary<String, NonUniquePulsarGuiWindow> LoadedNonUniqueWindows = new ();
        internal PulsarGuiWindow ActiveWindow { get; set; }
        internal List<List<UserOrbitSettings>> UserOrbitSettingsMtx = new ();
        internal List<float> DrawNameZoomLvl = new ();
        internal Dictionary<string, IntPtr> SDLImageDictionary = new ();
        internal Dictionary<string, int> GLImageDictionary = new ();
        public event EntityClickedEventHandler EntityClickedEvent;
        internal EntityState LastClickedEntity = null;
        internal EntityState PrimaryEntity { get; private set; }
        internal Orbital.Vector3 LastWorldPointClicked_m { get; set; }
        internal SpaceMasterVM SpaceMasterVM;
        internal bool SMenabled = false;
        internal Dictionary<Guid, EntityWindow> EntityWindows { get; private set; } = new();

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

        internal void SetFaction(Entity factionEntity)
        {
            Faction = factionEntity;
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
                DeactivateAllClosableWindows();
                var SelectedSys = StarSystemStates[activeSysID].StarSystem;
                PrimarySystemDateTime = SelectedSys.ManagerSubpulses.StarSysDateTime;
                GalacticMap.SelectedStarSysGuid = activeSysID;
                DebugWindow.GetInstance().systemState = StarSystemStates[activeSysID];
                LastClickedEntity = null;
                PrimaryEntity = null;
            }

        }

        internal void RefreshStarSystemStates()
        {
            SetFaction(Faction);
            SetActiveSystem(SelectedStarSysGuid ,true);
        }

        internal void EnableGameMaster()
        {
            SMenabled = true;
            StarSystemStates = new Dictionary<Guid, SystemState>();
            var masterFaction = StaticRefLib.Game.GameMasterFaction;
            SetFaction(masterFaction);
            if(Game != null)
            {
                foreach (var system in Game.Systems)
                {
                    StarSystemStates[system.Key] = SystemState.GetMasterState(system.Value);
                }
            }
        }

        internal void ToggleGameMaster()
        {
            SMenabled = !SMenabled;
            if(SMenabled)
                EnableGameMaster();
        }

        //checks wether any event changed the mouse position after a new mouse click, indicating the user is doing something else with the mouse as he was doing before.
        internal void OnFocusMoved()
        {
            _lastContextMenuOpenedEntityGuid = Guid.Empty;
        }

        //checks wether the planet icon is clicked
        internal void MapClicked(Orbital.Vector3 worldCoord, MouseButtons button)
        {
            if (button == MouseButtons.Primary)
                LastWorldPointClicked_m = worldCoord;

            ActiveWindow?.MapClicked(worldCoord, button);

            if (LoadedWindows.ContainsKey(typeof(DistanceRuler)))
                LoadedWindows[typeof(DistanceRuler)].MapClicked(worldCoord, button);

            Dictionary<Guid, EntityState> allEntities = null;
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

                        EntityClicked(closestEntity.Guid, SelectedStarSysGuid, button);
                        ImGui.End();

                        if(button == MouseButtons.Alt){
                            _lastContextMenuOpenedEntityGuid = closestEntity.Guid;
                        }
                    }
                }
            }

            if (LoadedWindows.ContainsKey(typeof(ToolBarWindow)))
                LoadedWindows[typeof(ToolBarWindow)].MapClicked(worldCoord, button);
        }

        internal void EntitySelectedAsPrimary(Guid entityGuid, Guid starSys)
        {
            PrimaryEntity = StarSystemStates[starSys].EntityStatesWithNames[entityGuid];
            ActiveWindow?.EntitySelectedAsPrimary(PrimaryEntity);
        }

        internal void EntityClicked(Guid entityGuid, Guid starSys, MouseButtons button)
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
        }
    }

}
