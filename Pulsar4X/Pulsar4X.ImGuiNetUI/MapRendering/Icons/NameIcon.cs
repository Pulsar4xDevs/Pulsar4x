using System;
using System.Collections.Generic;
using ImGuiNET;
using Pulsar4X.ECSLib;
using SDL2;

namespace Pulsar4X.SDL2UI
{
    public class NameIcon : Icon, IComparable<NameIcon>, IRectangle
    {

        protected ImGuiWindowFlags _flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoBringToFrontOnFocus;
        internal bool IsActive = true;
        GlobalUIState _state;
        NameDB _nameDB;
        internal string NameString;
        public float Width { get; set; }
        public float Height{ get; set; }
        public float X { get { return ViewScreenPos.x; }  }
        public float Y { get { return ViewScreenPos.y; } }
        Guid _entityGuid;
        Guid _starSysGuid;
        public Dictionary<Guid, string> SubNames = new Dictionary<Guid, string>();
        public System.Numerics.Vector2 ViewOffset { get; set; } = new System.Numerics.Vector2();
        public Rectangle ViewDisplayRect = new Rectangle();
        UserOrbitSettings.OrbitBodyType _bodyType = UserOrbitSettings.OrbitBodyType.Unknown;
        internal float DrawAtZoom { get { return _state.DrawNameZoomLvl[(int)_bodyType]; } }
        public NameIcon(EntityState entityState, GlobalUIState state) : base(entityState.Entity.GetDataBlob<PositionDB>())
        {
            Random rnd = new Random();
            _state = state;
            _entityGuid = entityState.Entity.Guid;
            StarSystem starsys = (StarSystem)entityState.Entity.Manager;
            _starSysGuid = starsys.Guid;
            _nameDB = entityState.Entity.GetDataBlob<NameDB>();
            NameString = _nameDB.GetName(state.Faction);
            entityState.Name = NameString;
            entityState.NameIcon = this;
            _bodyType = entityState.BodyType;
        }


        public static NameIcon operator +(NameIcon nameIcon, SDL.SDL_Point point)
        {
            System.Numerics.Vector2 newpoint = new System.Numerics.Vector2()
            {
                X = nameIcon.ViewOffset.X + point.x,
                Y = nameIcon.ViewOffset.Y + point.y
            };
            nameIcon.ViewOffset = newpoint;

            return nameIcon;

        }

        //adds or updates the subname - this is mostly used for colonys on a planet
        public void AddSubName(Entity entity)
        {
            var nameString = entity.GetDataBlob<NameDB>().GetName(_state.Faction);
            SubNames[entity.Guid] = nameString;
        }
        public void RemoveSubName(Guid guid)
        {
            SubNames.Remove(guid);
        }

        public override void OnFrameUpdate(Matrix matrix, Camera camera)
        {
            //DefaultViewOffset = new SDL.SDL_Point() { x = Width, y = -Height };
            if (camera.ZoomLevel < DrawAtZoom)
                return;
            System.Numerics.Vector2 defualtOffset = new System.Numerics.Vector2(4,-(Height / 2));
            ViewOffset = defualtOffset;
            base.OnFrameUpdate(matrix, camera);

            ViewDisplayRect.X = ViewScreenPos.x;
            ViewDisplayRect.Y = ViewScreenPos.y;

        }

        /// <summary>
        /// Default comparer, based on worldposition. TODO: should this maybe be done on viewscreen position?
        /// Sorts Bottom to top, left to right, then alphabetically
        /// </summary>
        /// <param name="compareIcon"></param>
        /// <returns></returns>
        public int CompareTo(NameIcon compareIcon)
        {

            if (WorldPosition_AU.Y > compareIcon.WorldPosition_AU.Y) return -1;
            else if (this.WorldPosition_AU.Y < compareIcon.WorldPosition_AU.Y) return 1;
            else
            {
                if (this.WorldPosition_AU.X > compareIcon.WorldPosition_AU.X) return 1;
                else if (this.WorldPosition_AU.X < compareIcon.WorldPosition_AU.X) return -1;
                else return -NameString.CompareTo(compareIcon.NameString);
            }
        }

        private void SetUpContextMenu(Guid entityGuid)
        {
            _state.EntityClicked(entityGuid, _starSysGuid, MouseButtons.Alt);
            _state.ContextMenu = new EntityContextMenu(_state, entityGuid);
            _state.ContextMenu.Display();
        }


        //use to correctly draw all passed name icons
        public static void DrawAll(IntPtr rendererPtr, Camera camera, List<NameIcon> nameIcons)
        {
            List<List<NameIcon>> nameIconGroupings = new List<List<NameIcon>>();
            List<bool> alreadyGroupedItems = new List<bool>();
            alreadyGroupedItems.Resize<bool>(nameIcons.Count, false);

          // while(true){

           //}

            //ImGui.Begin("");
            //ImGui.Text(nameIcons[5].NameString);
            //ImGui.End();

            int iterations = 0;
            foreach(var nameIcon in nameIcons)
            {
                if(!alreadyGroupedItems[iterations])
                {
                    nameIconGroupings.Add(new List<NameIcon>());
                    nameIconGroupings[nameIconGroupings.Count -1].Add(nameIcon);
                    alreadyGroupedItems[iterations] = true;
                    int nestedIterations = 0;
                    foreach(var nestedNameIcon in nameIcons)
                    {
                        if(iterations != nestedIterations && !alreadyGroupedItems[nestedIterations])
                        {
                            //check if two names are within the same pixel of distance, if so groups them together into a single window to prevent name overlapping.
                            if((Math.Pow((nameIcon.X+nameIcon.ViewOffset.X)-(nestedNameIcon.X+nestedNameIcon.ViewOffset.X),2)+Math.Pow((nameIcon.Y+nameIcon.ViewOffset.Y)-(nestedNameIcon.Y+nestedNameIcon.ViewOffset.Y),2)) < Math.Pow(2,2))
                            {
                                nameIconGroupings[nameIconGroupings.Count -1].Add(nestedNameIcon);
                                alreadyGroupedItems[nestedIterations] = true;
                            }
                        }
                        nestedIterations++;
                    }
                }
                iterations++;
            }

            foreach(var nameIconGrouping in nameIconGroupings)
            {
                int x = (int)(nameIconGrouping[0].X + nameIconGrouping[0].ViewOffset.X);
                int y = (int)(nameIconGrouping[0].Y + nameIconGrouping[0].ViewOffset.Y);
                System.Numerics.Vector2 pos = new System.Numerics.Vector2(x, y);

                ImGui.PushStyleColor(ImGuiCol.WindowBg, new System.Numerics.Vector4(0, 0, 0, 0)); //make the background transperent. 

                ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 2);

                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(1, 2));
                ImGui.SetNextWindowPos(pos, ImGuiCond.Always);
                ImGui.Begin(nameIconGrouping[0].NameString, ref nameIconGrouping[0].IsActive, nameIconGrouping[0]._flags | ImGuiWindowFlags.NoDocking);
                foreach(var finalNameIcon in nameIconGrouping)
                {
                   finalNameIcon.Draw(rendererPtr, camera, false);
                }
                ImGui.PopStyleColor(); //have to pop the color change after pushing it.
                ImGui.PopStyleVar(3);
                ImGui.End();
            }
        }

        public override void Draw(IntPtr rendererPtr, Camera camera)
        {
            base.Draw(rendererPtr, camera);
            Draw(rendererPtr, camera, true);
        }

        //WARNING!!! DO NOT USE FOR BULK DRAWING, USE DRAWALL FOR BULK DRAWING^^;
        public void Draw(IntPtr rendererPtr, Camera camera, bool createNewWindow)
        {
            if (camera.ZoomLevel < DrawAtZoom)
                return;

            int x = (int)(X + ViewOffset.X);
            int y = (int)(Y + ViewOffset.Y);
            System.Numerics.Vector2 pos = new System.Numerics.Vector2(x, y);

            if(createNewWindow)
            {
                ImGui.PushStyleColor(ImGuiCol.WindowBg, new System.Numerics.Vector4(0, 0, 0, 0)); //make the background transperent. 

                ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
                //ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(1, 1));// <- not used
                ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 2);

                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(1, 2));
                ImGui.SetNextWindowPos(pos, ImGuiCond.Always);

                ImGui.Begin(NameString, ref IsActive, _flags);
            }

            ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0, 0, 0, 0));
            if (ImGui.Button(NameString+"##"+_entityGuid.ToString())) //If the name gets clicked, we tell the state. 
            {
                _state.EntityClicked(_entityGuid, _starSysGuid, MouseButtons.Primary);

            }

            if (ImGui.BeginPopupContextItem("NameContextMenu"+_entityGuid.ToString()+NameString, ImGuiPopupFlags.MouseButtonRight))
            {
                SetUpContextMenu(_entityGuid);
                ImGui.EndPopup();
            }
            //checks the state if the icon of the entity with this nameicon was altClicked, if yes then display the normal context menu
            if(_state._lastContextMenuOpenedEntityGuid == _entityGuid)
            {
                if(ImGui.BeginPopupContextVoid())
                {
                   SetUpContextMenu(_entityGuid);
                   ImGui.EndPopup();
                }
            }
            if(_state.StarSystemStates[_starSysGuid].EntityStatesWithNames[_entityGuid].Entity.HasDataBlob<JPSurveyableDB>() && _state.StarSystemStates.ContainsKey(_state.StarSystemStates[_starSysGuid].EntityStatesWithNames[_entityGuid].Entity.GetDataBlob<JPSurveyableDB>().SystemToGuid))
            {
                ImGui.Text("Jumps to: "+_state.StarSystemStates[_state.StarSystemStates[_starSysGuid].EntityStatesWithNames[_entityGuid].Entity.GetDataBlob<JPSurveyableDB>().SystemToGuid].StarSystem.NameDB.DefaultName);
            }

            //ImGui.BeginChild("subnames");
            foreach (var name in SubNames)
            {
                if (ImGui.Button(name.Value+"##"+_entityGuid.ToString()+name.Key.ToString()+name.Value))
                {
                    _state.EntityClicked(name.Key, _starSysGuid, MouseButtons.Primary);
                }
                if (ImGui.BeginPopupContextItem("subNameContextMenu"+name.Key+name.Value+_entityGuid.ToString()+NameString, ImGuiPopupFlags.MouseButtonRight))
                {
                    SetUpContextMenu(name.Key);

                    ImGui.EndPopup();
                }
                //checks the state if the icon of the entity with this subNameicon was altClicked, if yes then display the normal context menu for the the subname
                if(_state._lastContextMenuOpenedEntityGuid == name.Key)
                {
                    if(ImGui.BeginPopupContextVoid())
                    {
                        SetUpContextMenu(name.Key);
                        ImGui.EndPopup();
                    }
                }
            }
            //ImGui.EndChild();

            //var size = ImGui.GetItemRectSize();
            var size = ImGui.GetWindowSize();
            Width = size.X;
            Height = size.Y;
            ViewDisplayRect.Width = size.X;
            ViewDisplayRect.Height = size.Y;

            ImGui.PopStyleColor();
            if(createNewWindow){
               ImGui.PopStyleColor(); //have to pop the color change after pushing it.
               ImGui.PopStyleVar(3);
               ImGui.End();
            }
        }
    }

    /// <summary>
    /// IComparer for the Texticonrectangles (or any other rectangle)
    /// Sorts Bottom to top, left to right
    /// </summary>
    internal class ByViewPosition : IComparer<IRectangle>
    {
        public int Compare(IRectangle r1, IRectangle r2)
        {
            float r1B = r1.Y + r1.Height;
            float r1L = r1.X;
            float r2B = r2.Y + r1.Height;
            float r2L = r2.X;

            if (r1B > r2B) return -1;
            else if (r1B < r2B) return 1;
            else
            {
                if (r1L > r2L) return -1;
                else if (r1L < r2L) return 1;
                else return 0;
            }
        }
    }
}
