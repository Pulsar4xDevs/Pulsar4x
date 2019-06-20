using System;
using System.Collections.Generic;
using ImGuiNET;
using Pulsar4X.ECSLib;
using SDL2;
using System.Numerics;
//using Vector2 = ImGuiNET.Vector2;

namespace Pulsar4X.SDL2UI
{
    public class NameIcon : Icon, IComparable<NameIcon>, IRectangle
    {
        protected ImGuiWindowFlags _flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings;
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
        public Vector2 ViewOffset { get; set; } = new Vector2();
        public Rectangle ViewDisplayRect = new Rectangle();
        UserOrbitSettings.OrbitBodyType _bodyType = UserOrbitSettings.OrbitBodyType.Unknown;
        internal float DrawAtZoom { get { return _state.DrawNameZoomLvl[(int)_bodyType]; } }
        public NameIcon(EntityState entityState, GlobalUIState state) : base(entityState.Entity.GetDataBlob<PositionDB>())
        {
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
            Vector2 newpoint = new Vector2()
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
            Vector2 defualtOffset = new Vector2(4,-(Height / 2));
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

            if (WorldPosition.Y > compareIcon.WorldPosition.Y) return -1;
            else if (this.WorldPosition.Y < compareIcon.WorldPosition.Y) return 1;
            else
            {
                if (this.WorldPosition.X > compareIcon.WorldPosition.X) return 1;
                else if (this.WorldPosition.X < compareIcon.WorldPosition.X) return -1;
                else return -NameString.CompareTo(compareIcon.NameString);
            }
        }

        public override void Draw(IntPtr rendererPtr, Camera camera)
        {
            if (camera.ZoomLevel < DrawAtZoom)
                return;

            int x = (int)(X + ViewOffset.X);
            int y = (int)(Y + ViewOffset.Y);
            System.Numerics.Vector2 pos = new System.Numerics.Vector2(x, y);


            ImGui.PushStyleColor(ImGuiCol.WindowBg, new System.Numerics.Vector4(0, 0, 0, 0)); //make the background transperent. 

            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
            //ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(1, 1));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 2);

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(1, 2));
            ImGui.SetNextWindowPos(pos, ImGuiCond.Always);

            ImGui.Begin(NameString, ref IsActive, _flags);
            ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0, 0, 0, 0));
            if (ImGui.Button(NameString)) //If the name gets clicked, we tell the state. 
            {
                _state.EntityClicked(_entityGuid, _starSysGuid, MouseButtons.Primary);

            }
            if (ImGui.BeginPopupContextItem("NameContextMenu", 1))
            {
                _state.EntityClicked(_entityGuid, _starSysGuid, MouseButtons.Alt);
                _state.ContextMenu = new EntityContextMenu(_state, _entityGuid);
                _state.ContextMenu.Display();

                ImGui.EndPopup();
            }

            ImGui.BeginChild("subnames");
            foreach (var name in SubNames)
            {
                if (ImGui.Button(name.Value))
                {
                    _state.EntityClicked(name.Key, _starSysGuid, MouseButtons.Primary);
                }
                if (ImGui.BeginPopupContextItem("subNameContextMenu"+name.Key, 1))
                {
                    _state.EntityClicked(name.Key, _starSysGuid, MouseButtons.Alt);
                    _state.ContextMenu = new EntityContextMenu(_state, name.Key);
                    _state.ContextMenu.Display();

                    ImGui.EndPopup();
                }
            }

            ImGui.EndChild();

            //var size = ImGui.GetItemRectSize();
            var size = ImGui.GetWindowSize();
            Width = size.X;
            Height = size.Y;
            ViewDisplayRect.Width = size.X;
            ViewDisplayRect.Height = size.Y;

            ImGui.PopStyleColor();

            ImGui.End();
            ImGui.PopStyleColor(); //have to pop the color change after pushing it.
            ImGui.PopStyleVar(3);


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
