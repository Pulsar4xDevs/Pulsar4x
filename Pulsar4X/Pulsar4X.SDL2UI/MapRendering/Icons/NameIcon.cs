using System;
using System.Collections.Generic;
using ImGuiNET;
using Pulsar4X.ECSLib;
using SDL2;

namespace Pulsar4X.SDL2UI
{
    public class NameIcon : Icon, IComparable<NameIcon>, IRectangle
    {
        protected ImGuiWindowFlags _flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize;
        internal bool IsActive = true;
        GlobalUIState _state;
        NameDB _nameDB;
        internal string NameString;
        public float Width { get; set; }
        public float Height{ get; set; }
        public float X { get { return ViewScreenPos.x; }  }
        public float Y { get { return ViewScreenPos.y; } }
        Guid _entityGuid;

        public ImVec2 ViewOffset { get; set; } = new ImVec2();
        public Rectangle ViewDisplayRect = new Rectangle(); 

        public NameIcon(ref EntityState entityState, GlobalUIState state) : base(entityState.Entity.GetDataBlob<PositionDB>())
        {
            _state = state;
            _entityGuid = entityState.Entity.Guid;
            _nameDB = entityState.Entity.GetDataBlob<NameDB>();
            NameString = _nameDB.GetName(state.Faction);
            entityState.Name = NameString;
            entityState.NameIcon = this;

        }


        public static NameIcon operator +(NameIcon nameIcon, SDL.SDL_Point point)
        {
            ImVec2 newpoint = new ImVec2()
            {
                x = nameIcon.ViewOffset.x + point.x,
                y = nameIcon.ViewOffset.y + point.y
            };
            nameIcon.ViewOffset = newpoint;

            return nameIcon;

        }



        public override void OnFrameUpdate(Matrix matrix, Camera camera)
        {
            //DefaultViewOffset = new SDL.SDL_Point() { x = Width, y = -Height };

            ImVec2 defualtOffset = new ImVec2(4,-(Height / 2));
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
            var camerapoint = camera.CameraViewCoordinate();
            int x = (int)(X + camerapoint.x + ViewOffset.x);
            int y = (int)(Y + camerapoint.y + ViewOffset.y);
            ImVec2 pos = new ImVec2(x, y);

            ImGui.PushStyleColor(ImGuiCol.WindowBg, new ImVec4(0, 0, 0, 0)); //make the background transperent. 
            ImGui.SetNextWindowPos(pos, ImGuiCond.Always);


            ImGui.Begin(NameString, ref IsActive, _flags);

            ImGui.PushStyleColor(ImGuiCol.Button, new ImVec4(0, 0, 0, 0));
            if (ImGui.Button(NameString)) //If the name gets clicked, we tell the state. 
            {
                _state.EntityClicked(_entityGuid, MouseButtons.Primary);

            }
            var size = ImGui.GetLastItemRectSize();
            Height = size.y;
            Width = size.x;
            ViewDisplayRect.Width = size.x;
            ViewDisplayRect.Height = size.y;

            ImGui.PopStyleColor();
            if (ImGui.BeginPopupContextItem("NameContextMenu", 1))
            {
                _state.EntityClicked(_entityGuid, MouseButtons.Alt);
                _state.ContextMenu = new EntityContextMenu(_state, _entityGuid);
                _state.ContextMenu.Display();

                ImGui.EndPopup();
            }



            ImGui.End();
            ImGui.PopStyleColor(); //have to pop the color change after pushing it. 
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
