using System.Linq;
using ImGuiNET;
using Pulsar4X.Datablobs;

namespace Pulsar4X.SDL2UI
{
    public static class MineralsDBDisplay
    {
        public static void Display(this MineralsDB mineralsDB, EntityState entityState, GlobalUIState uiState)
        {
            var minerals = uiState.Faction.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetMineralsList();

            if(ImGui.BeginTable("###MineralTable" + entityState.Entity.Guid, 3, ImGuiTableFlags.BordersV | ImGuiTableFlags.BordersOuterH | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Name");
                ImGui.TableSetupColumn("Amount");
                ImGui.TableSetupColumn("Accessibility");
                ImGui.TableHeadersRow();

                foreach(var (id, mineral) in mineralsDB.Minerals)
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
                    }
                }

                ImGui.EndTable();
            }
        }
    }
}