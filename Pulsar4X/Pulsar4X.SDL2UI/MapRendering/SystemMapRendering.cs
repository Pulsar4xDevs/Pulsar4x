using System;
using Pulsar4X.ECSLib;
using ImGuiSDL2CS;
using SDL2;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ImGuiNET;
using System.Linq;

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
        internal List<IDrawData> UIWidgets = new List<IDrawData>();
        Dictionary<Guid, Icon> _testIcons = new Dictionary<Guid, Icon>();
        Dictionary<Guid, Icon> _entityIcons = new Dictionary<Guid, Icon>();
        Dictionary<Guid, OrbitIcon> _orbitRings = new Dictionary<Guid, OrbitIcon>();
        Dictionary<Guid, ShipMoveWidget> _moveIcons = new Dictionary<Guid, ShipMoveWidget>();
        internal Dictionary<Guid, NameIcon> _nameIcons = new Dictionary<Guid, NameIcon>();
        List<Vector4> _positions = new List<Vector4>();
        List<OrbitDB> _orbits = new List<OrbitDB>();
        internal SystemMap_DrawableVM SysMap;
        Entity _faction;

        internal Dictionary<Guid, EntityState> IconEntityStates = new Dictionary<Guid, EntityState>();


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
                _testIcons.Add(Guid.NewGuid(), item);
            }
        }

        internal void SetSystem(FactionVM factionVM)
        {
            SysMap = factionVM.SystemMap;
            _faction = _state.Faction;
            SysMap.SystemSubpulse.SystemDateChangedEvent += OnSystemDateChange;
            _state.CurrentSystemDateTime = SysMap.SystemSubpulse.SystemLocalDateTime;
            //_state.LastClickedEntity =

            foreach (var entityItem in SysMap.IconableEntitys)
            {


                var entityState = new EntityState() { Entity = entityItem, Name = "Unknown"  };

                if (entityItem.HasDataBlob<NameDB>())
                {
                    _nameIcons.Add(entityItem.Guid, new NameIcon(ref entityState, _state));
                }
                if (entityItem.HasDataBlob<OrbitDB>())
                {
                    var orbitDB = entityItem.GetDataBlob<OrbitDB>();
                    if(!orbitDB.IsStationary)
                    {
                        OrbitIcon orbit = new OrbitIcon(ref entityState, _state.UserOrbitSettings);
                        _orbitRings.Add(entityItem.Guid, orbit);
                    }
                }
                if (entityItem.HasDataBlob<StarInfoDB>())
                {
                    _entityIcons.Add(entityItem.Guid, new StarIcon(entityItem));
                }
                if (entityItem.HasDataBlob<SystemBodyInfoDB>())
                {
                    _entityIcons.Add(entityItem.Guid, new SysBodyIcon(entityItem));
                }
                if (entityItem.HasDataBlob<ShipInfoDB>())
                {
                    _entityIcons.Add(entityItem.Guid, new ShipIcon(entityItem));
                }

                IconEntityStates.Add(entityItem.Guid, entityState);
            }
            _state.LastClickedEntity = _state.MapRendering.IconEntityStates.Values.ElementAt(0);

        }

        void OnSystemDateChange(DateTime newDate)
        {
            _state.CurrentSystemDateTime = newDate;
            if (SysMap.UpdatesReady)
                HandleChanges();
            foreach (var icon in UIWidgets)
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
            var updates = SysMap.GetUpdates();
            foreach (var changeData in updates)
            {
                if (changeData.ChangeType == EntityChangeData.EntityChangeType.DBAdded)
                {
                    if (changeData.Datablob is OrbitDB && changeData.Entity.GetDataBlob<OrbitDB>().Parent != null)
                    {
                        if (!((OrbitDB)changeData.Datablob).IsStationary)
                        {
                            EntityState entityState;
                            if (IconEntityStates.ContainsKey(changeData.Entity.Guid))
                                entityState = IconEntityStates[changeData.Entity.Guid];
                            else
                                entityState = new EntityState() { Entity = changeData.Entity, Name = "Unknown" };
                            
                            _orbitRings[changeData.Entity.Guid] = new OrbitIcon(ref entityState, _state.UserOrbitSettings);
                        
                        }
                    }
                    if (changeData.Datablob is TranslateMoveDB)
                    {
                        var widget = new ShipMoveWidget(changeData.Entity);
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
                        _orbitRings.Remove(changeData.Entity.Guid);
                    if (changeData.Datablob is TranslateMoveDB)
                        _moveIcons.Remove(changeData.Entity.Guid);

                    //if (changeData.Datablob is NameDB)
                        //TextIconList.Remove(changeData.Entity.Guid);
                }
            }
        }


        public void TextIconsDistribute()
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
                var item = texiconsCopy[i-1];
                ImVec2 height = new ImVec2() { x = 0, y = item.Height };
                int lowestPosIndex = occupiedPosition.BinarySearch(item.ViewDisplayRect + height, byViewPos);
                int lpi = lowestPosIndex;
                if (lowestPosIndex < 0)
                    lpi = ~lowestPosIndex;

                for (int j = lpi; j < occupiedPosition.Count; j++)
                {
                    if (item.ViewDisplayRect.Intersects(occupiedPosition[j]))
                    {
                        var newpoint = new ImVec2()
                        {
                            x = item.ViewOffset.x,
                            y = item.ViewOffset.Y - occupiedPosition[j].Height
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

            byte oR, oG, oB, oA;
            SDL.SDL_GetRenderDrawColor(rendererPtr, out oR, out oG, out oB, out oA);
            SDL.SDL_BlendMode blendMode;
            SDL.SDL_GetRenderDrawBlendMode(rendererPtr, out blendMode);
            SDL.SDL_SetRenderDrawBlendMode(rendererPtr, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);

            Matrix matrix = new Matrix();
            matrix.Scale(_camera.ZoomLevel);

            if (SysMap == null)
            {
                foreach (var icon in _testIcons.Values)
                {
                    icon.ViewScreenPos = matrix.Transform(icon.WorldPosition.X, icon.WorldPosition.Y);
                    icon.Draw(rendererPtr, _camera);
                }
            }
            else
            {

                foreach (var icon in UIWidgets.ToArray())
                {
                    icon.OnFrameUpdate(matrix, _camera);
                }
                foreach (var icon in _orbitRings.Values.ToArray())
                {
                    icon.OnFrameUpdate(matrix, _camera);
                }
                foreach (var icon in _moveIcons.Values.ToArray())
                {
                    icon.OnFrameUpdate(matrix, _camera);
                }
                foreach (var icon in _entityIcons.Values.ToArray())
                {

                    icon.OnFrameUpdate(matrix, _camera);
                }
                foreach (var icon in _nameIcons.Values.ToArray())
                {
                    icon.OnFrameUpdate(matrix, _camera);
                }
                TextIconsDistribute();

                foreach (var icon in UIWidgets.ToArray())
                {
                    icon.Draw(rendererPtr, _camera);
                }
                foreach (var icon in _orbitRings.Values.ToArray())
                {
                    
                    icon.Draw(rendererPtr, _camera);
                }
                foreach (var icon in _moveIcons.Values.ToArray())
                {
                    icon.Draw(rendererPtr, _camera);
                }
                foreach (var icon in _entityIcons.Values.ToArray())
                {
                    icon.Draw(rendererPtr, _camera);
                }


                SDL.SDL_SetRenderDrawColor(rendererPtr, oR, oG, oB, oA);
                SDL.SDL_SetRenderDrawBlendMode(rendererPtr, blendMode);
            }
        }
    }
}
