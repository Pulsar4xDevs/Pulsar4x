using ImGuiNET;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;
using Pulsar4X.Colonies;
using Stringify = Pulsar4X.Api.Stringify;

namespace Pulsar4X.Client
{
    public static class ColonyInfoDBDisplay
    {
        /// <summary>Snapshot-based population display for UI ported to the API galaxy model.</summary>
        public static void Display(this Pulsar4X.Api.ColonyView colony, int entityId)
        {
            ImGui.PushID("###Population " + entityId);
            ImGui.Columns(1);
            if(ImGui.CollapsingHeader("Population", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Columns(2);

                foreach(var species in colony.SpeciesPopulations)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                    ImGui.Text(species.SpeciesName);
                    ImGui.PopStyleColor();
                    ImGui.NextColumn();
                    ImGui.Text(Stringify.Quantity(species.Population, "0.##", true));
                    ImGui.NextColumn();
                }

                ImGui.Columns(1);
            }
            ImGui.PopID();
        }

        public static void Display(this ColonyInfoDB colony, EntityState entityState, GlobalUIState uiState)
        {
            if(uiState.Game == null) return;

            ImGui.PushID("###Population " + entityState.Id);
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