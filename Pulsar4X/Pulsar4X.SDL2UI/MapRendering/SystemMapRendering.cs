using System;
using Pulsar4X.ECSLib;
using ImGuiSDL2CS;
using SDL2;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ImGuiNET;

namespace Pulsar4X.SDL2UI
{
    public class UserOrbitSettings
    {
        public float EllipseSweepRadians = 4.71239f;

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
        Camera _camera;
        internal IntPtr windowPtr;
        internal IntPtr surfacePtr; 
        internal IntPtr rendererPtr;
        ImGuiSDL2CSWindow _window;
        Dictionary<Guid, Icon> _testIcons = new Dictionary<Guid, Icon>();
        Dictionary<Guid, Icon> _entityIcons = new Dictionary<Guid, Icon>();
        Dictionary<Guid, OrbitIcon> _orbitRings = new Dictionary<Guid, OrbitIcon>();
        internal Dictionary<Guid, NameIcon> _nameIcons = new Dictionary<Guid, NameIcon>();
        List<Vector4> _positions = new List<Vector4>();
        List<OrbitDB> _orbits = new List<OrbitDB>();
        SystemMap_DrawableVM _sysMap;
        Entity _faction;



        internal SystemMapRendering(ImGuiSDL2CSWindow window, GlobalUIState state)
        {
            _state = state;
            _camera = _state.Camera;
            _window = window;
            windowPtr = window.Handle;
            surfacePtr = SDL.SDL_GetWindowSurface(windowPtr);
            rendererPtr = SDL.SDL_GetRenderer(windowPtr);
            foreach (var item in TestDrawIconData.GetTestIcons())
            {
                _testIcons.Add(Guid.NewGuid(), item);
            }
        }

        internal void SetSystem(FactionVM factionVM)
        {
            _sysMap = factionVM.SystemMap;
            _faction = _state.Faction;

            foreach (var entityItem in _sysMap.IconableEntitys)
            {
                if (entityItem.HasDataBlob<OrbitDB>())
                {
                    var orbitDB = entityItem.GetDataBlob<OrbitDB>();
                    if(!orbitDB.IsStationary)
                    {
                        OrbitIcon orbit = new OrbitIcon(entityItem, _state.UserOrbitSettings);
                        _orbitRings.Add(entityItem.Guid, orbit);
                    }
                }
                if (entityItem.HasDataBlob<StarInfoDB>())
                {
                    _entityIcons.Add(entityItem.Guid, new StarDrawData(entityItem));
                }
                if (entityItem.HasDataBlob<SystemBodyInfoDB>())
                {
                    _entityIcons.Add(entityItem.Guid, new PlanetDrawData(entityItem));
                }
                if (entityItem.HasDataBlob<ShipInfoDB>())
                {
                    _entityIcons.Add(entityItem.Guid, new ShipIcon(entityItem));
                }
                if (entityItem.HasDataBlob<NameDB>())
                {
                    _nameIcons.Add(entityItem.Guid, new NameIcon(entityItem));
                }

            }


        }

        public void UpdateUserOrbitSettings()
        {
            foreach (var item in _orbitRings.Values)
            {
                item.UpdateUserSettings();
            }
        }

        void HandleChanges()
        {
            var updates = _sysMap.GetUpdates();
            foreach (var changeData in updates)
            {
                if (changeData.ChangeType == EntityChangeData.EntityChangeType.DBAdded)
                {
                    if (changeData.Datablob is OrbitDB && changeData.Entity.GetDataBlob<OrbitDB>().Parent != null)
                    {
                        if (!((OrbitDB)changeData.Datablob).IsStationary)
                            _orbitRings[changeData.Entity.Guid] = new OrbitIcon(changeData.Entity, _state.UserOrbitSettings);
                    }
                    //if (changeData.Datablob is NameDB)
                        //TextIconList[changeData.Entity.Guid] = new TextIcon(changeData.Entity, _camera);

                    //_entityIcons[changeData.Entity.Guid] = new EntityIcon(changeData.Entity, _camera);
                }
                if (changeData.ChangeType == EntityChangeData.EntityChangeType.DBRemoved)
                {
                    if (changeData.Datablob is OrbitDB)
                        _orbitRings.Remove(changeData.Entity.Guid);
                    //if (changeData.Datablob is NameDB)
                        //TextIconList.Remove(changeData.Entity.Guid);
                }
            }
        }


        internal void Draw()
        {
            Matrix matrix = new Matrix();



            matrix.Scale(_camera.ZoomLevel);
            if (_sysMap == null)
            {
                foreach (var item in _testIcons.Values)
                {
                    item.ViewScreenPos = matrix.Transform(item.WorldPositionX, item.WorldPositionY);
                    item.Draw(rendererPtr, _camera);
                }
            }
            else
            {
                if (_sysMap.UpdatesReady)
                    HandleChanges();



                foreach (var icon in _orbitRings.Values)
                {
                    icon.ViewScreenPos = matrix.Transform(icon.WorldPositionX, icon.WorldPositionY);
                    icon.Draw(rendererPtr, _camera);
                }
                foreach (var icon in _entityIcons.Values)
                {
                    icon.ViewScreenPos = matrix.Transform(icon.WorldPositionX, icon.WorldPositionY);
                    icon.Draw(rendererPtr, _camera);
                }
                foreach (var icon in _nameIcons.Values)
                {
                    icon.ViewScreenPos = matrix.Transform(icon.WorldPositionX, icon.WorldPositionY);
                    //icon.Draw(rendererPtr, _camera); //cannot Begin here. 
                }
            }
        }
    }
}
