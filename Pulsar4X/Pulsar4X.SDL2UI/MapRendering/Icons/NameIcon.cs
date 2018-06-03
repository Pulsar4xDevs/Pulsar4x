using System;
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
        public int Width { get; set; }
        public int Height{ get; set; }
        public int X { get { return ViewScreenPos.x; } }
        public int Y { get { return ViewScreenPos.y; } }
        Guid _entityGuid;

        public NameIcon(ref EntityState entityState, GlobalUIState state) : base(entityState.Entity.GetDataBlob<PositionDB>())
        {
            _state = state;
            _entityGuid = entityState.Entity.Guid;
            _nameDB = entityState.Entity.GetDataBlob<NameDB>();
            NameString = _nameDB.DefaultName;
            entityState.Name = NameString;
            entityState.NameIcon = this;
        }


        /// <summary>
        /// Default comparer, based on worldposition.
        /// Sorts Bottom to top, left to right, then alphabetically
        /// </summary>
        /// <param name="compareIcon"></param>
        /// <returns></returns>
        public int CompareTo(NameIcon compareIcon)
        {
            if (this.WorldPositionY > compareIcon.WorldPositionY) return -1;
            else if (this.WorldPositionY < compareIcon.WorldPositionY) return 1;
            else
            {
                if (this.WorldPositionX > compareIcon.WorldPositionX) return 1;
                else if (this.WorldPositionX < compareIcon.WorldPositionX) return -1;
                else return -NameString.CompareTo(compareIcon.NameString);
            }
        }

        public bool Intersects(NameIcon icon)
        {
            int myL = ViewScreenPos.x;
            int myR = ViewScreenPos.x + Width;
            int myT = ViewScreenPos.y;
            int myB = ViewScreenPos.y + Height; 


            int iconL = icon.ViewScreenPos.x;
            int iconR = icon.ViewScreenPos.x + icon.Width;
            int iconT = icon.ViewScreenPos.y;
            int iconB = icon.ViewScreenPos.y + icon.Height;


            return (myL < iconR &&
                    myR > iconL &&
                    myT < iconB &&
                    myB > iconT);
        }

        public override void Draw(IntPtr rendererPtr, Camera camera)
        {
            var camerapoint = camera.CameraViewCoordinate();
            int x = (int)(ViewScreenPos.x + camerapoint.x);
            int y = (int)(ViewScreenPos.y + camerapoint.y);
            ImVec2 pos = new ImVec2(x, y);

            ImGui.PushStyleColor(ImGuiCol.WindowBg, new ImVec4(0, 0, 0, 0)); //make the background transperent. 
            ImGui.SetNextWindowPos(pos, ImGuiCond.Always);

            ImGui.Begin(NameString, ref IsActive, _flags);

            ImGui.PushStyleColor(ImGuiCol.Button, new ImVec4(0, 0, 0, 0));
            if (ImGui.Button(NameString)) //name.
            {
                _state.EntitySelected(_entityGuid);
            }
            ImGui.PopStyleColor();
            if (ImGui.BeginPopupContextItem("NameContextMenu", 1))
            {
                //if(_state.ContextMenu == null)
                _state.ContextMenu = new EntityContextMenu(_state, _entityGuid);
                _state.ContextMenu.Display();

                ImGui.EndPopup();
            }



            ImGui.End();
            ImGui.PopStyleColor(); //have to pop the color change after pushing it. 
        }

    }
}
