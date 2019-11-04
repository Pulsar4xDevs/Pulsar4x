using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ImGuiSDL2CS;
using SDL2;

namespace Pulsar4X.SDL2UI
{
    public class GalacticMapRender
    {
        GlobalUIState _state;
        List<SystemState> SystemStates = new List<SystemState>();
        Dictionary<Guid,SystemMapRendering> RenderedMaps = new Dictionary<Guid, SystemMapRendering>();
        Dictionary<Guid, StarIcon> StarIcons = new Dictionary<Guid, StarIcon>();
        ConcurrentDictionary<Guid, NameIcon> _nameIcons = new ConcurrentDictionary<Guid, NameIcon>();
        ImGuiSDL2CSWindow _window;
        internal Guid CapitolSysMap { get; set; }
        internal Guid SelectedStarSysGuid { get; set; }
        internal SystemMapRendering SelectedSysMapRender { get { return RenderedMaps[SelectedStarSysGuid]; } }
        Camera _camera;
        IntPtr _renderPtr;

        CollisionGrid grid;

        public GalacticMapRender(ImGuiSDL2CSWindow window, GlobalUIState state)
        {
            _state = state;
            _window = window;
            _camera = state.Camera;

            var windowPtr = window.Handle;
            var surfacePtr = SDL.SDL_GetWindowSurface(windowPtr);
            _renderPtr = SDL.SDL_GetRenderer(windowPtr);

            var size = window.Size;
            int cellSize = 16;
            int gridwid = (int)size.X / cellSize;
            int gridhig = (int)size.Y / cellSize;
            grid = new CollisionGrid(gridwid, gridhig, cellSize);
            _state.EntityClickedEvent += _state_EntityClickedEvent;
        }

        internal void SetFaction()
        {
            //StarIcons = new Dictionary<Guid, IDrawData>();
            int i = 0;
            double startangle = 0;//Math.PI * 0.5;
            float angleIncrease = (float)Math.Max(0.78539816339, 6.28318530718 / _state.StarSystemStates.Count);
            int startR = 200;
            int radInc = 5;
            foreach (KeyValuePair<Guid, SystemState> item in _state.StarSystemStates)
            {

                SystemMapRendering map = new SystemMapRendering(_window, _state);

                map.SetSystem(item.Value.StarSystem);
                RenderedMaps[item.Key] = map;

                //TODO: handle binary/multiple star systems better.
                var starEntity = item.Value.StarSystem.GetFirstEntityWithDataBlob<ECSLib.StarInfoDB>();
                var orbitdb = starEntity.GetDataBlob<ECSLib.OrbitDB>();
                starEntity = orbitdb.Root; //just incase it's a binary system and the entity we got was not the primary

                var starIcon = new StarIcon(starEntity);
                StarIcons[item.Key] = starIcon;
                var nameIcon = new NameIcon(item.Value.EntityStatesWithNames[starEntity.Guid], _state);
                _nameIcons[item.Key] = nameIcon;
                var x = (startR + radInc * i) * Math.Sin(startangle - angleIncrease * i);
                var y = (startR + radInc * i) * Math.Cos(startangle - angleIncrease * i);
                starIcon.WorldPosition_m = new ECSLib.Vector3(x, y, 0);
                nameIcon.WorldPosition_m = new ECSLib.Vector3(x, y, 0);
                map.GalacticMapPosition.X = x;
                map.GalacticMapPosition.Y = y;
                i++;
            }
        }

        void _state_EntityClickedEvent(EntityState entityState, MouseButtons mouseButton)
        {
            var sysGuid = entityState.StarSysGuid;
            if(SelectedStarSysGuid != sysGuid && RenderedMaps.ContainsKey(sysGuid))
            {
                SelectedStarSysGuid = sysGuid; 
            }

        }



        internal void DrawNameIcons()
        {
            var zoomlvl = _state.Camera.ZoomLevel;
            if (zoomlvl >= 2.0)
            {
                foreach (var kvp in RenderedMaps)
                {
                    var sysid = kvp.Key;
                    var sysmap = kvp.Value;
                    sysmap.DrawNameIcons();
                }
            }
            else
            {
                lock (_nameIcons)
                {
                    foreach (var item in _nameIcons)
                    {
                        item.Value.Draw(_state.rendererPtr, _state.Camera);
                    }
                }
            }
        }

        internal void Draw()
        {
            var matrix = _camera.GetZoomMatrix();
            var zoomlvl = _state.Camera.ZoomLevel;
            if (zoomlvl < 0.99) // draw galmap
            {
                if (zoomlvl < 0.99 && zoomlvl > 0.99) //draw systems as well as galmap
                {
                    foreach (var kvp in RenderedMaps)
                    {
                        var sysid = kvp.Key;
                        var sysmap = kvp.Value;
                        sysmap.Draw();
                    }
                }
                else
                {
                    DrawGalmap(matrix);
                }
            }
            else// only draw the systemmap. 
            {
                if (SelectedStarSysGuid != Guid.Empty) 
                    RenderedMaps[SelectedStarSysGuid].Draw();
            }

        }

        private void DrawGalmap(Matrix matrix)
        {

            foreach (var item in StarIcons)
            {
                item.Value.OnFrameUpdate(matrix, _camera);
                lock (_nameIcons)
                {
                    foreach (var name in _nameIcons.Values)
                        name.OnFrameUpdate(matrix, _camera);
                }
                item.Value.Draw(_renderPtr, _camera);
            }
            lock (_nameIcons)
            {
                foreach (var item in _nameIcons)
                {
                    item.Value.OnFrameUpdate(matrix, _camera);

                }
            }

        }
    }


    public class CollisionGrid
    {
        int _cellSize;
        int _gridWidth;
        int _gridHeight;
        GridItems[] _gridItems;
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
