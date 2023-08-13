using System;
using ImGuiNET;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public class EntityWindow : PulsarGuiWindow
    {
        public Entity Entity { get; private set; }
        public EntityState EntityState { get; private set; }
        public String Title { get; private set; }

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
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(264, 325), ImGuiCond.Once);
            if (ImGui.Begin(Title + " (" + EntityState.BodyType.ToDescription() + ")###" + Entity.Guid, ref IsActive, _flags))
            {
                ImGui.BeginTabBar("Tab bar!");

                foreach(var db in Entity.DataBlobs)
                {
                    if(db is StarInfoDB)
                    {
                        StarInfoDB starInfo = (StarInfoDB)db;
                        if(ImGui.BeginTabItem("Star Info"))
                        {
                            starInfo.Display(EntityState, _uiState);
                            ImGui.EndTabItem();
                        }
                    }
                }

                ImGui.EndTabBar();
                ImGui.End();
            }
        }
    }
}