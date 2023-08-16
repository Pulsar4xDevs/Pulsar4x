using System.Linq;
using ImGuiNET;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public static class EntityDisplay
    {
        public static void DisplayMining(this Entity entity, GlobalUIState uiState)
        {
            var mineralStaticInfo = uiState.Game.StaticData.CargoGoods.GetMineralsList();
            var minerals = entity.GetDataBlob<ColonyInfoDB>().PlanetEntity.GetDataBlob<MineralsDB>().Minerals;
            var storage = entity.GetDataBlob<VolumeStorageDB>().TypeStores;

            ImGui.Text("Number of Mines: " + 1); // TODO: add the actual number of mines

            if(ImGui.BeginTable("###MineralTable" + entity.Guid, 6, ImGuiTableFlags.BordersV | ImGuiTableFlags.BordersOuterH | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Mineral");
                ImGui.TableSetupColumn("Quantity (tons)");
                ImGui.TableSetupColumn("Accessibility");
                ImGui.TableSetupColumn("Annual Production");
                ImGui.TableSetupColumn("Years to Depletion");
                ImGui.TableSetupColumn("Stockpile (tons)");
                ImGui.TableHeadersRow();

                foreach(var (id, mineral) in minerals)
                {
                    var mineralData = mineralStaticInfo.FirstOrDefault(x => x.ID == id);
                    var stockpileData = storage.FirstOrDefault(x => x.Value.CurrentStoreInUnits.ContainsKey(id)).Value;
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
                        ImGui.Text(""); // TODO: add annual production
                        ImGui.TableNextColumn();
                        ImGui.Text(""); // TODO: add years to depletion
                        ImGui.TableNextColumn();
                        if(stockpileData != null)
                        {
                            ImGui.Text(stockpileData.CurrentStoreInUnits[id].ToString("#,###,###,###,###,###,##0"));
                        }
                        else
                        {
                            ImGui.Text("0");
                        }
                    }
                }

                ImGui.EndTable();
            }
        }
    }
}