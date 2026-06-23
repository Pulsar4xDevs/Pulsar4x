using System;
using System.Collections.Generic;
using System.Linq;
using SDL3;
using ImGuiNET;
using Pulsar4X.Api;
using Pulsar4X.Orbital;
using Pulsar4X.Client.Rendering;
using Pulsar4X.Client.Interface;
using Pulsar4X.Client.Interface.Widgets;

namespace Pulsar4X.Client
{
    public class GalacticMapRender
    {
        GlobalUIState _state;
        Dictionary<string, SystemMapRendering> RenderedMaps = new ();
        Dictionary<string, StarIcon> StarIcons = new ();
        Dictionary<string, string> _galMapLabels = new ();
        SDL3Window _window;
        internal string? CapitolSysMap { get; set; }
        internal string SelectedStarSysGuid { get { return _state.SelectedStarSystemId; } }
        internal SystemMapRendering? SelectedSysMapRender
        {
            get
            {
                return SelectedStarSysGuid != null && RenderedMaps.ContainsKey(SelectedStarSysGuid)
                    ? RenderedMaps[SelectedStarSysGuid]
                    : null;
            }
        }
        Camera _camera;

        int _syncedSystemCount = -1;

        public GalacticMapRender(SDL3Window window, GlobalUIState state)
        {
            _state = state;
            _window = window;
            _camera = state.Camera;

            _state.EntityClickedEvent += _state_EntityClickedEvent;
        }

        /// <summary>Reconciles the per-system maps and galaxy-map star icons against the known
        /// systems in the replicated galaxy. Cheap when nothing changed.</summary>
        void SyncKnownSystems()
        {
            var galaxy = _state.GameClient?.Galaxy;
            if (galaxy == null)
                return;

            var known = galaxy.KnownSystems;
            if (known.Count == _syncedSystemCount)
                return;
            _syncedSystemCount = known.Count;

            int i = 0;
            double startangle = 0;
            float angleIncrease = (float)Math.Max(0.78539816339, 6.28318530718 / Math.Max(1, known.Count));
            int startR = 200;
            int radInc = 5;

            foreach (var summary in known)
            {
                var systemId = summary.SystemId;
                var x = (startR + radInc * i) * Math.Sin(startangle - angleIncrease * i);
                var y = (startR + radInc * i) * Math.Cos(startangle - angleIncrease * i);

                if (!RenderedMaps.ContainsKey(systemId))
                {
                    SystemMapRendering map = new SystemMapRendering(_window, _state);
                    map.Initialize(systemId);
                    RenderedMaps[systemId] = map;
                    map.GalacticMapPosition.X = x;
                    map.GalacticMapPosition.Y = y;
                }

                _galMapLabels[systemId] = summary.Name;

                // The galaxy-map star icon: the system's primary star, drawn at the synthetic
                // galaxy-map position (treated as AU and converted to metres).
                if (!StarIcons.ContainsKey(systemId)
                    && galaxy.GetSystem(systemId) is { } system)
                {
                    var star = system.Entities.FirstOrDefault(e => e.Kind == BodyKind.Star
                        && (e.GetView<OrbitView>()?.ParentId ?? null) == null);
                    star ??= system.Entities.FirstOrDefault(e => e.Kind == BodyKind.Star);

                    if (star?.GetView<StarView>() is { } starView
                        && star.GetView<MassVolumeView>() is { } massVolume)
                    {
                        var posAU = new Orbital.Vector3(x, y, 0);
                        var starIcon = new StarIcon(starView, massVolume, new StaticPosition(Distance.AuToMt(posAU)));
                        StarIcons[systemId] = starIcon;
                    }
                }

                i++;
            }
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
                foreach (var (systemId, label) in _galMapLabels)
                {
                    if (!StarIcons.TryGetValue(systemId, out var starIcon))
                        continue;

                    var screenPos = starIcon.ViewScreenPos;
                    if (!_camera.IsOnScreen(screenPos.X, screenPos.Y))
                        continue;

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

                    ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(1f, 1f, 1f, 1f));
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
            SyncKnownSystems();

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
}
