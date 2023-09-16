using System.Linq;
using ImGuiNET;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public static class ComponentInstancesDBDisplay
    {
        public static void Display(this ComponentInstancesDB db, EntityState entityState, GlobalUIState uiState)
        {
            if(ImGui.BeginTable("InstallationTable", 3, Styles.TableFlags))
            {
                ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.None, 1f);
                ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.None, 0.25f);
                ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.None, 1f);
                ImGui.TableHeadersRow();

                // FIXME: we should probably not do this every frame
                var sortedData = db.ComponentsByDesign.OrderBy(entry => entry.Value.First().Name).ToDictionary(entry => entry.Key, entry => entry.Value);
                foreach(var (designID, listPerDesign) in sortedData)
                {
                    if(listPerDesign.Count == 0) continue;

                    ImGui.TableNextColumn();
                    ImGui.Text(listPerDesign[0].Name);
                    if(ImGui.IsItemHovered())
                    {
                        ImGui.SetNextWindowSize(Styles.ToolTipsize);
                        ImGui.BeginTooltip();
                        ImGui.Text(listPerDesign[0].Name);
                        if(listPerDesign[0].Design.TypeName.IsNotNullOrEmpty() && !listPerDesign[0].Design.TypeName.Equals(listPerDesign[0].Name))
                        {
                            var size = ImGui.GetContentRegionAvail();
                            var textSize = ImGui.CalcTextSize(listPerDesign[0].Design.TypeName);
                            ImGui.SameLine();
                            ImGui.SetCursorPosX(size.X - textSize.X);
                            ImGui.PushStyleColor(ImGuiCol.Text, Styles.HighlightColor);
                            ImGui.Text(listPerDesign[0].Design.TypeName);
                            ImGui.PopStyleColor();
                        }
                        if(listPerDesign[0].Design.Description.IsNotNullOrEmpty())
                        {
                            ImGui.Separator();
                            ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                            ImGui.TextWrapped(listPerDesign[0].Design.Description);
                            ImGui.PopStyleColor();
                        }
                        ImGui.EndTooltip();
                    }
                    ImGui.TableNextColumn();
                    ImGui.Text(listPerDesign.Count.ToString());
                    ImGui.TableNextColumn();

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
                }
                ImGui.EndTable();
            }
        }
    }
}