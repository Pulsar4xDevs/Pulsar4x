using ImGuiNET;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;

namespace Pulsar4X.SDL2UI
{
    public static class ColonyInfoDBDisplay
    {
        public static void Display(this ColonyInfoDB colony, EntityState entityState, GlobalUIState uiState)
        {
            ImGui.PushID("###Population " + entityState.Entity.Id);
            ImGui.Columns(1);
            if(ImGui.CollapsingHeader("Population", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Columns(2);

                foreach(var (species, population) in colony.Population)
                {
                    var speciesEntity = uiState.Game.GlobalManager.GetGlobalEntityById(species);
                    ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                    ImGui.Text(speciesEntity.GetDefaultName());
                    ImGui.PopStyleColor();
                    ImGui.NextColumn();
                    ImGui.Text(Stringify.Quantity(population, "0.##", true));
                    ImGui.NextColumn();
                }

                ImGui.Columns(1);
            }
            ImGui.PopID();
        }
    }
}