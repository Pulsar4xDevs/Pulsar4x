﻿using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using Pulsar4X.ECSLib;
using ImGuiSDL2CS;
using SDL2;


namespace Pulsar4X.SDL2UI
{
    public class UserOrbitSettings
    {
        internal enum OrbitBodyType
        {
            Unknown,
            Star,
            Planet,
            Moon,
            Asteroid,
            Comet,
            Ship,
            NumberOf

        }
        internal enum OrbitTrajectoryType
        {
            Unknown,
            Elliptical,
            Hyperbolic,
            NewtonionThrust,
            NonNewtonionTranslation,
            NumberOf         
        }

        public float EllipseSweepRadians = 4.71239f;
        public float ShowNameAtZoom = 100;
        //32 is a good low number, slightly ugly.  180 is a little overkill till you get really big orbits. 
        public byte NumberOfArcSegments = 180; 

        public byte Red = 0;
        public byte Grn = 0;
        public byte Blu = 255;
        public byte MaxAlpha = 255;
        public byte MinAlpha = 0;
    }

    internal class SystemMapRendering
    {
        GlobalUIState _state;
        SystemSensorContacts _sensorMgr;
        ConcurrentQueue<EntityChangeData> _sensorChanges;
        SystemState _sysState;
        Camera _camera;
        internal IntPtr windowPtr;
        internal IntPtr surfacePtr;
        internal IntPtr rendererPtr;
        ImGuiSDL2CSWindow _window;
        internal Dictionary<string, IDrawData> UIWidgets = new Dictionary<string, IDrawData>();
        ConcurrentDictionary<Guid, Icon> _testIcons = new ConcurrentDictionary<Guid, Icon>();
        ConcurrentDictionary<Guid, IDrawData> _entityIcons = new ConcurrentDictionary<Guid, IDrawData>();
        ConcurrentDictionary<Guid, IDrawData> _orbitRings = new ConcurrentDictionary<Guid, IDrawData>();
        ConcurrentDictionary<Guid, IDrawData> _moveIcons = new ConcurrentDictionary<Guid, IDrawData>();
        internal ConcurrentDictionary<Guid, NameIcon> _nameIcons = new ConcurrentDictionary<Guid, NameIcon>();

        internal List<IDrawData> SelectedEntityExtras = new List<IDrawData>();
        internal PointD GalacticMapPosition = new PointD();
        //internal SystemMap_DrawableVM SysMap;
        Entity _faction;

        internal SystemMapRendering(ImGuiSDL2CSWindow window, GlobalUIState state)
        {
            _state = state;

            _camera = _state.Camera;
            _window = window;
            windowPtr = window.Handle;
            surfacePtr = SDL.SDL_GetWindowSurface(windowPtr);
            rendererPtr = SDL.SDL_GetRenderer(windowPtr);
            //UIWidgets.Add(new CursorCrosshair(new Vector4())); //used for debugging the cursor world position. 
            foreach (var item in TestDrawIconData.GetTestIcons())
            {
                _testIcons.TryAdd(Guid.NewGuid(), item);
            }
        }

        internal void SetSystem(FactionVM factionVM)
        {
            if (_sysState != null)
            {
                _sysState.StarSystem.ManagerSubpulses.SystemDateChangedEvent -= OnSystemDateChange;
                _sysState.StarSystem.GetSensorContacts(_faction.Guid).Changes.Unsubscribe(_sensorChanges);

            }
            if (_state.StarSystemStates.ContainsKey(factionVM.SystemMap.StarSystem.Guid))
                _sysState = _state.StarSystemStates[factionVM.SystemMap.StarSystem.Guid];
            else
            {
                _sysState = new SystemState(factionVM.SystemMap.StarSystem, factionVM.FactionEntity);
                _state.StarSystemStates[_sysState.StarSystem.Guid] = _sysState;
            }
            var starSys = _sysState.StarSystem;
            _faction = _state.Faction;
            starSys.ManagerSubpulses.SystemDateChangedEvent += OnSystemDateChange;
            _sensorMgr = starSys.GetSensorContacts(_faction.Guid);
            _sensorChanges = _sensorMgr.Changes.Subscribe();



            _state.SetActiveSystem(starSys.Guid);


            foreach (var entityItem in _sysState.EntityStatesWithPosition.Values)
            {
                AddIconable(entityItem);
            }

            _state.LastClickedEntity = _sysState.EntityStatesWithPosition.Values.ElementAt(0);

        }

        internal void SetSystem(StarSystem starSys)
        {
            if (_sysState != null)
            {
                _sysState.StarSystem.ManagerSubpulses.SystemDateChangedEvent -= OnSystemDateChange;
                _sysState.StarSystem.GetSensorContacts(_faction.Guid).Changes.Unsubscribe(_sensorChanges);
            }
            if (_state.StarSystemStates.ContainsKey(starSys.Guid))
            {
                _sysState = _state.StarSystemStates[starSys.Guid];
            }
            else
            {
                _sysState = new SystemState(starSys, _state.Faction);
                _state.StarSystemStates[_sysState.StarSystem.Guid] = _sysState;
            }

            _faction = _state.Faction;

            starSys.ManagerSubpulses.SystemDateChangedEvent += OnSystemDateChange;
            _sensorMgr = starSys.GetSensorContacts(_faction.Guid);

            _sensorChanges = _sensorMgr.Changes.Subscribe();
            foreach (var entityItem in _sysState.EntityStatesWithPosition.Values)
            {
                AddIconable(entityItem);
            }
        }

        void AddIconable(EntityState entityState)
        {
            var entityItem = entityState.Entity;

            if (entityItem.HasDataBlob<NameDB>())
            {
                _nameIcons.TryAdd(entityItem.Guid, new NameIcon(entityState, _state));
            }


            if (entityItem.HasDataBlob<OrbitDB>())
            {
                var orbitDB = entityItem.GetDataBlob<OrbitDB>();
                if (!orbitDB.IsStationary)
                {
                    OrbitIconBase orbit;
                    if (orbitDB.Eccentricity < 1)
                    {
                        orbit = new OrbitEllipseIcon(entityState, _state.UserOrbitSettingsMtx);
                        _orbitRings.TryAdd(entityItem.Guid, orbit);
                    }

                }
            }

            if (entityItem.HasDataBlob<NewtonMoveDB>())
            {
                var hyp = entityItem.GetDataBlob<NewtonMoveDB>();
                OrbitHypobolicIcon orb;
                orb = new OrbitHypobolicIcon(entityState, _state.UserOrbitSettingsMtx);
                _orbitRings.TryAdd(entityItem.Guid, orb);
            }

            if (entityItem.HasDataBlob<StarInfoDB>())
            {
                _entityIcons.TryAdd(entityItem.Guid, new StarIcon(entityItem));
            }
            if (entityItem.HasDataBlob<SystemBodyInfoDB>())
            {
                _entityIcons.TryAdd(entityItem.Guid, new SysBodyIcon(entityItem));
                if (entityItem.GetDataBlob<SystemBodyInfoDB>().Colonies.Count > 0)
                {
                    foreach (var colony in entityItem.GetDataBlob<SystemBodyInfoDB>().Colonies)
                    {
                        _nameIcons[entityItem.Guid].AddSubName(colony);
                        _sysState.EntityStatesWithPosition[entityItem.Guid].NameIcon = _nameIcons[entityItem.Guid];
                        /*
                        IconEntityStates.Add(colony.Guid, new EntityState(colony)
                        {

                            Name = _nameIcons[entityItem.Guid].SubNames[colony.Guid],
                            NameIcon = _nameIcons[entityItem.Guid]
                        });*/
                    }
                }
            }
            if (entityItem.HasDataBlob<ShipInfoDB>())
            {
                _entityIcons.TryAdd(entityItem.Guid, new ShipIcon(entityItem));
            }

        }

        void RemoveIconable(Guid entityGuid)
        {
            _nameIcons.TryRemove(entityGuid, out NameIcon nameIcon);
            _entityIcons.TryRemove(entityGuid, out IDrawData entityIcon);
            _orbitRings.TryRemove(entityGuid, out IDrawData orbitIcon);
        }

        void OnSystemDateChange(DateTime newDate)
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
            foreach (var icon in _nameIcons.Values)
            {
                icon.OnPhysicsUpdate();
            }
            foreach(var icon in SelectedEntityExtras)
            {
                icon.OnPhysicsUpdate();
            }
        }


        public void UpdateUserOrbitSettings()
        {
            foreach (OrbitEllipseIcon item in _orbitRings.Values)
            {
                item.UpdateUserSettings();
            }
        }

        void HandleChanges(EntityState entityState)
        {

            foreach (var changeData in entityState.Changes)
            {
                if (changeData.ChangeType == EntityChangeData.EntityChangeType.DBAdded)
                {
                    if (changeData.Datablob is OrbitDB && changeData.Entity.GetDataBlob<OrbitDB>().Parent != null)
                    {
                        if (!((OrbitDB)changeData.Datablob).IsStationary)
                        {
                            if (_sysState.EntityStatesWithPosition.ContainsKey(changeData.Entity.Guid))
                                entityState = _sysState.EntityStatesWithPosition[changeData.Entity.Guid];
                            else
                                entityState = new EntityState(changeData.Entity) { Name = "Unknown" };

                            _orbitRings[changeData.Entity.Guid] = new OrbitEllipseIcon(entityState, _state.UserOrbitSettingsMtx);

                        }
                    }
                    if (changeData.Datablob is TranslateMoveDB)
                    {
                        var widget = new ShipMoveWidget(changeData.Entity);
                        widget.OnPhysicsUpdate();
                        //Matrix matrix = new Matrix();
                        //matrix.Scale(_camera.ZoomLevel);
                        //widget.OnFrameUpdate(matrix, _camera);
                        _moveIcons[changeData.Entity.Guid] = widget;
                        //_moveIcons.Add(changeData.Entity.Guid, widget);
                    }
                    //if (changeData.Datablob is NameDB)
                    //TextIconList[changeData.Entity.Guid] = new TextIcon(changeData.Entity, _camera);

                    //_entityIcons[changeData.Entity.Guid] = new EntityIcon(changeData.Entity, _camera);
                }
                if (changeData.ChangeType == EntityChangeData.EntityChangeType.DBRemoved)
                {
                    if (changeData.Datablob is OrbitDB)
                    {

                        _orbitRings.TryRemove(changeData.Entity.Guid, out IDrawData foo);
                    }
                    if (changeData.Datablob is TranslateMoveDB)
                    {
                        _moveIcons.TryRemove(changeData.Entity.Guid, out IDrawData foo);
                    }

                    //if (changeData.Datablob is NameDB)
                    //TextIconList.Remove(changeData.Entity.Guid);
                }
            }
        }

        void TextIconsDistribute()
        {
            if (_nameIcons.Count == 0)
                return;
            var occupiedPosition = new List<IRectangle>();
            IComparer<IRectangle> byViewPos = new ByViewPosition();
            var textIconList = new List<NameIcon>(_nameIcons.Values);


            //Consolidate TextIcons that share the same position and name
            textIconList.Sort();
            int listLength = textIconList.Count;
            int textIconQuantity = 1;
            for (int i = 1; i < listLength; i++)
            {
                if (textIconList[i - 1].CompareTo(textIconList[i]) == 0)
                {
                    textIconQuantity++;
                    textIconList.RemoveAt(i);
                    i--;
                    listLength--;
                }
                else if (textIconQuantity > 1)
                {
                    textIconList[i - 1].NameString += " x" + textIconQuantity;
                    textIconQuantity = 1;
                }
            }

            //Placement happens bottom to top, left to right
            //Each newly placed Texticon is compared to only the Texticons that are placed above its position
            //Therefore a sorted list of the occupied Positions is maintained
            occupiedPosition.Add(textIconList[0]);



            List<NameIcon> texiconsCopy = new List<NameIcon>();
            texiconsCopy.AddRange(_nameIcons.Values);

            int numTextIcons = texiconsCopy.Count;

            for (int i = 1; i < numTextIcons; i++)
            {
                var item = texiconsCopy[i - 1];
                Vector2 height = new Vector2() { X = 0, Y = item.Height };
                int lowestPosIndex = occupiedPosition.BinarySearch(item.ViewDisplayRect + height, byViewPos);
                int lpi = lowestPosIndex;
                if (lowestPosIndex < 0)
                    lpi = ~lowestPosIndex;

                for (int j = lpi; j < occupiedPosition.Count; j++)
                {
                    if (item.ViewDisplayRect.Intersects(occupiedPosition[j]))
                    {
                        var newpoint = new Vector2()
                        {
                            X = item.ViewOffset.X,
                            Y = item.ViewOffset.Y - occupiedPosition[j].Height
                        };
                        item.ViewOffset = newpoint;
                    }
                }
                //Inserts the new label sorted
                int insertIndex = occupiedPosition.BinarySearch(item, byViewPos);
                if (insertIndex < 0) insertIndex = ~insertIndex;
                occupiedPosition.Insert(insertIndex, item);
            }


        }



        internal void Draw()
        {
            if (_camera.ZoomLevel <= 0.1) //todo: base this number off the largest orbit
            {
                //draw galaxy map instead
            }
            else
            {
                if (_sysState != null)
                {
                    foreach (var entityGuid in _sysState.EntitiesAdded)
                    {
                        AddIconable(_sysState.EntityStatesWithPosition[entityGuid]);
                    }
                    foreach (var item in _sysState.EntityStatesWithPosition.Values)
                    {
                        if (item.Changes.Count > 0)
                        {
                            HandleChanges(item);
                        }
                    }
                    foreach (var item in _sysState.EntitysToBin)
                    {
                        RemoveIconable(item);
                    }
                }

                byte oR, oG, oB, oA;
                SDL.SDL_GetRenderDrawColor(rendererPtr, out oR, out oG, out oB, out oA);
                SDL.SDL_BlendMode blendMode;
                SDL.SDL_GetRenderDrawBlendMode(rendererPtr, out blendMode);
                SDL.SDL_SetRenderDrawBlendMode(rendererPtr, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);

                var matrix = _camera.GetZoomMatrix();

                UpdateAndDraw(UIWidgets, matrix);

                UpdateAndDraw(_orbitRings, matrix);

                UpdateAndDraw(_moveIcons, matrix);

                UpdateAndDraw(_entityIcons, matrix);

                UpdateAndDraw(SelectedEntityExtras, matrix);

                //because _nameIcons are imgui not sdl, we don't draw them here.
                //we draw them in PulsarMainWindow.ImGuiLayout
                lock (_nameIcons)
                {
                    foreach (var item in _nameIcons.Values)
                        item.OnFrameUpdate(matrix, _camera);
                }
                TextIconsDistribute();

                //ImGui.GetOverlayDrawList().AddText(new System.Numerics.Vector2(500, 500), 16777215, "FooBarBaz");

                SDL.SDL_SetRenderDrawColor(rendererPtr, oR, oG, oB, oA);
                SDL.SDL_SetRenderDrawBlendMode(rendererPtr, blendMode);
                //}
            }
        }

        public void DrawNameIcons()
        {

            lock (_nameIcons)
            {
                foreach (var item in _nameIcons.Values)
                {
                    item.Draw(_state.rendererPtr, _state.Camera);
                }
            }

        }

        void UpdateAndDraw(Dictionary<string, IDrawData> icons, Matrix matrix)
        {
            foreach (var item in icons.Values)
                item.OnFrameUpdate(matrix, _camera);
            foreach (var item in icons.Values)
                item.Draw(rendererPtr, _camera);
        }

        void UpdateAndDraw(IList<IDrawData> icons, Matrix matrix)
        {
            foreach (var item in icons)
                item.OnFrameUpdate(matrix, _camera);
            foreach (var item in icons)           
                item.Draw(rendererPtr, _camera);
        }
        void UpdateAndDraw(Dictionary<Guid, IDrawData> icons, Matrix matrix)
        {
            lock (icons)
            {
                foreach (var item in icons.Values)
                    item.OnFrameUpdate(matrix, _camera);
                foreach (var item in icons.Values)
                    item.Draw(rendererPtr, _camera);
            }
        }
        void UpdateAndDraw(ConcurrentDictionary<Guid, IDrawData> icons, Matrix matrix)
        {
            foreach (var item in icons.Values)
            {
                item.OnFrameUpdate(matrix, _camera);
            }
            foreach (var item in icons.Values)
            {
                item.Draw(rendererPtr, _camera);
            }
        }
    }
}
