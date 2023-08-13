using ImGuiNET;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public static class ComponentInstancesDBDisplay
    {
        public static void Display(this ComponentInstancesDB db, EntityState entityState, GlobalUIState uiState)
        {
            var instancesDB = entityState.Entity.GetDataBlob<ComponentInstancesDB>();

            BorderGroup.Begin("Components:");
            ImGui.Columns(3);
            ImGui.SetColumnWidth(0, 164);
            ImGui.SetColumnWidth(1, 42);
            ImGui.SetColumnWidth(2, 42);

            foreach(var listPerDesign in instancesDB.ComponentsByDesign.Values)
            {
                foreach(var instance in listPerDesign)
                {
                    ImGui.Text(instance.Name);

                    ImGui.NextColumn();
                    ImGui.Text((100 * instance.HealthPercent()) + "%%");

                    ImGui.NextColumn();
                    if(instance.IsEnabled)
                    {
                        ImGui.Text("On");
                    }
                    else
                    {
                        ImGui.Text("Off");
                    }

                    ImGui.NextColumn();
                }
            }
            ImGui.Columns(1);
            BorderGroup.End();
        }
    }
}