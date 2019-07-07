using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ImGuiSDL2CS;
using SDL2;

namespace Pulsar4X.SDL2UI
{
    public class GalacticMapRender
    {
        GlobalUIState state;
        List<SystemState> SystemStates = new List<SystemState>();
        Dictionary<Guid,SystemMapRendering> RenderedMaps = new Dictionary<Guid, SystemMapRendering>();
        Dictionary<Guid, StarIcon> StarIcons = new Dictionary<Guid, StarIcon>();
        ConcurrentDictionary<Guid, NameIcon> _nameIcons = new ConcurrentDictionary<Guid, NameIcon>();
        ImGuiSDL2CSWindow window;
        internal Guid CapitolSysMap { get; set; }
        internal Guid SelectedStarSysGuid { get; set; }
        internal SystemMapRendering SelectedSysMapRender { get { return RenderedMaps[SelectedStarSysGuid]; } }
        Camera camera;
        IntPtr renderPtr;

        CollisionGrid grid;

        public GalacticMapRender(ImGuiSDL2CSWindow window, GlobalUIState state)
        {
            this.state = state;
            this.window = window;
            this.camera = state.Camera;

            var windowPtr = window.Handle;
            var surfacePtr = SDL.SDL_GetWindowSurface(windowPtr);
            this.renderPtr = SDL.SDL_GetRenderer(windowPtr);

            var size = window.Size;
            int cellSize = 16;
            int gridwid = (int)size.X / cellSize;
            int gridhig = (int)size.Y / cellSize;
            grid = new CollisionGrid(gridwid, gridhig, cellSize);
            this.state.EntityClickedEvent += _state_EntityClickedEvent;
        }

        internal void SetFaction()
        {
            //StarIcons = new Dictionary<Guid, IDrawData>();
            int i = 0;
            double startangle = 0;//Math.PI * 0.5;
            float angleIncrease = (float)Math.Max(Math.PI / 2, Math.PI * 2 / state.StarSystemStates.Count);
            int startR = 200;
            int radInc = 5;
            foreach (var item in state.StarSystemStates)
            {

                SystemMapRendering map = new SystemMapRendering(window, state);

                map.SetSystem(item.Value.StarSystem);
                RenderedMaps[item.Key] = map;

                //TODO: handle binary/multiple star systems better.
                var starEntity = item.Value.StarSystem.GetFirstEntityWithDataBlob<ECSLib.StarInfoDB>();
                var orbitdb = starEntity.GetDataBlob<ECSLib.OrbitDB>();
                starEntity = orbitdb.Root; //just in case it's a binary system and the entity we got was not the primary

                var starIcon = new StarIcon(starEntity);
                StarIcons[item.Key] = starIcon;
                var nameIcon = new NameIcon(item.Value.EntityStatesWithNames[starEntity.Guid], state);
                _nameIcons[item.Key] = nameIcon;
                var x = (startR + radInc * i) * Math.Sin(startangle - angleIncrease * i);
                var y = (startR + radInc * i) * Math.Cos(startangle - angleIncrease * i);
                starIcon.WorldPosition = new ECSLib.Vector3(x, y, 0);
                nameIcon.WorldPosition = new ECSLib.Vector3(x, y, 0);
                map.GalacticMapPosition.X = x;
                map.GalacticMapPosition.Y = y;
                i++;
            }
        }

        void _state_EntityClickedEvent(EntityState entityState, MouseButtons mouseButton)
        {
            var sysGuid = entityState.StarSysGuid;
            /** This is redundant? Why not assign the value and check it when using it? */
            if(SelectedStarSysGuid != sysGuid && RenderedMaps.ContainsKey(sysGuid))
            {
                SelectedStarSysGuid = sysGuid; 
            }
        }



        /** If the zoom level allows, perhaps only write the name of the System? */
        internal void DrawNameIcons()
        {
            var zoomlvl = state.Camera.ZoomLevel;
            if (zoomlvl >= 2.0)
            {
                foreach (var systemmap in RenderedMaps.Values)
                {
                    systemmap.DrawNameIcons();
                }
            }
            else
            {
                lock (_nameIcons)
                {
                    foreach (var item in _nameIcons.Values)
                    {
                        item.Draw(state.rendererPtr, state.Camera);
                    }
                }
            }
        }

        internal void Draw()
        {
            var matrix = camera.GetZoomMatrix();
            var zoomlvl = state.Camera.ZoomLevel;
            if (zoomlvl < 0.99) // draw galactic map
            {
                /** Removed redundant condition but this is still unreachable. X > 0.99 && X < 0.99 will always be FALSE. 
                    Perhaps you missed an equals (=) sign? */
                if (zoomlvl > 0.99) // draw systems as well as galactic map
                {
                    foreach (var systemmap in RenderedMaps.Values)
                    {
                        systemmap.Draw();
                    }
                }
                else
                {
                    DrawGalmap(matrix);
                }
            }
            else // only draw the system map. 
            {
                if (RenderedMaps.ContainsKey(SelectedStarSysGuid))
                {
                    RenderedMaps[SelectedStarSysGuid].Draw();
                }
            }

        }

        private void DrawGalmap(Matrix matrix)
        {

            foreach (var item in StarIcons)
            {
                item.Value.OnFrameUpdate(matrix, camera);
                lock (_nameIcons)
                {
                    foreach (var name in _nameIcons.Values) 
                    {
                        name.OnFrameUpdate(matrix, camera);
                    }
                }
                item.Value.Draw(renderPtr, camera);
            }
            lock (_nameIcons)
            {
                foreach (var item in _nameIcons)
                {
                    item.Value.OnFrameUpdate(matrix, camera);
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
                /** This is unreachable code!!!
                    Either there *is* a matching item -> then the break; exits the for loop.
                    Or there is no matching item      -> then the indexToRemove remains -1.
                 */
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
