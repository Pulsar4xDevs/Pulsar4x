using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public static class EntityDisplay
    {
        public static void DisplaySummary(this Entity entity, EntityState entityState, GlobalUIState uiState)
        {
            Vector2 windowContentSize = ImGui.GetContentRegionAvail();
            if(ImGui.BeginChild("ColonySummary1", new Vector2(windowContentSize.X * 0.5f, windowContentSize.Y), true))
            {
                if(ImGui.CollapsingHeader("Parent Information", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    var colonyInfoDb = entity.GetDataBlob<ColonyInfoDB>();
                    var bodyInfoDb = colonyInfoDb.PlanetEntity.GetDataBlob<SystemBodyInfoDB>();

                    ImGui.Columns(2);
                    ImGui.Text("Name");
                    ImGui.NextColumn();
                    if(ImGui.SmallButton(colonyInfoDb.PlanetEntity.GetDefaultName()))
                    {
                        // TODO: open the EntityWindow for the parent
                    }
                    ImGui.NextColumn();
                    ImGui.Separator();
                    PrintRow("Type", bodyInfoDb.BodyType.ToDescription());
                    PrintRow("Tectonic Activity", bodyInfoDb.Tectonics.ToDescription());
                    PrintRow("Gravity", bodyInfoDb.Gravity.ToString("#"));
                    PrintRow("Temperature", bodyInfoDb.BaseTemperature.ToString("#.#") + " C");
                    PrintRow("Length of Day", bodyInfoDb.LengthOfDay.ToString("hh") + " hours");
                    PrintRow("Tilt", bodyInfoDb.AxialTilt.ToString("#"));
                    PrintRow("Magnetic Field", bodyInfoDb.MagneticField.ToString("#"));
                    PrintRow("Radiation Level", bodyInfoDb.RadiationLevel.ToString("#"));
                    PrintRow("Atmospheric Dust", bodyInfoDb.AtmosphericDust.ToString("#"), false);
                }
                ImGui.Columns(1);
                entity.GetDataBlob<ColonyInfoDB>().Display(entityState, uiState);
                ImGui.EndChild();
            }
            ImGui.SameLine();
            if(ImGui.BeginChild("ColonySummary2", new Vector2(windowContentSize.X * 0.5f - 8f, windowContentSize.Y), true))
            {
                ImGui.Text("Some summary stuff");
                ImGui.EndChild();
            }
        }

        public static void DisplayIndustry(this Entity entity, GlobalUIState uiState)
        {
            ImGui.Text("Industry");
        }
        public static void DisplayMining(this Entity entity, GlobalUIState uiState)
        {
            var mineralStaticInfo = uiState.Game.StaticData.CargoGoods.GetMineralsList();
            var minerals = entity.GetDataBlob<ColonyInfoDB>().PlanetEntity.GetDataBlob<MineralsDB>()?.Minerals;
            var miningRates = entity.GetDataBlob<MiningDB>()?.ActualMiningRate;
            var storage = entity.GetDataBlob<VolumeStorageDB>()?.TypeStores;

            Vector2 topSize = ImGui.GetContentRegionAvail();
            if(ImGui.BeginChild("NumberOfMines" + entity.Guid, new Vector2(topSize.X, 28f), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                if(entity.HasDataBlob<MiningDB>())
                {
                    ImGui.Text("Number of Mines: " + entity.GetDataBlob<MiningDB>().NumberOfMines);
                }
                else
                {
                    ImGui.Text("Number of Mines: 0");
                }
                ImGui.EndChild();
            }

            if(ImGui.BeginTable("###MineralTable" + entity.Guid, 6, ImGuiTableFlags.BordersV | ImGuiTableFlags.BordersOuterH | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Mineral");
                ImGui.TableSetupColumn("Quantity (tons)");
                ImGui.TableSetupColumn("Accessibility");
                ImGui.TableSetupColumn("Annual Production");
                ImGui.TableSetupColumn("Years to Depletion");
                ImGui.TableSetupColumn("Stockpile (tons)");
                ImGui.TableHeadersRow();

                if(minerals == null) minerals = new Dictionary<Guid, MineralDeposit>();

                foreach(var (id, mineral) in minerals)
                {
                    var mineralData = mineralStaticInfo.FirstOrDefault(x => x.ID == id);

                    if(mineralData == null) continue;

                    var stockpileData = storage?.FirstOrDefault(x => x.Value.CurrentStoreInUnits.ContainsKey(id)).Value;
                    var annualProduction = miningRates.ContainsKey(id) ? 365 * miningRates[id] : 0;

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text(mineralData.Name);
                    ImGui.TableNextColumn();
                    ImGui.Text(mineral.Amount.ToString("#,###,###,###,###,###,##0"));
                    ImGui.TableNextColumn();
                    ImGui.Text(mineral.Accessibility.ToString("0.00"));
                    ImGui.TableNextColumn();
                    if(miningRates.ContainsKey(id))
                    {
                        ImGui.Text(annualProduction.ToString("#,###,###"));
                    }
                    else
                    {
                        ImGui.Text("-");
                    }
                    ImGui.TableNextColumn();
                    if(annualProduction > 0)
                    {
                        ImGui.Text(Math.Round((double)mineral.Amount / (double)annualProduction, 4).ToString("#.0"));
                    }
                    else
                    {
                        ImGui.Text("-");
                    }
                    ImGui.TableNextColumn();
                    if(stockpileData != null)
                    {
                        ImGui.Text(stockpileData.CurrentStoreInUnits[id].ToString("#,###,###,###,###,###,##0"));
                    }
                    else
                    {
                        if(storage == null)
                            ImGui.Text("Unavailable");
                        else
                            ImGui.Text("0");
                    }
                }

                ImGui.EndTable();

                if(minerals.Count == 0)
                {
                    ImGui.Text("No minerals available.");
                }
            }
        }

        public static void DisplayLogistics(this Entity entity, EntityState entityState, GlobalUIState uiState)
        {
            ColonyLogisticsDisplay.GetInstance(StaticRefLib.StaticData, entityState).Display();
        }

        private static void PrintRow(string one, string two, bool separator = true)
        {
            ImGui.Text(one); ImGui.NextColumn(); ImGui.Text(two); ImGui.NextColumn();
            if(separator)
                ImGui.Separator();
        }
    }
}