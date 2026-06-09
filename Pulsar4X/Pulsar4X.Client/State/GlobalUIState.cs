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
using Pulsar4X.Orbits;
using Pulsar4X.Client.Rendering;
using System.Collections.Concurrent;
using System.Threading;

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
        internal Engine.Game? Game { get; set; }
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
        internal GalacticMapRender? GalacticMap;
        internal SafeList<UpdateWindowState> UpdateableWindows = new();
        internal DateTime LastGameUpdateTime = new();
        internal StarSystem SelectedSystem => StarSystemStates[SelectedStarSystemId].StarSystem;
        internal SystemState SelectedSystemState => StarSystemStates[SelectedStarSystemId];
        internal DateTime SelectedSystemTime => StarSystemStates[SelectedStarSystemId].StarSystem.StarSysDateTime;
        internal DateTime SelectedSysLastUpdateTime = new();
        internal string SelectedStarSystemId { get; private set; }
        internal SystemMapRendering? SelectedSysMapRender => GalacticMap == null ? null : GalacticMap.SelectedSysMapRender;
        internal DateTime PrimarySystemDateTime;
        internal EntityContextMenu? ContextMenu { get; set; }
        internal SafeDictionary<string, SystemState> StarSystemStates = new();
        internal Camera Camera;
        internal SDL3Window ViewPort { get; private set; }

        internal Dictionary<Type, PulsarGuiWindow> LoadedWindows = new();
        internal Dictionary<String, NonUniquePulsarGuiWindow> LoadedNonUniqueWindows = new();
        internal PulsarGuiWindow? ActiveWindow { get; set; }
        internal List<List<UserOrbitSettings>> UserOrbitSettingsMtx = new();
        internal Dictionary<UserOrbitSettings.OrbitBodyType, float> DrawNameZoomLvl = new();
        internal Dictionary<string, IntPtr> SDLImageDictionary = new();
        internal Dictionary<string, int> GLImageDictionary = new();
        public event EntityClickedEventHandler? EntityClickedEvent;
        internal EntityState? LastClickedEntity = null;
        internal EntityState? PrimaryEntity { get; private set; }
        //internal SpaceMasterVM SpaceMasterVM;
        internal bool SMenabled = false;
        internal Dictionary<int, EntityWindow> EntityWindows { get; private set; } = new();
        private string _previousSystemIdBeforeSM = "";

        internal Stack<IHotKeyHandler> HotKeys { get; private set; } = new();

        // Maneuver node panel for orbit-click placement
        internal ManeuverNodePanel? ManeuverNodePanel { get; set; }
        private ManuverLinesComplete? _orbitClickManuverLines;

        // Click-vs-drag detection
        private float _mouseDownX;
        private float _mouseDownY;
        private const float DragThreshold = 5f;

        // Maneuver node dragging
        private bool _isDraggingNode = false;

        // Game Settings
        internal GameSettings GameSettings { get; set; }

        // TODO: Extract this to a helper class, along with SystemState buffer.
        // Double buffering events that are received from the engine.
        private class ChangeBuffer
        {
            public ConcurrentQueue<Message> RevealedSystems = new();
        }

        private ChangeBuffer _clientSide = new();
        private ChangeBuffer _serverSide = new();
        private readonly object _bufferSwapLock = new object();

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

            for (int i = 0; i < Utils.EnumEntries<UserOrbitSettings.OrbitBodyType>(); i++)
            {
                UserOrbitSettingsMtx.Add(new List<UserOrbitSettings>());
                for (int j = 0; j < Utils.EnumEntries<UserOrbitSettings.OrbitTrajectoryType>(); j++)
                {
                    UserOrbitSettingsMtx[i].Add(new UserOrbitSettings());
                }
            }

            // Stars: yellowish, ~120 degree tail
            foreach (var settings in UserOrbitSettingsMtx[(int)UserOrbitSettings.OrbitBodyType.Star])
            {
                settings.Red = 255;
                settings.Grn = 220;
                settings.Blu = 80;
                settings.EllipseSweepRadians = 2.09f; // ~120 degrees
            }

            // Planets/dwarf planets/moons: ~90 degree tail
            foreach (int bodyIdx in new[] {
                (int)UserOrbitSettings.OrbitBodyType.Planet,
                (int)UserOrbitSettings.OrbitBodyType.DwarfPlanet,
                (int)UserOrbitSettings.OrbitBodyType.Moon })
            {
                foreach (var settings in UserOrbitSettingsMtx[bodyIdx])
                {
                    settings.EllipseSweepRadians = 1.57f; // ~90 degrees
                }
            }

            // Asteroids: subtle dark gray, very short tail
            foreach (var settings in UserOrbitSettingsMtx[(int)UserOrbitSettings.OrbitBodyType.Asteroid])
            {
                settings.Red = 55;
                settings.Grn = 55;
                settings.Blu = 55;
                settings.MaxAlpha = 160;
                settings.GhostOrbitAlpha = 0;
                settings.EllipseSweepRadians = 0.26f; // ~15 degrees
            }

            // Ships: short tail, ghost orbit enabled
            foreach (var settings in UserOrbitSettingsMtx[(int)UserOrbitSettings.OrbitBodyType.Ship])
            {
                settings.EllipseSweepRadians = 0.26f; // ~15 degrees
                settings.GhostOrbitAlpha = 20;
            }

            // Comets: white-ish, very short tail
            foreach (var settings in UserOrbitSettingsMtx[(int)UserOrbitSettings.OrbitBodyType.Comet])
            {
                settings.Red = 200;
                settings.Grn = 210;
                settings.Blu = 220;
                settings.MaxAlpha = 160;
                settings.EllipseSweepRadians = 0.26f; // ~15 degrees
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
            mainWin.MouseButtonDownOccured += (object sender, SDL.Event e) =>
            {
                if (e.Button.Button == 1)
                {
                    _mouseDownX = e.Motion.X;
                    _mouseDownY = e.Motion.Y;

                    // Check if mouse is near a node marker — start dragging instead of panning
                    if (IsMouseNearNodeMarker((int)e.Motion.X, (int)e.Motion.Y))
                    {
                        _isDraggingNode = true;
                    }
                    else
                    {
                        Camera.IsGrabbingMap = true;
                        Camera.MouseFrameIncrementX = e.Motion.X;
                        Camera.MouseFrameIncrementY = e.Motion.Y;
                    }
                }
            };
            mainWin.MouseButtonUpOccured += (object sender, SDL.Event e) =>
            {
                if (e.Button.Button == 1)
                {
                    Camera.IsGrabbingMap = false;
                    bool wasDraggingNode = _isDraggingNode;
                    _isDraggingNode = false;

                    // Check if this was a drag (not a click)
                    float dx = e.Motion.X - _mouseDownX;
                    float dy = e.Motion.Y - _mouseDownY;
                    bool wasDrag = (dx * dx + dy * dy) > DragThreshold * DragThreshold;

                    if (wasDraggingNode)
                    {
                        // Node drag completed — final position was set in MouseMove
                        return;
                    }

                    if (!wasDrag)
                    {
                        // Try orbit-line click first for maneuver node placement
                        TryOrbitClick((int)e.Motion.X, (int)e.Motion.Y);
                    }
                }
            };
            mainWin.MouseWheelOccured += (object sender, SDL.Event e) =>
            {
                if (e.Wheel.Y > 0)
                    Camera.ZoomIn((int)e.Wheel.MouseX, (int)e.Wheel.MouseY);
                else if (e.Wheel.Y < 0)
                    Camera.ZoomOut((int)e.Wheel.MouseX, (int)e.Wheel.MouseY);
            };
            mainWin.MouseMoveOccured += (object sender, SDL.Event e) =>
            {
                if (_isDraggingNode)
                {
                    // Reposition the node along the orbit as the mouse moves
                    DragNodeToScreenPos((int)e.Motion.X, (int)e.Motion.Y);
                }
                else if (Camera.IsGrabbingMap)
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
            foreach (var window in LoadedWindows)
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
            ContextMenu = null;
            ActiveWindow = null;
        }

        /// <summary>
        /// Called every frame to update the UI state with the changes from the server.
        /// </summary>
        internal void Update()
        {
            lock (_bufferSwapLock)
            {
                (_clientSide, _serverSide) = (_serverSide, _clientSide);
            }

            // Handle all buffered events.
            while (_clientSide.RevealedSystems.TryDequeue(out var message))
            {
                if(Game is null || Faction is null)
                    throw new InvalidOperationException("Revealed systems require a game and faction to be set.");

                if (message.SystemId is null) continue;

                if (!StarSystemStates.ContainsKey(message.SystemId))
                {
                    var system = Game?.Systems.FirstOrDefault(s => s.ID.Equals(message.SystemId));
                    if (system == null)
                    {
                        Console.WriteLine($"ERROR: {message.SystemId} was revealed but not found in the game systems.");
                        continue;
                    }

                    StarSystemStates[message.SystemId] = new SystemState(system, Faction.Id);
                }

                OnStarSystemAdded?.Invoke(this, message.SystemId);
            }

            // Update the individual system states.
            foreach (var (_, systemState) in StarSystemStates)
            {
                systemState.Update();
            }

            // Update the galactic map.
            GalacticMap?.Update();
        }

        internal void SetFaction(Entity factionEntity, bool setAsPlayer = false)
        {
            if (Game == null) throw new NullReferenceException("Game is null");

            if (setAsPlayer)
                PlayerFaction = factionEntity;

            // Remove the old selected system's priority observer
            if (!string.IsNullOrEmpty(SelectedStarSystemId))
            {
                StarSystemStates[SelectedStarSystemId].StarSystem.DecrementExternalObserver(true);
            }

            Faction = factionEntity;
            FactionInfoDB factionInfo = factionEntity.GetDataBlob<FactionInfoDB>();
            StarSystemStates = new SafeDictionary<string, SystemState>();
            foreach (var guid in factionInfo.KnownSystems)
            {
                var system = Game.Systems.FirstOrDefault(s => s.ID.Equals(guid));
                if (system == null) continue;

                StarSystemStates[guid] = new SystemState(system, factionEntity.Id);

                // Notify that the currently selected system is on focus.
                if (!string.IsNullOrEmpty(SelectedStarSystemId) && SelectedStarSystemId.Equals(guid))
                {
                    system.IncrementExternalObserver(true);
                }
            }

            // Unsubscribe to any previous message listeners
            MessagePublisher.Instance.Unsubscribe(MessageTypes.StarSystemRevealed, OnSystemRevealed);

            // Subscribe to new listeners with current faction
            MessagePublisher.Instance.Subscribe(MessageTypes.StarSystemRevealed, OnSystemRevealed, msg => msg.FactionId == Faction.Id);

            OnFactionChanged?.Invoke(this);
        }

        internal Task OnSystemRevealed(Message message)
        {
            if (message.SystemId is null)
                return Task.CompletedTask;

            lock (_bufferSwapLock)
            {
                _serverSide.RevealedSystems.Enqueue(message);
            }
            return Task.CompletedTask;
        }

        internal void SetActiveSystem(string activeSysID, bool refresh = false)
        {
            if (Game == null || Faction == null) throw new NullReferenceException("Game or Faction is null");

            if (!activeSysID.Equals(SelectedStarSystemId) || refresh)
            {
                // Demote the old system from Foreground to Background
                if (!string.IsNullOrEmpty(SelectedStarSystemId) && StarSystemStates.ContainsKey(SelectedStarSystemId))
                {
                    var oldSystem = StarSystemStates[SelectedStarSystemId].StarSystem;

                    oldSystem.DecrementExternalObserver(true);

                    StarSystemStates[SelectedStarSystemId].SavedCameraState = Camera.SaveState();
                }

                if (!StarSystemStates.ContainsKey(activeSysID))
                {
                    var newSys = new SystemState(Game.Systems.First(s => s.ID.Equals(activeSysID)), Faction.Id);
                    StarSystemStates[activeSysID] = newSys;
                }

                // Promote the new system to Foreground
                var newSystem = Game.Systems.First(s => s.ID.Equals(activeSysID));
                newSystem.IncrementExternalObserver(true);

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
            if (Game == null) throw new NullReferenceException("Game is null");
            SMenabled = true;
            // Store the current system ID before switching to GameMaster
            _previousSystemIdBeforeSM = SelectedStarSystemId;
            SetFaction(Game.GameMasterFaction);
        }

        internal void DisableGameMaster()
        {
            if (PlayerFaction == null) throw new NullReferenceException("PlayerFaction is null");
            SMenabled = false;
            SetFaction(PlayerFaction);

            // Restore the previous system if the player has access to it
            if (!string.IsNullOrEmpty(_previousSystemIdBeforeSM) && StarSystemStates.ContainsKey(_previousSystemIdBeforeSM))
            {
                SetActiveSystem(_previousSystemIdBeforeSM);
            }
            else if (StarSystemStates.Count > 0)
            {
                // If the previous system is not available, switch to the first known system
                SetActiveSystem(StarSystemStates.Keys.First());
            }
        }

        internal void ToggleGameMaster()
        {
            SMenabled = !SMenabled;
            if (SMenabled)
                EnableGameMaster();
            else
                DisableGameMaster();
        }

        /// <summary>
        /// Attempts to place a maneuver node where the user clicked on a ship's orbit line.
        /// Returns true if the click was consumed (a node was placed), false to fall through
        /// to normal entity selection.
        /// </summary>
        private bool TryOrbitClick(int screenX, int screenY)
        {
            // Only works when a ship with thrust capability is selected
            if (PrimaryEntity == null)
                return false;

            if (!PrimaryEntity.Entity.TryGetDataBlob<NewtonThrustAbilityDB>(out _))
                return false;

            if (!PrimaryEntity.Entity.HasDataBlob<OrbitDB>())
                return false;

            // Check if user clicked on the existing editing node marker (to re-select it)
            if (_orbitClickManuverLines != null && _orbitClickManuverLines.EditingNodeScreenPositions.Length > 0)
            {
                for (int i = 0; i < _orbitClickManuverLines.EditingNodeScreenPositions.Length; i++)
                {
                    var np = _orbitClickManuverLines.EditingNodeScreenPositions[i];
                    float dx = screenX - np.X;
                    float dy = screenY - np.Y;
                    if (dx * dx + dy * dy < 15 * 15)
                    {
                        // Re-open panel for this existing node
                        if (ManeuverNodePanel == null || !ManeuverNodePanel.IsActive)
                        {
                            ManeuverNodePanel = new ManeuverNodePanel(
                                this,
                                PrimaryEntity.Entity,
                                _orbitClickManuverLines,
                                _orbitClickManuverLines.EditingNodes[i]);
                        }
                        return true;
                    }
                }
            }

            // If the panel is already open, clicking elsewhere on the orbit moves the node
            if (ManeuverNodePanel != null && ManeuverNodePanel.IsActive)
            {
                var orbitIconForMove = PrimaryEntity.OrbitIcon as OrbitIconBase;
                if (orbitIconForMove == null)
                    return true; // consume click anyway

                var mousePtForMove = new SDL.Point() { X = screenX, Y = screenY };
                var (segIdx, ta) = orbitIconForMove.HitTest(mousePtForMove);
                if (segIdx >= 0)
                {
                    // Clicked on orbit: reposition the node
                    var nodeDateTime = TrueAnomalyToDateTime(ta);
                    if (nodeDateTime.HasValue)
                        ManeuverNodePanel.RepositionNode(nodeDateTime.Value);
                    return true;
                }
                // Clicked off the orbit: close the panel
                ManeuverNodePanel.ClosePanel();
                return false; // let MapClicked handle it
            }

            // Get the orbit icon for the selected entity
            var orbitIcon = PrimaryEntity.OrbitIcon as OrbitIconBase;
            if (orbitIcon == null)
                return false;

            // Hit test the orbit line
            var mousePoint = new SDL.Point() { X = screenX, Y = screenY };
            var (segmentIndex, trueAnomaly) = orbitIcon.HitTest(mousePoint);
            if (segmentIndex < 0)
                return false;

            // Calculate the DateTime at this orbit position
            var nodeTime = TrueAnomalyToDateTime(trueAnomaly);
            if (!nodeTime.HasValue)
                return false;

            // Clean up any previous maneuver lines
            CleanupManeuverNode();

            // Create maneuver lines and node
            _orbitClickManuverLines = new ManuverLinesComplete();
            var soiParentPosition = MoveMath.GetSOIParentPositionDB(PrimaryEntity.Entity);
            if (soiParentPosition == null)
                return false;

            _orbitClickManuverLines.RootSequence.ParentPosition = soiParentPosition;
            _orbitClickManuverLines.AddNewEditNode(PrimaryEntity.Entity, nodeTime.Value);

            // Add to render extras
            if (SelectedSysMapRender != null)
            {
                if (!SelectedSysMapRender.SelectedEntityExtras.Contains(_orbitClickManuverLines))
                    SelectedSysMapRender.SelectedEntityExtras.Add(_orbitClickManuverLines);
            }

            // Create and show the panel
            ManeuverNodePanel = new ManeuverNodePanel(
                this,
                PrimaryEntity.Entity,
                _orbitClickManuverLines,
                _orbitClickManuverLines.EditingNodes[0]);

            return true;
        }

        /// <summary>
        /// Converts a true anomaly on the primary entity's orbit to a future DateTime.
        /// Uses Kepler's equation (true anomaly → eccentric anomaly → mean anomaly)
        /// for correct results on eccentric orbits.
        /// </summary>
        private DateTime? TrueAnomalyToDateTime(double trueAnomaly)
        {
            if (PrimaryEntity == null || !PrimaryEntity.Entity.HasDataBlob<OrbitDB>())
                return null;

            var orbitDB = PrimaryEntity.Entity.GetDataBlob<OrbitDB>();
            var period = orbitDB.OrbitalPeriod.TotalSeconds;
            var eccentricity = orbitDB.Eccentricity;
            var currentTime = PrimaryEntity.Entity.StarSysDateTime;

            // Convert both true anomalies to mean anomalies via eccentric anomaly (Kepler's equation)
            var currentTrueAnomaly = OrbitMath.GetTrueAnomaly(orbitDB, currentTime);

            var currentE = OrbitMath.GetEccentricAnomalyFromTrueAnomaly(currentTrueAnomaly, eccentricity);
            var currentM = currentE - eccentricity * Math.Sin(currentE);

            var targetE = OrbitMath.GetEccentricAnomalyFromTrueAnomaly(trueAnomaly, eccentricity);
            var targetM = targetE - eccentricity * Math.Sin(targetE);

            // Mean anomaly progresses linearly with time
            var meanAnomalyDiff = targetM - currentM;
            if (meanAnomalyDiff < 0) meanAnomalyDiff += Math.PI * 2;

            var timeFraction = meanAnomalyDiff / (Math.PI * 2);
            var nodeDateTime = currentTime + TimeSpan.FromSeconds(period * timeFraction);

            if (nodeDateTime <= currentTime)
                nodeDateTime += TimeSpan.FromSeconds(period);

            return nodeDateTime;
        }

        /// <summary>
        /// Checks if a screen position is near an editing node marker.
        /// Used to decide whether to start a node drag or a map pan.
        /// </summary>
        private bool IsMouseNearNodeMarker(int screenX, int screenY)
        {
            if (_orbitClickManuverLines == null)
                return false;

            for (int i = 0; i < _orbitClickManuverLines.EditingNodeScreenPositions.Length; i++)
            {
                var np = _orbitClickManuverLines.EditingNodeScreenPositions[i];
                float dx = screenX - np.X;
                float dy = screenY - np.Y;
                if (dx * dx + dy * dy < 15 * 15)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Repositions the editing node to the closest point on the orbit to the given screen position.
        /// Called during mouse drag.
        /// </summary>
        private void DragNodeToScreenPos(int screenX, int screenY)
        {
            if (PrimaryEntity == null || ManeuverNodePanel == null || !ManeuverNodePanel.IsActive)
                return;

            var orbitIcon = PrimaryEntity.OrbitIcon as OrbitIconBase;
            if (orbitIcon == null)
                return;

            var mousePoint = new SDL.Point() { X = screenX, Y = screenY };
            var (segmentIndex, trueAnomaly) = orbitIcon.HitTest(mousePoint, 50f); // wider threshold during drag
            if (segmentIndex < 0)
                return;

            var nodeDateTime = TrueAnomalyToDateTime(trueAnomaly);
            if (nodeDateTime.HasValue)
                ManeuverNodePanel.RepositionNode(nodeDateTime.Value);
        }

        /// <summary>
        /// Removes previous maneuver node visuals from the render list.
        /// </summary>
        private void CleanupManeuverNode()
        {
            if (_orbitClickManuverLines != null && SelectedSysMapRender != null)
            {
                SelectedSysMapRender.SelectedEntityExtras.Remove(_orbitClickManuverLines);
            }
            ManeuverNodePanel = null;
            _orbitClickManuverLines = null;
        }

        /// <summary>
        /// Opens a ManeuverNodePanel for editing an existing NewtonThrustCommand.
        /// Sets up the maneuver lines, node, and panel with the command's values.
        /// </summary>
        internal void OpenManeuverPanelForOrder(Entity entity, NewtonThrustCommand command)
        {
            // Don't open if the order is already running
            if (command.IsRunning)
                return;

            // Need an orbit to place the node on
            if (!entity.HasDataBlob<OrbitDB>())
                return;

            // Clean up any previous maneuver node UI
            CleanupManeuverNode();

            // Create maneuver lines and node at the command's burn center time
            _orbitClickManuverLines = new ManuverLinesComplete();
            var soiParentPosition = MoveMath.GetSOIParentPositionDB(entity);
            if (soiParentPosition == null)
                return;

            _orbitClickManuverLines.RootSequence.ParentPosition = soiParentPosition;
            _orbitClickManuverLines.AddNewEditNode(entity, command.NodeDateTime);

            // Set the node's delta-v from the command (X=radial, Y=prograde)
            var node = _orbitClickManuverLines.EditingNodes[0];
            float prograde = (float)command.OrbitrelativeDeltaV.Y;
            float radial = (float)command.OrbitrelativeDeltaV.X;
            if (prograde != 0 || radial != 0)
            {
                node.SetNode(prograde, radial, 0, command.NodeDateTime);
            }

            // Add to render extras
            if (SelectedSysMapRender != null)
            {
                if (!SelectedSysMapRender.SelectedEntityExtras.Contains(_orbitClickManuverLines))
                    SelectedSysMapRender.SelectedEntityExtras.Add(_orbitClickManuverLines);
            }

            // Create panel in edit mode
            ManeuverNodePanel = new ManeuverNodePanel(
                this,
                entity,
                _orbitClickManuverLines,
                node,
                command);
        }

        /// <summary>
        /// Called during the ImGui render pass to display the active ManeuverNodePanel.
        /// </summary>
        internal void DisplayManeuverNodePanel()
        {
            if (ManeuverNodePanel != null)
            {
                if (ManeuverNodePanel.IsActive)
                {
                    ManeuverNodePanel.Display();
                    _orbitClickManuverLines?.DrawApsisLabels();
                    _orbitClickManuverLines?.DrawEncounterLabels();
                }
                else
                {
                    CleanupManeuverNode();
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
            if (SelectedSysMapRender == null) throw new NullReferenceException("SelectedSysMapRender is null");

            var entityState = StarSystemStates[starSys].EntityStatesWithNames[entityGuid];
            LastClickedEntity = entityState;

            ActiveWindow?.EntityClicked(entityState, button);

            SelectedSysMapRender.SelectedEntityExtras = new List<IDrawData>();
            if (LastClickedEntity.DebugOrbitOrder != null)
            {
                SelectedSysMapRender.SelectedEntityExtras.Add(LastClickedEntity.DebugOrbitOrder);
            }

            if (LastClickedEntity.TryGetDataBlob(out NavSequenceDB? navDB))
            {
                ManuverNodesDraw2 nodeDraw = new ManuverNodesDraw2(LastClickedEntity);
                SelectedSysMapRender.SelectedEntityExtras.Add(nodeDraw);
            }

            if (ActiveWindow == null || ActiveWindow.GetActive() == false || ActiveWindow.ClickedEntityIsPrimary)
                PrimaryEntity = LastClickedEntity;

            EntityClickedEvent?.Invoke(LastClickedEntity, button);

            if (button == MouseButtons.Primary)
            {
                if (!EntityWindows.ContainsKey(entityGuid))
                {
                    EntityWindows.Add(entityGuid, new EntityWindow(entityState));
                }
                EntityWindows[entityGuid].ToggleActive();

                if (!ViewPort.IsCtrlPressed)
                {
                    foreach (var (id, window) in EntityWindows)
                    {
                        if (id == entityGuid) continue;

                        window.SetActive(false);
                    }
                }
            }
        }

        internal void EntityClicked(EntityState entityState, MouseButtons button)
        {
            if (entityState.StarSystemId == null) throw new NullReferenceException("StarSystemId is null");
            EntityClicked(entityState.Id, entityState.StarSystemId, button);
        }
    }

}
