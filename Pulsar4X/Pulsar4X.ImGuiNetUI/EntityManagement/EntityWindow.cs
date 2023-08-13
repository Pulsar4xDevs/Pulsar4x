using System;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public class EntityWindow : NonUniquePulsarGuiWindow
    {
        public Entity Entity { get; private set; }
        public EntityState EntityState { get; private set; }
        public String Title { get; private set; }

        private Vector2 ButtonSize = new Vector2(32, 32);

        public EntityWindow(EntityState entityState)
        {
            Entity = entityState.Entity;
            EntityState = entityState;

            if(Entity.HasDataBlob<NameDB>())
            {
                Title = Entity.GetDataBlob<NameDB>().GetName(_uiState.Faction);
            }
            else
            {
                Title = "Unknown";
            }
        }

        internal override void Display()
        {
            if(!IsActive) return;

            ImGui.SetNextWindowSize(new System.Numerics.Vector2(264, 325), ImGuiCond.Once);
            if (ImGui.Begin(Title + " (" + EntityState.BodyType.ToDescription() + ")###" + Entity.Guid, ref IsActive, _flags))
            {
                ImGui.PushID(0);
                if(ImGui.ImageButton(_uiState.Img_Pin(), ButtonSize))
                {
                    _uiState.Camera.PinToEntity(Entity);
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Pin camera");
                ImGui.PopID();

                ImGui.BeginTabBar("Tab bar!###Tabs" + Entity.Guid);
                
                if(ImGui.BeginTabItem("Stats"))
                {
                    if(Entity.HasDataBlob<MassVolumeDB>())
                    {
                        ImGui.Text("Volume: " + Stringify.Volume(Entity.GetDataBlob<MassVolumeDB>().Volume_m3));
                        ImGui.Text("Mass: " + Stringify.Mass(Entity.GetDataBlob<MassVolumeDB>().MassTotal));
                    }

                    if(Entity.HasDataBlob<PositionDB>())
                    {
                        Entity parent = Entity.GetDataBlob<PositionDB>().Parent;
                        if(parent != null)
                        {
                            ImGui.Text("Orbiting: ");
                            ImGui.SameLine();
                            if(ImGui.SmallButton(parent.GetName(_uiState.Faction.Guid)))
                            {
                                _uiState.EntityClicked(parent.Guid, _uiState.SelectedStarSysGuid, MouseButtons.Primary);
                            }
                        }
                    }

                    ImGui.EndTabItem();
                }

                foreach(var db in Entity.DataBlobs)
                {
                    if(db is StarInfoDB && ImGui.BeginTabItem("Star Info"))
                    {
                        ((StarInfoDB)db).Display(EntityState, _uiState);
                        ImGui.EndTabItem();
                    }
                    if(db is ComponentInstancesDB && ImGui.BeginTabItem("Components"))
                    {
                        ((ComponentInstancesDB)db).Display(EntityState, _uiState);
                        ImGui.EndTabItem();
                    }
                }

                ImGui.EndTabBar();
                ImGui.End();
            }
        }
    }
}