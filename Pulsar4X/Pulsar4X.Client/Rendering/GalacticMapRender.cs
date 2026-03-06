using System;
using System.Collections.Generic;
using SDL3;
using ImGuiNET;
using Pulsar4X.DataStructures;
using Pulsar4X.Names;
using Pulsar4X.Orbits;
using Pulsar4X.Galaxy;
using Pulsar4X.Movement;
using Pulsar4X.Engine;
using Pulsar4X.Orbital;
using Pulsar4X.Client.Rendering;
using Pulsar4X.Client.Interface;
using Pulsar4X.Client.Interface.Widgets;

namespace Pulsar4X.Client
{
    public class GalacticMapRender
    {
        GlobalUIState _state;
        List<SystemState> SystemStates = new List<SystemState>();
        SafeDictionary<string, SystemMapRendering> RenderedMaps = new ();
        Dictionary<string, StarIcon> StarIcons = new ();
        Dictionary<string, string> _galMapLabels = new ();
        SDL3Window _window;
        internal string? CapitolSysMap { get; set; }
        internal string SelectedStarSysGuid { get { return _state.SelectedStarSystemId; } }
        internal SystemMapRendering? SelectedSysMapRender
        {
            get
            {
                return SelectedStarSysGuid == null ? null : RenderedMaps[SelectedStarSysGuid];
            }
        }
        Camera _camera;

        CollisionGrid grid;

        public GalacticMapRender(SDL3Window window, GlobalUIState state)
        {
            _state = state;
            _window = window;
            _camera = state.Camera;

            var size = window.Size;
            int cellSize = 16;
            int gridwid = (int)size.Width / cellSize;
            int gridhig = (int)size.Height / cellSize;
            grid = new CollisionGrid(gridwid, gridhig, cellSize);
            _state.EntityClickedEvent += _state_EntityClickedEvent;
            _state.OnFactionChanged += SetFaction;
            _state.OnStarSystemAdded += OnSystemAdded;
        }

        internal void SetFaction(GlobalUIState uIState)
        {
            if (_state.Game == null) return;

            int i = 0;
            double startangle = 0;
            float angleIncrease = (float)Math.Max(0.78539816339, 6.28318530718 / Math.Max(1, _state.Game.Systems.Count));
            int startR = 200;
            int radInc = 5;

            foreach (var starSystem in _state.Game.Systems)
            {
                var starSystemId = starSystem.ID;
                var x = (startR + radInc * i) * Math.Sin(startangle - angleIncrease * i);
                var y = (startR + radInc * i) * Math.Cos(startangle - angleIncrease * i);

                bool isKnown = _state.StarSystemStates.ContainsKey(starSystemId);

                // Create SystemMapRendering only for known systems
                if (isKnown)
                {
                    var systemState = _state.StarSystemStates[starSystemId];
                    if(!RenderedMaps.ContainsKey(starSystemId))
                    {
                        SystemMapRendering map = new SystemMapRendering(_window, _state);
                        map.Initialize(systemState.StarSystem);
                        RenderedMaps[starSystemId] = map;
                        map.GalacticMapPosition.X = x;
                        map.GalacticMapPosition.Y = y;
                    }
                    RenderedMaps[starSystemId].UpdateSystemState(systemState);
                }

                //TODO: handle binary/multiple star systems better.
                var starEntity = starSystem.GetFirstEntityWithDataBlob<StarInfoDB>();
                var orbitdb = starEntity.GetDataBlob<OrbitDB>();
                starEntity = orbitdb.Root; //just incase it's a binary system and the entity we got was not the primary

                if(!starEntity.TryGetDataBlob<StarInfoDB>(out var starInfoDB))
                {
                    throw new NullReferenceException("Star must have a StarInfoDB");
                }

                if(!starEntity.TryGetDataBlob<PositionDB>(out var starPositionDB))
                {
                    throw new NullReferenceException("Star must have a PositionDB");
                }

                if(!starEntity.TryGetDataBlob<MassVolumeDB>(out var starMassVolumeDB))
                {
                    throw new NullReferenceException("Star must have a MassVolumeDB");
                }

                var starIcon = new StarIcon(starInfoDB, starPositionDB, starMassVolumeDB);
                StarIcons[starSystemId] = starIcon;

                // Treat galactic layout values as AU and convert to meters
                var posAU = new Orbital.Vector3(x, y, 0);
                starIcon.WorldPosition_m = Distance.AuToMt(posAU);

                // Store galmap label: actual name for known systems, "??" for unknown
                if (isKnown)
                {
                    var factionId = _state.Faction?.Id ?? Game.NeutralFactionId;
                    _galMapLabels[starSystemId] = starSystem.NameDB.GetName(factionId);
                }
                else
                {
                    _galMapLabels[starSystemId] = "??";
                }

                i++;
            }
        }

        void OnSystemAdded(GlobalUIState globalUIState, string systemId)
        {
            if (!_state.StarSystemStates.ContainsKey(systemId)) return;

            var systemState = _state.StarSystemStates[systemId];
            SystemMapRendering map = new SystemMapRendering(_window, _state);
            map.Initialize(systemState.StarSystem);
            RenderedMaps[systemId] = map;

            // Update galmap label from "??" to actual name
            var factionId = _state.Faction?.Id ?? Game.NeutralFactionId;
            _galMapLabels[systemId] = systemState.StarSystem.NameDB.GetName(factionId);
        }

        void _state_EntityClickedEvent(EntityState entityState, MouseButtons mouseButton)
        {
            var sysGuid = entityState.StarSystemId;
            if(!string.IsNullOrEmpty(sysGuid) && SelectedStarSysGuid != sysGuid && RenderedMaps.ContainsKey(sysGuid))
            {
                _state.SetActiveSystem(sysGuid);
            }

        }

        internal void DrawNameIcons()
        {
            var zoomlvl = _state.Camera.ZoomLevel;
            if (zoomlvl < 0.99)
            {
                // Draw galmap labels for all systems
                foreach (var (systemId, label) in _galMapLabels)
                {
                    if (!StarIcons.TryGetValue(systemId, out var starIcon))
                        continue;

                    var screenPos = starIcon.ViewScreenPos;
                    if (!_camera.IsOnScreen(screenPos.X, screenPos.Y))
                        continue;

                    bool isKnown = _state.StarSystemStates.ContainsKey(systemId);
                    var textColor = isKnown
                        ? new System.Numerics.Vector4(1f, 1f, 1f, 1f)
                        : new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 0.7f);

                    ImGui.PushStyleColor(ImGuiCol.WindowBg, Styles.InvisibleColor);
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(1, 2));
                    float textHeight = ImGui.GetTextLineHeight() + 4; // 4 for window padding
                    ImGui.SetNextWindowPos(new System.Numerics.Vector2(screenPos.X + 20, screenPos.Y - textHeight * 0.5f), ImGuiCond.Always);

                    bool isActive = true;
                    Window.Begin("galLabel##" + systemId, ref isActive,
                        ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize |
                        ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoBringToFrontOnFocus |
                        ImGuiWindowFlags.NoScrollWithMouse);

                    ImGui.PushStyleColor(ImGuiCol.Text, textColor);
                    ImGui.TextUnformatted(label);
                    ImGui.PopStyleColor();

                    Window.End();
                    ImGui.PopStyleColor();
                    ImGui.PopStyleVar(2);
                }
            }
        }

        internal void Update()
        {
            foreach(var (id, system) in RenderedMaps)
            {
                system.Update();
            }
        }

        internal void Draw()
        {
            // Save the current render state & turn on blend mode
            RenderState savedRenderState = _window.GetRenderState();
            _window.SetBlendMode(SDL.BlendMode.Blend);

            // Draw the appropriate map
            var matrix = _camera.GetZoomMatrix();
            var zoomlvl = _state.Camera.ZoomLevel;
            if (zoomlvl < 0.99)
            {
                DrawGalmap(matrix);
            }
            else
            {
                if (!string.IsNullOrEmpty(SelectedStarSysGuid) && RenderedMaps.ContainsKey(SelectedStarSysGuid))
                    RenderedMaps[SelectedStarSysGuid].Draw();
            }

            // Restore the render state
            _window.SetRenderState(savedRenderState);
        }

        private void DrawGalmap(Matrix matrix)
        {
            foreach (var item in StarIcons)
            {
                item.Value.OnFrameUpdate(matrix, _camera);
                item.Value.Draw(_window.Renderer, _camera);
            }
        }
    }


    public class CollisionGrid
    {
        int _cellSize;
        int _gridWidth;
        int _gridHeight;
        GridItems[] _gridItems = new GridItems[1];
        struct GridItems
        {
            internal Guid[] itemGuids;
        }

        public CollisionGrid(int width, int height, int cellSize)
        {
            SetGrid(width, height, cellSize);
        }

        void SetGrid(int width, int height, int cellSize)
        {
            _cellSize = cellSize;
            _gridWidth = width;
            _gridHeight = height;
            _gridItems = new GridItems[width * height];
        }

        public Guid[] GetItemsAtPx(int x, int y)
        {
            return GetItemsAtCell(x / _cellSize, y / _cellSize);
        }

        public Guid[] GetItemsAtCell(int x, int y)
        {
            int index = y * _gridWidth + x;
            GridItems items = _gridItems[index];
            return items.itemGuids;
        }

        public void SetItemsFromPx(int x, int y, Guid guid)
        {
            SetItemsToCell(x / _cellSize, y / _cellSize, guid);
        }

        public void SetItemsToCell(int x, int y, Guid guid)
        {
            int index = y * _gridWidth + x;
            GridItems items = _gridItems[index];
            int itemCount = items.itemGuids.Length + 1;

            var itemGuids = new Guid[itemCount];
            for (int i = 0; i < itemCount; i++)
            {
                itemGuids[i] = items.itemGuids[i];
            }
            itemGuids[itemCount - 1] = guid;

            _gridItems[index] = new GridItems
            {
                itemGuids = itemGuids
            };
        }

        public void RemoveItemsFromPx(int x, int y, Guid guid)
        {
            RemoveItemsFromCell(x / _cellSize, y / _cellSize, guid);
        }

        public void RemoveItemsFromCell(int x, int y, Guid guid)
        {
            int index = y * _gridWidth + x;
            GridItems items = _gridItems[index];
            int itemCount = items.itemGuids.Length + 1;
            int indexToRemove = -1;
            for (int i = 0; i < itemCount; i++)
            {
                if (items.itemGuids[i] == guid)
                {
                    indexToRemove = i;
                    break;
                }

                if(indexToRemove != -1)
                {
                    var itemGuids = new Guid[itemCount];
                    for (int i2 = 0; i2 < itemCount; i2++)
                    {
                        if(i2 != indexToRemove)
                            itemGuids[i2] = items.itemGuids[i];
                    }
                    _gridItems[index] = new GridItems
                    { };
                }
            }

        }

    }

}
