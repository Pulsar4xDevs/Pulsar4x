using System;
using ImGuiNET;
using Pulsar4X.ECSLib;
using SDL2;

namespace Pulsar4X.SDL2UI
{
    public class NameIcon : Icon
    {
        protected ImGuiWindowFlags _flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize;
        internal bool IsActive = true;

        Entity _entity;
        NameDB _nameDB;
        string _nameString;

        public NameIcon(Entity entity) : base(entity.GetDataBlob<PositionDB>())
        {
            _entity = entity;
            _nameDB = entity.GetDataBlob<NameDB>();
            _nameString = _nameDB.DefaultName;
        }

        public override void Draw(IntPtr rendererPtr, Camera camera)
        {
            var camerapoint = camera.CameraViewCoordinate();
            int x = (int)(ViewScreenPos.x + camerapoint.x);
            int y = (int)(ViewScreenPos.y + camerapoint.y);
            ImVec2 pos = new ImVec2(x, y);

            ImGui.PushStyleColor(ImGuiCol.WindowBg, new ImVec4(0, 0, 0, 0)); //make the background transperent. 

            ImGui.SetNextWindowPos(pos, ImGuiCond.Always);
            ImGui.Begin(_nameString, ref IsActive, _flags);
            ImGui.Text(_nameString);

            if (ImGui.BeginPopupContextItem("NameContextMenu", 1))
            {
                if (ImGui.Button("Pin Camera"))
                {
                    camera.PinToEntity(_entity);
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }

            ImGui.End();
            ImGui.PopStyleColor(); //have to pop the color change. 
        }


    }
}
