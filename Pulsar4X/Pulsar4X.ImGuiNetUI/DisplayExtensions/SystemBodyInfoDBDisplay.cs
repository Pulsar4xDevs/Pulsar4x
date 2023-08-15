using System.Linq;
using ImGuiNET;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public static class SystemBodyInfoDBDisplay
    {
        public static void Display(this SystemBodyInfoDB systemBodyInfo, EntityState entityState, GlobalUIState uiState)
        {
            var minerals = uiState.Game.StaticData.CargoGoods.GetMineralsList();

            if(ImGui.BeginTable("###MineralTable" + entityState.Entity.Guid, 4))
            {
                ImGui.TableSetupColumn("Name");
                ImGui.TableSetupColumn("Amount");
                ImGui.TableSetupColumn("Accessibility");
                ImGui.TableSetupColumn("Mining Rate");
                ImGui.TableHeadersRow();

                foreach(var (id, mineral) in systemBodyInfo.Minerals)
                {
                    var mineralData = minerals.FirstOrDefault(x => x.ID == id);
                    if (mineralData != null)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.Text(mineralData.Name);
                        ImGui.TableNextColumn();
                        ImGui.Text(mineral.Amount.ToString("#,###,###,###,###,###,##0"));
                        ImGui.TableNextColumn();
                        ImGui.Text(mineral.Accessibility.ToString("0.00"));
                        ImGui.TableNextColumn();
                        ImGui.Text(""); // TODO: add this
                    }
                }

                ImGui.EndTable();
            }
        }
    }
}