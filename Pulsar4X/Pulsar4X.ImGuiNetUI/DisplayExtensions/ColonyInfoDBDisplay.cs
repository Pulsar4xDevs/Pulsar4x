using ImGuiNET;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public static class ColonyInfoDBDisplay
    {
        public static void Display(this ColonyInfoDB colony, EntityState entityState, GlobalUIState uiState)
        {
            ImGui.PushID("###Population " + entityState.Entity.Guid);
            if(ImGui.CollapsingHeader("Population", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Columns(2);
                ImGui.Text("Species");
                ImGui.NextColumn();
                ImGui.Text("Quantity");
                ImGui.NextColumn();
                ImGui.Separator();

                foreach(var (species, population) in colony.Population)
                {
                    ImGui.Text(species.GetDefaultName());
                    ImGui.NextColumn();
                    ImGui.Text(Stringify.Quantity(population, "0.##", true));
                    ImGui.NextColumn();
                }
            }
            ImGui.PopID();
        }
    }
}