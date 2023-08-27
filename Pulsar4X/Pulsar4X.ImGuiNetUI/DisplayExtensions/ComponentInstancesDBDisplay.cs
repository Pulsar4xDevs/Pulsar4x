using System.Linq;
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
            ImGui.SetColumnWidth(1, 48);
            ImGui.Text("#");
            ImGui.NextColumn();
            ImGui.SetColumnWidth(2, 100);
            ImGui.Text("Status");
            ImGui.NextColumn();
            ImGui.Separator();

            // FIXME: we should probably not do this every frame
            var sortedData = db.ComponentsByDesign.OrderBy(entry => entry.Value.First().Name).ToDictionary(entry => entry.Key, entry => entry.Value);
            foreach(var (designID, listPerDesign) in sortedData)
            {
                if(listPerDesign.Count == 0) continue;

                ImGui.Text(listPerDesign[0].Name);
                ImGui.NextColumn();
                ImGui.Text(listPerDesign.Count.ToString());
                ImGui.NextColumn();

                var onCount = listPerDesign.Where(x => x.IsEnabled).Count();
                var offCount = listPerDesign.Where(x => !x.IsEnabled).Count();

                if(onCount > 0 && offCount > 0)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Styles.OkColor);
                    ImGui.Text("Degraded");
                    ImGui.PopStyleColor();
                }
                else if(onCount == 0)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Styles.BadColor);
                    ImGui.Text("Disabled");
                    ImGui.PopStyleColor();
                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Styles.HighlightColor);
                    ImGui.Text("Operational");
                    ImGui.PopStyleColor();
                }

                ImGui.NextColumn();
            }
            ImGui.Columns(1);
        }
    }
}