using ImGuiNET;
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

    }
}
