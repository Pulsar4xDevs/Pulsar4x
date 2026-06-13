using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using Pulsar4X.Api;
using Pulsar4X.Orbital;
using SDL3;

namespace Pulsar4X.Client.Rendering
{
    internal class SystemMapRendering : UpdateWindowState
    {
        GlobalUIState _state;
        string? _systemId;
        Camera _camera;
        SDL3Window _window;
        SystemLabelDistributor _distributor;

        internal Dictionary<string, IDrawData> UIWidgets = new ();

        ConcurrentDictionary<int, Icon> _testIcons = new ();
        ConcurrentDictionary<int, Icon> _orbitRings = new ();
        ConcurrentDictionary<int, Icon> _moveIcons = new ();
        ConcurrentDictionary<int, Icon> _entityIcons = new ();
        ConcurrentDictionary<int, Icon> _bodyIcons = new ();

        HashSet<EntityLabel> _allLabels = new ();
        HashSet<EntityLabel> _visibleLabels = new ();

        // The last snapshot reference each entity's icons were built from. Snapshots are immutable,
        // so a reference change means the entity changed and its icons need rebuilding. This is
        // sync bookkeeping only — nothing reads game data from it.
        Dictionary<int, EntitySnapshot> _iconedSnapshots = new ();

        DateTime _lastPhysicsTime;

        // Per-body-type minimum camera zoom for the label to render. Lower-tier
        // bodies (moons, ships, asteroids, comets) only show labels once you've
        // zoomed in enough that they aren't just visual clutter. Stars, planets,
        // dwarf planets and colonies are always shown (subject to view prefs).
        static readonly Dictionary<UserOrbitSettings.OrbitBodyType, float> _minZoomForLabel = new ()
        {
            { UserOrbitSettings.OrbitBodyType.Star,         0f },
            { UserOrbitSettings.OrbitBodyType.Planet,       0f },
            { UserOrbitSettings.OrbitBodyType.DwarfPlanet,  0f },
            { UserOrbitSettings.OrbitBodyType.Colony,       0f },
            { UserOrbitSettings.OrbitBodyType.Moon,        1e4f },
            { UserOrbitSettings.OrbitBodyType.Ship,        2e4f },
            { UserOrbitSettings.OrbitBodyType.Asteroid,    5e4f },
            { UserOrbitSettings.OrbitBodyType.Comet,       2e4f },
            { UserOrbitSettings.OrbitBodyType.Unknown,      0f },
        };

        ConcurrentDictionary<int, InteractableState[]> _interactable = new ();
        IOrderedEnumerable<IGrouping<byte, InteractableState>> _interactableGrouped;

        internal List<IDrawData> SelectedEntityExtras = new List<IDrawData>();
        internal Vector2 GalacticMapPosition = new Vector2();

        bool _updateLabels = false;

        internal SystemMapRendering(SDL3Window window, GlobalUIState state)
        {
            _state = state;

            _distributor = EntityLabelDistributor.Group;

            _camera = _state.Camera;
            _window = window;

            // Initialize ship icon texture
            ShipIcon.InitializeTexture(window.Renderer);

            foreach (var item in TestDrawIconData.GetTestIcons())
            {
                _testIcons.TryAdd(-1, item);
            }

            var mainWin = (PulsarMainWindow)window;
            mainWin.MouseButtonDownOccured += (object sender, SDL.Event e) => {
                if (mainWin.PlatformBackend.WantsMouseCapture())
                    return;

                foreach (var i in _interactableGrouped)
                {
                    var key = i.Key;

                    foreach (var j in i)
                    {
                        if (j.IsDisabled)
                            continue;

                        var item = j.Item;

                        var c = item.Contains(new (e.Motion.X, e.Motion.Y));

                        if (c)
                        {
                            j.IsPressed = true;
                            if (item.OnPointerDown(e))
                                return;
                        }
                    }
                }
            };
            mainWin.MouseButtonUpOccured += (object sender, SDL.Event e) => {
                if (mainWin.PlatformBackend.WantsMouseCapture())
                    return;

                foreach (var i in _interactableGrouped)
                {
                    var key = i.Key;

                    foreach (var j in i)
                    {
                        if (j.IsDisabled)
                            continue;

                        var item = j.Item;

                        var c = item.Contains(new (e.Motion.X, e.Motion.Y));

                        if (c)
                        {
                            j.IsPressed = false;
                            if (item.OnPointerUp(e))
                                return;
                        }
                    }
                }
            };
            mainWin.MouseMoveOccured += (object sender, SDL.Event e) => {
                foreach (var i in _interactableGrouped)
                {
                    var key = i.Key;

                    foreach (var j in i)
                    {
                        if (j.IsDisabled)
                            continue;

                        var item = j.Item;

                        if (mainWin.PlatformBackend.WantsMouseCapture())
                        {
                            if (j.IsHovered)
                            {
                                j.IsHovered = false;
                                if (item.OnPointerExit(e))
                                    return;
                            }
                            continue;
                        }

                        var c = item.Contains(new (e.Motion.X, e.Motion.Y));

                        if (j.IsHovered)
                        {
                            if (c)
                            {
                                if (item.OnPointerMove(e))
                                    return;
                            }
                            else
                            {
                                j.IsHovered = false;
                                if (item.OnPointerExit(e))
                                    return;
                            }
                        }
                        else if (c)
                        {
                            j.IsHovered = true;
                            if (item.OnPointerEnter(e))
                                return;
                        }
                    }
                }
            };

            _camera.PanOccured +=
                (object sender, Orbital.Vector3 pos) => _updateLabels = true;

            _camera.ZoomOccured +=
                (object sender, float zoom) => _updateLabels = true;

            SystemViewPreferences.GetInstance().ViewUpdateOccured +=
                (object sender, SystemViewPreferences.View view) => _updateLabels = true;

            // should be empty
            _interactableGrouped = _interactable
                .Values
                .SelectMany(x => x)
                .GroupBy(x => x.Item.Priority)
                .OrderByDescending(x => x.Key);
        }

        internal void Initialize(string systemId)
        {
            _systemId = systemId;
            SyncIcons();
            _updateLabels = true; // update labels on first frame
        }

        void AddEntityIcon(EntitySnapshot entity, Icon icon)
        {
            var l = new EntityLabelExtCombo(_state, entity, _systemId!);
            l.Padding = 3;

            _interactable.TryAdd(
                    entity.Id,
                    new[] { new InteractableState(l) });
            _entityIcons.TryAdd(entity.Id, icon);
            _allLabels.Add(l);
        }

        void AddIconable(EntitySnapshot entity)
        {
            if (_systemId == null)
                return;

            var position = new SnapshotPosition(_state, _systemId, entity.Id);
            var bodyType = UserOrbitSettings.FromBodyKind(entity.Kind);
            var massVolume = entity.GetView<MassVolumeView>();

            var orbit = entity.GetView<OrbitView>();
            if (orbit != null && orbit.SemiMajorAxisM > 0 && orbit.StandardGravParameter > 0)
            {
                IPosition parentPosition = orbit.ParentId is int parentId
                    ? new SnapshotPosition(_state, _systemId, parentId)
                    : position;
                if (orbit.Eccentricity < 1)
                    _orbitRings.TryAdd(entity.Id,
                        new OrbitEllipseIcon(orbit, position, parentPosition, bodyType, _state.UserOrbitSettingsMtx));
                else if (orbit.ParentSoiRadiusM > 0)
                    _orbitRings.TryAdd(entity.Id,
                        new OrbitHyperbolicIcon2(orbit, position, parentPosition, bodyType, _state.UserOrbitSettingsMtx));
            }

            if (entity.GetView<NewtonMoveView>() is { } newton && newton.SoiParentId is int newtonParentId)
            {
                _orbitRings.TryAdd(entity.Id, new NewtonMoveIcon(
                    newton, position, new SnapshotPosition(_state, _systemId, newtonParentId),
                    bodyType, _state.UserOrbitSettingsMtx));
            }

            if (entity.GetView<NewtonSimpleMoveView>() is { } newtonSimple && newtonSimple.SoiParentId is int simpleParentId)
            {
                var time = _state.GameClient?.Galaxy.Time.GameDateTime ?? default;
                _orbitRings.TryAdd(entity.Id, new NewtonSimpleIcon(
                    newtonSimple, position, new SnapshotPosition(_state, _systemId, simpleParentId),
                    bodyType, _state.UserOrbitSettingsMtx, time));
            }

            if (entity.GetView<WarpMovingView>() is { } warp)
            {
                IPosition? targetPosition = warp.TargetEntityId is int targetId
                    ? new SnapshotPosition(_state, _systemId, targetId)
                    : null;
                _orbitRings.TryAdd(entity.Id, new WarpMovingIcon(warp, position, targetPosition));
            }

            if (entity.GetView<StarView>() is { } star && massVolume != null)
            {
                AddEntityIcon(entity, new StarIcon(star, massVolume, position));
            }

            if (entity.HasView<BodyView>() && entity.Kind != BodyKind.Star && massVolume != null)
            {
                var i = new SysBodyIcon(entity, _systemId, position, Distance.MToAU(massVolume.RadiusMetres));
                i.AttachState(_state);

                var l = new EntityLabelExtCombo(_state, entity, _systemId);
                l.Padding = 3;

                _interactable.TryAdd(
                        entity.Id,
                        new[] { new InteractableState(i), new InteractableState(l) });
                _bodyIcons.TryAdd(entity.Id, i);
                _allLabels.Add(l);
            }

            if (entity.HasView<ShipView>() && entity.HasView<PositionView>())
            {
                AddEntityIcon(entity, new ShipIcon(position));
            }

            if (entity.HasView<ProjectileView>() && entity.HasView<PositionView>())
            {
                AddEntityIcon(entity, new ProjectileIcon(position, underThrust: entity.HasView<NewtonMoveView>()));
            }

            if (entity.GetView<BeamView>() is { } beam)
            {
                _entityIcons.TryAdd(entity.Id, new BeamIcon(beam, position));
            }

            if (entity.HasView<GravSurveyView>() && entity.HasView<PositionView>())
            {
                AddEntityIcon(entity, new PointOfInterestIcon(position));
            }
        }

        void RemoveIconable(int entityGuid)
        {
            _testIcons.TryRemove(entityGuid, out _);
            _entityIcons.TryRemove(entityGuid, out _);
            _orbitRings.TryRemove(entityGuid, out _);
            _moveIcons.TryRemove(entityGuid, out _);
            _interactable.TryRemove(entityGuid, out _);
            _bodyIcons.TryRemove(entityGuid, out _);
            _allLabels.RemoveWhere(x => x.EntityId == entityGuid);
        }

        /// <summary>The entity's orbit-ring icon, for screen-space hit testing (maneuver-node
        /// placement); null when the entity has no orbit ring.</summary>
        internal OrbitIconBase? GetOrbitIcon(int entityId)
            => _orbitRings.TryGetValue(entityId, out var icon) ? icon as OrbitIconBase : null;

        public void UpdateUserOrbitSettings()
        {
            foreach (var item in _orbitRings.Values)
            {
                if(item is IUpdateUserSettings foo)
                {
                    foo.UpdateUserSettings();
                }
            }
        }

        /// <summary>Reconciles the icon set against the system's current snapshots: new entities
        /// gain icons, changed snapshots rebuild them, departed entities lose them.</summary>
        void SyncIcons()
        {
            var system = _systemId != null ? _state.GameClient?.Galaxy.GetSystem(_systemId) : null;
            if (system == null)
                return;

            bool changed = false;
            var seen = new HashSet<int>();
            foreach (var entity in system.Entities)
            {
                seen.Add(entity.Id);
                if (_iconedSnapshots.TryGetValue(entity.Id, out var iconed))
                {
                    if (ReferenceEquals(iconed, entity))
                        continue;
                    RemoveIconable(entity.Id);
                }

                _iconedSnapshots[entity.Id] = entity;
                AddIconable(entity);
                changed = true;
            }

            foreach (var entityId in _iconedSnapshots.Keys.Where(id => !seen.Contains(id)).ToList())
            {
                RemoveIconable(entityId);
                _iconedSnapshots.Remove(entityId);
                changed = true;
            }

            if (changed)
                _updateLabels = true;
        }

        internal void Update()
        {
            if (_systemId == null) return;

            SyncIcons();

            // The galaxy clock only moves on server pushes; re-run the physics pass (orbit tail
            // indexes, warp curves) when it does.
            var galaxyTime = _state.GameClient?.Galaxy.Time.GameDateTime;
            if (galaxyTime is { } time && time != _lastPhysicsTime)
            {
                _lastPhysicsTime = time;
                RunPhysicsUpdate();
            }

            var matrix = _camera.GetZoomMatrix();
            foreach (var (_, item) in UIWidgets)
                item.OnFrameUpdate(matrix, _camera);

            foreach (var (_, item) in _orbitRings)
                item.OnFrameUpdate(matrix, _camera);

            foreach (var (_, item) in _moveIcons)
                item.OnFrameUpdate(matrix, _camera);

            foreach (var (_, item) in _entityIcons)
                item.OnFrameUpdate(matrix, _camera);

            foreach (var (_, item) in _bodyIcons)
                item.OnFrameUpdate(matrix, _camera);

            foreach (var item in SelectedEntityExtras)
                item.OnFrameUpdate(matrix, _camera);

            foreach (var item in _allLabels)
                item.OnFrameUpdate(matrix, _camera);

            if (_updateLabels)
            {
                _updateLabels = false;

                var prefs = SystemViewPreferences.GetInstance();

                foreach (var item in _interactable.Values)
                {
                    foreach (var i in item)
                        i.IsDisabled = true;
                }

                var zoom = _camera.ZoomLevel;
                var lbl = _allLabels
                    .Where(x => prefs.ShouldDisplay("map", x.BodyType)
                        && zoom >= _minZoomForLabel[x.BodyType]);

                _visibleLabels.Clear();
                foreach (var i in _distributor(lbl))
                {
                    if (!_interactable.TryGetValue(i.EntityId, out var states))
                        continue;
                    foreach (var j in states)
                        j.IsDisabled = false;
                    _visibleLabels.Add(i);
                }

                _interactableGrouped = _interactable
                    .Values
                    .SelectMany(x => x)
                    .GroupBy(x => x.Item.Priority)
                    .OrderByDescending(x => x.Key);
            }
        }

        void RunPhysicsUpdate()
        {
            foreach (var icon in UIWidgets.Values)
            {
                icon.OnPhysicsUpdate();
            }
            foreach (var icon in _orbitRings.Values)
            {
                icon.OnPhysicsUpdate();
            }
            foreach (var icon in _entityIcons.Values)
            {
                icon.OnPhysicsUpdate();
            }
            foreach (var icon in _moveIcons.Values.ToArray())
            {
                icon.OnPhysicsUpdate();
            }
            foreach (var icon in SelectedEntityExtras)
            {
                icon.OnPhysicsUpdate();
            }
        }

        internal void Draw()
        {
            DrawIcons(UIWidgets.Values);
            DrawIcons(_orbitRings.Values);
            DrawIcons(_moveIcons.Values);
            DrawIcons(_entityIcons.Values);
            DrawIcons(_bodyIcons.Values);
            DrawIcons(SelectedEntityExtras);

            foreach (var i in _visibleLabels)
                i.Draw(_window.Renderer, _camera);
        }

        void DrawIcons(IEnumerable<IDrawData> icons)
        {
            foreach (var item in icons)
                item.Draw(_window.Renderer, _camera);
        }

        public override bool GetActive()
        {
            return true;
        }

        public override void OnGameTickChange(DateTime newDate)
        {

        }

        public override void OnSystemTickChange(DateTime newDate)
        {
            _state.PrimarySystemDateTime = newDate;
        }
    }
}
