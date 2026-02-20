using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using System.Drawing;
using Pulsar4X.Orbital;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Sensors;
using Pulsar4X.Messaging;
using Pulsar4X.JumpPoints;
using Pulsar4X.Names;
using Pulsar4X.Orbits;
using Pulsar4X.Ships;
using Pulsar4X.Weapons;
using Pulsar4X.Galaxy;
using Pulsar4X.Movement;
using SDL3;

namespace Pulsar4X.Client.Rendering
{
    internal class SystemMapRendering : UpdateWindowState
    {
        GlobalUIState _state;
        SystemSensorContacts? _sensorMgr;
        ConcurrentQueue<Message>? _sensorChanges;
        SystemState? _sysState;
        Camera _camera;
        SDL3Window _window;
        internal Dictionary<string, IDrawData> UIWidgets = new ();
        ConcurrentDictionary<int, Icon> _testIcons = new ();
        ConcurrentDictionary<int, Icon> _entityIcons = new ();
        ConcurrentDictionary<int, Icon> _orbitRings = new ();
        ConcurrentDictionary<int, Icon> _moveIcons = new ();

        HashSet<EntityLabel> _allLabels = new ();
        EntityLabel[] _labels = [];
        SystemLabelDistributor _distributor;

        HashSet<InteractableState> _interactable = new ();
        IOrderedEnumerable<IGrouping<byte, InteractableState>> _interactableGrouped;

        internal List<IDrawData> SelectedEntityExtras = new List<IDrawData>();
        internal Vector2 GalacticMapPosition = new Vector2();
        //internal SystemMap_DrawableVM SysMap;
        Entity? _faction;

        internal SystemMapRendering(SDL3Window window, GlobalUIState state)
        {
            _state = state;

            _distributor = EntityLabelDistributor.Group;

            _camera = _state.Camera;
            _window = window;

            // Initialize ship icon texture
            ShipIcon.InitializeTexture(window.Renderer);

            //UIWidgets.Add(new CursorCrosshair(new Vector4())); //used for debugging the cursor world position.
            foreach (var item in TestDrawIconData.GetTestIcons())
            {
                _testIcons.TryAdd(-1, item);
            }

            //_state.OnStarSystemChanged += RespondToSystemChange;
            //_state.OnFactionChanged += RespondToSystemChange;

            var mainWin = (PulsarMainWindow)window;
            mainWin.MouseButtonDownOccured += (object sender, SDL.Event e) => {
                foreach (var i in _interactableGrouped)
                {
                    var key = i.Key;

                    foreach (var j in i)
                    {
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
                foreach (var i in _interactableGrouped)
                {
                    var key = i.Key;

                    foreach (var j in i)
                    {
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
                        var item = j.Item;

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
        }

        internal void Initialize(StarSystem starSys)
        {
            if(_state.Faction == null)
                throw new NullReferenceException();

            _faction = _state.Faction;

            if (_state.StarSystemStates.ContainsKey(starSys.ID))
            {
                _sysState = _state.StarSystemStates[starSys.ID];
            }
            else
            {
                _sysState = new SystemState(starSys, _faction.Id);
                _state.StarSystemStates[_sysState.StarSystem.ID] = _sysState;
            }

            _sensorMgr = starSys.GetSensorContacts(_faction.Id);
            _sensorChanges = _sensorMgr.Changes.Subscribe();
            _sysState.OnEntityAdded += OnSystemStateEntityAdded;
            _sysState.OnEntityUpdated += OnSystemStateEntityUpdated;
            _sysState.OnEntityRemoved += OnSystemStateEntityRemoved;

            foreach (var entityItem in _sysState.EntityStatesWithPosition.Values)
            {
                AddIconable(entityItem);
            }

            _interactableGrouped = _interactable
                .GroupBy(x => x.Item.Priority)
                .OrderByDescending(x => x.Key);
        }

        public void UpdateSystemState(SystemState systemState)
        {
            _testIcons.Clear();
            _entityIcons.Clear();
            _orbitRings.Clear();
            _moveIcons.Clear();
            _allLabels.Clear();
            _interactable.Clear();

            _sysState = systemState;
            _state.StarSystemStates[_sysState.StarSystem.ID] = _sysState;

            if(_state.Faction == null)
                throw new NullReferenceException();

            _faction = _state.Faction;
            _sensorMgr = systemState.StarSystem.GetSensorContacts(_faction.Id);
            _sensorChanges = _sensorMgr.Changes.Subscribe();

            foreach (var entityItem in _sysState.EntityStatesWithPosition.Values)
            {
                AddIconable(entityItem);
            }

            _interactableGrouped = _interactable
                .GroupBy(x => x.Item.Priority)
                .OrderByDescending(x => x.Key);
        }

        void AddIconable(EntityState entityState)
        {
            entityState.TryGetDataBlob<PositionDB>(out var positionDB);
            entityState.TryGetDataBlob<MassVolumeDB>(out var massVolumeDB);

            if (entityState.TryGetDataBlob<NameDB>(out var nameDB) && positionDB != null)
            {
                lock (_allLabels)
                {
                    var l = new EntityLabelExtCombo(entityState.Entity);

                    l.Padding = 3;
                    l.Faction = _state.Faction?.Id ?? Game.NeutralFactionId;
                    l.AttachState(_state);

                    _allLabels.Add(l);
                    _interactable.Add(new InteractableState(l));
                }
            }

            if (entityState.TryGetDataBlob<OrbitDB>(out var orbitDB))
            {
                if (!orbitDB.IsStationary)
                {
                    OrbitIconBase orbit;
                    if (orbitDB.Eccentricity < 1)
                    {
                        orbit = new OrbitEllipseIcon(entityState, _state.UserOrbitSettingsMtx);
                        _orbitRings.TryAdd(entityState.Id, orbit);
                    }
                    else
                    {
                        orbit = new OrbitHyperbolicIcon2(entityState, _state.UserOrbitSettingsMtx);
                        _orbitRings.TryAdd(entityState.Id, orbit);
                    }
                }
            }

            if (entityState.TryGetDataBlob<NewtonMoveDB>(out var newtonMoveDB))
            {
                _orbitRings.TryAdd(entityState.Id, new NewtonMoveIcon(entityState, newtonMoveDB, _state.UserOrbitSettingsMtx));
            }

            if (entityState.TryGetDataBlob<NewtonSimpleMoveDB>(out var newtonSimpleMoveDB))
            {
                _orbitRings.TryAdd(entityState.Id, new NewtonSimpleIcon(entityState, newtonSimpleMoveDB, _state.UserOrbitSettingsMtx));
            }

            if (entityState.TryGetDataBlob<WarpMovingDB>(out var warpMovingDB) && positionDB != null)
            {
                _orbitRings.TryAdd(entityState.Id, new WarpMovingIcon(warpMovingDB, positionDB));
            }


            if (entityState.TryGetDataBlob<StarInfoDB>(out var starInfoDB)
                && massVolumeDB != null
                && positionDB != null)
            {
                _entityIcons.TryAdd(entityState.Id, new StarIcon(starInfoDB, positionDB, massVolumeDB));
            }

            if (entityState.TryGetDataBlob<SystemBodyInfoDB>(out var systemBodyInfoDB)
                && massVolumeDB != null
                && positionDB != null)
            {
                var i = new SysBodyIcon(entityState, systemBodyInfoDB, positionDB, massVolumeDB);
                i.AttachState(_state);

                _entityIcons.TryAdd(entityState.Id, i);
                _interactable.Add(new InteractableState(i));
            }

            if (entityState.TryGetDataBlob<ShipInfoDB>(out var shipInfoDB) && positionDB != null)
            {
                _entityIcons.TryAdd(entityState.Id, new ShipIcon(entityState, shipInfoDB, positionDB));
            }

            if (entityState.TryGetDataBlob<ProjectileInfoDB>(out var projectileInfoDB) && positionDB != null)
            {
                _entityIcons.TryAdd(entityState.Id, new ProjectileIcon(entityState, positionDB));
            }

            if (entityState.TryGetDataBlob<BeamInfoDB>(out var beamInfoDB) && positionDB != null)
            {
                _entityIcons.TryAdd(entityState.Id, new BeamIcon(beamInfoDB, positionDB));
            }

            if(entityState.TryGetDataBlob<JPSurveyableDB>(out var jPSurveyableDB) && positionDB != null)
            {
                _entityIcons.TryAdd(entityState.Id, new PointOfInterestIcon(positionDB));
            }

        }

        void RemoveIconable(int entityGuid)
        {
            _testIcons.TryRemove(entityGuid, out var testIcon);
            _entityIcons.TryRemove(entityGuid, out var entityIcon);
            _orbitRings.TryRemove(entityGuid, out var orbitIcon);
            _moveIcons.TryRemove(entityGuid, out var moveIcon);

            lock (_allLabels)
            {
                _allLabels.RemoveWhere(x => x.Entity.Id == entityGuid);
            }
        }


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

        void HandleChanges(EntityState entityState)
        {

            foreach (var message in entityState.Changes)
            {
                if(message.EntityId == null) continue;

                if (message.MessageType == MessageTypes.DBAdded)
                {
                    if (message.DataBlob is OrbitDB)
                    {
                        OrbitDB orbitDB = (OrbitDB)message.DataBlob;
                        if (orbitDB.Parent == null)
                            continue;


                        if (!orbitDB.IsStationary)
                        {
                            if (_sysState != null && _sysState.EntityStatesWithPosition.ContainsKey(message.EntityId.Value))
                            {
                                entityState = _sysState.EntityStatesWithPosition[message.EntityId.Value];
                            }
                            else if(_sysState != null && message.FactionId != null && _sysState.StarSystem.TryGetEntityById(message.EntityId.Value, out var retrievedEntity))
                            {
                                entityState = new EntityState(retrievedEntity, message.EntityId.Value, message.FactionId.Value);
                            }

                            OrbitIconBase orbit;
                            if (orbitDB.Eccentricity < 1)
                            {
                               orbit = new OrbitEllipseIcon(entityState, _state.UserOrbitSettingsMtx);
                            }
                            else
                            {
                                orbit = new OrbitHyperbolicIcon2(entityState, _state.UserOrbitSettingsMtx);
                            }
                            _orbitRings[message.EntityId.Value] = orbit;

                        }
                    }
                    if (message.DataBlob is WarpMovingDB
                        && _sysState != null
                        && _sysState.StarSystem.TryGetEntityById(message.EntityId.Value, out var entity)
                        && entity.TryGetDataBlob<PositionDB>(out var positionDB))
                    {
                        var widget = new WarpMovingIcon((WarpMovingDB)message.DataBlob, positionDB);
                        widget.OnPhysicsUpdate();
                        //Matrix matrix = new Matrix();
                        //matrix.Scale(_camera.ZoomLevel);
                        //widget.OnFrameUpdate(matrix, _camera);
                        _moveIcons[message.EntityId.Value] = widget;
                        //_moveIcons.Add(changeData.Entity.ID, widget);
                    }

                    if (message.DataBlob is NewtonMoveDB)
                    {

                        Icon orb = new NewtonMoveIcon(entityState, (NewtonMoveDB)message.DataBlob, _state.UserOrbitSettingsMtx);
                        _orbitRings.AddOrUpdate(message.EntityId.Value, orb, ((guid, data) => data = orb));
                    }
                    //if (changeData.Datablob is NameDB)
                    //TextIconList[changeData.Entity.ID] = new TextIcon(changeData.Entity, _camera);

                    //_entityIcons[changeData.Entity.ID] = new EntityIcon(changeData.Entity, _camera);
                }
                if (message.MessageType == MessageTypes.DBRemoved)
                {
                    if (message.DataBlob is OrbitDB)
                    {

                        _orbitRings.TryRemove(message.EntityId.Value, out var foo);
                    }
                    if (message.DataBlob is WarpMovingDB)
                    {
                        _moveIcons.TryRemove(message.EntityId.Value, out var foo);
                    }

                    if (message.DataBlob is NewtonMoveDB)
                    {
                        _orbitRings.TryRemove(message.EntityId.Value, out var foo);
                    }
                }
            }
        }

        private void OnSystemStateEntityAdded(SystemState systemState, Entity entity)
        {
            if(systemState.EntityStatesWithPosition.ContainsKey(entity.Id))
                AddIconable(systemState.EntityStatesWithPosition[entity.Id]);
        }

        private void OnSystemStateEntityUpdated(SystemState systemState, int entityId, Message message)
        {
            // Refreseh the icons for the updated entity
            if(systemState.EntityStatesWithPosition.ContainsKey(entityId))
            {
                RemoveIconable(entityId);
                AddIconable(systemState.EntityStatesWithPosition[entityId]);
            }
        }

        private void OnSystemStateEntityRemoved(SystemState systemState, int entityId)
        {
            RemoveIconable(entityId);
        }

        internal void Update()
        {
            if(_sysState == null) return;

            foreach (var item in _sysState.EntityStatesWithPosition.Values)
            {
                if (item.Changes.Count > 0)
                {
                    HandleChanges(item);
                }
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

            foreach (var item in SelectedEntityExtras)
                item.OnFrameUpdate(matrix, _camera);

            lock (_allLabels)
            {
                foreach (var i in _allLabels)
                    i.OnFrameUpdate(matrix, _camera);

                _labels = _distributor(_allLabels).ToArray();
            }
        }

        internal void Draw()
        {
            DrawIcons(UIWidgets.Values.ToList());
            DrawFilteredIcons(_orbitRings);
            DrawFilteredIcons(_moveIcons);
            DrawFilteredIcons(_entityIcons);
            DrawIcons(SelectedEntityExtras);
        }

        internal void DrawNameIcons()
        {
            lock (_labels)
            {
                foreach (var i in _labels)
                    i.Draw(_window.Renderer, _camera);
            }
        }

        void DrawFilteredIcons(ConcurrentDictionary<int, Icon> icons)
        {
            if (_sysState == null)
            {
                foreach (var item in icons.Values)
                    item.Draw(_window.Renderer, _camera);
                return;
            }

            var prefs = SystemViewPreferences.GetInstance();
            foreach (var (entityId, item) in icons)
            {
                if (_sysState.EntityStatesWithPosition.TryGetValue(entityId, out var entityState)
                    && !prefs.ShouldDisplay("map", entityState.BodyType))
                    continue;

                item.Draw(_window.Renderer, _camera);
            }
        }

        void DrawIcons(List<IDrawData> icons)
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
            foreach(var icon in SelectedEntityExtras)
            {
                icon.OnPhysicsUpdate();
            }
        }
    }
}
