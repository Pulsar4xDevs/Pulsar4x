using System.Linq;
using ImGuiNET;

namespace Pulsar4X.Client
{
    public static class ComponentInstancesDBDisplay
    {
        /// <summary>Snapshot-based installations display for UI ported to the API galaxy model.
        /// <paramref name="holderId"/> is the colony/ship the installations belong to (the command target).</summary>
        public static void Display(this Pulsar4X.Api.InstallationsView view, int holderId, GlobalUIState uiState)
        {
            if(ImGui.BeginTable("InstallationTable", 3, Styles.TableFlags | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.None, 0.45f);
                ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.None, 0.1f);
                ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.None, 0.45f);
                ImGui.TableHeadersRow();

                foreach(var group in view.Installations)
                {
                    ImGui.TableNextColumn();
                    ImGui.Text(group.Name);
                    AddContextMenu(group, holderId, uiState);
                    DisplayHelpers.DescriptiveTooltip(group.Name, group.TemplateName, group.Description, null, true);
                    ImGui.TableNextColumn();
                    ImGui.Text(group.Count.ToString());
                    ImGui.TableNextColumn();

                    if(group.OperationalCount > 0 && group.OperationalCount < group.Count)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, Styles.OkColor);
                        ImGui.Text("Degraded");
                        ImGui.PopStyleColor();
                    }
                    else if(group.OperationalCount == 0)
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
                }
                ImGui.EndTable();
            }
        }

        private static void AddContextMenu(Pulsar4X.Api.InstallationGroup group, int holderId, GlobalUIState uiState)
        {
            ImGui.PushID(group.DesignId);
            if(ImGui.BeginPopupContextItem("###" + group.DesignId))
            {
                ImGui.Text(group.Name);
                ImGui.Separator();
                if(group.CanStore && ImGui.MenuItem("Move to Storage"))
                {
                    uiState.GameClient?.SubmitCommandAsync(
                        new Pulsar4X.Api.UninstallComponentCommand(holderId, group.DesignId));
                }
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.TerribleColor);
                if(ImGui.MenuItem("Destroy"))
                {

                }
                ImGui.PopStyleColor();
                ImGui.EndPopup();
            }
            ImGui.PopID();
        }

    }
}
