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

                foreach(var (species, population) in colony.Population)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                    ImGui.Text(species.GetDefaultName());
                    ImGui.PopStyleColor();
                    ImGui.NextColumn();
                    ImGui.Text(Stringify.Quantity(population, "0.##", true));
                    ImGui.NextColumn();
                }
            }
            ImGui.PopID();
        }
    }
}