using ImGuiNET;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public static class ComponentInstancesDBDisplay
    {
        public static void Display(this ComponentInstancesDB db, EntityState entityState, GlobalUIState uiState)
        {
            ImGui.Columns(3); 
            ImGui.SetColumnWidth(0, 164);
            ImGui.Text("Type");
            ImGui.NextColumn();
            ImGui.SetColumnWidth(1, 62);
            ImGui.Text("Health");
            ImGui.NextColumn();
            ImGui.SetColumnWidth(2, 62);
            ImGui.Text("Enabled");
            ImGui.NextColumn();

            foreach(var listPerDesign in db.ComponentsByDesign.Values)
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
        }
    }
}