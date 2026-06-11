using System;
using System.Linq;
using ImGuiNET;
using Pulsar4X.Factions;
using Pulsar4X.Industry;

namespace Pulsar4X.Client
{
    public static class MineralsDBDisplay
    {
        /// <summary>Snapshot-based mineral-deposit display for UI ported to the API galaxy model
        /// (amounts arrive pre-masked/obscured from the server).</summary>
        public static void Display(this Pulsar4X.Api.MineralDepositsView deposits, int entityId)
        {
            if(ImGui.BeginTable("###MineralTable" + entityId, 3, ImGuiTableFlags.BordersV | ImGuiTableFlags.BordersOuterH | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Name");
                ImGui.TableSetupColumn("Amount");
                ImGui.TableSetupColumn("Accessibility");
                ImGui.TableHeadersRow();

                foreach(var deposit in deposits.Deposits)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text(deposit.Name);
                    ImGui.TableNextColumn();
                    switch (deposit.Access)
                    {
                        case Pulsar4X.Api.DepositAccess.None:
                            ImGui.Text("Uknown");
                            break;
                        case Pulsar4X.Api.DepositAccess.Partial:
                            ImGui.Text("~" + deposit.Amount.ToString("#,###,###,###,###,###,##0"));
                            break;
                        case Pulsar4X.Api.DepositAccess.Full:
                            ImGui.Text(deposit.Amount.ToString("#,###,###,###,###,###,##0"));
                            break;
                    }
                    ImGui.TableNextColumn();
                    ImGui.Text(deposit.Accessibility.ToString("0.00"));
                }

                ImGui.EndTable();
            }
        }

        /// <summary>
        /// Obscures a value by applying a deterministic +/- 20% error margin.
        /// The error is consistent for the same input value.
        /// </summary>
        private static long ObscureWithError(long value)
        {
            // Use value's hash to get a deterministic factor between -0.20 and +0.20
            var hash = value.GetHashCode();
            var factor = (hash % 41 - 20) / 100.0;
            return (long)(value * (1 + factor));
        }

        public static void Display(this MineralsDB mineralsDB, EntityState entityState, GlobalUIState uiState)
        {
            var minerals = uiState.Faction.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetMineralsList();

            if(ImGui.BeginTable("###MineralTable" + entityState.Entity.Id, 3, ImGuiTableFlags.BordersV | ImGuiTableFlags.BordersOuterH | ImGuiTableFlags.RowBg))
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
                        var amount = mineral.Amount.Resolve(uiState.FactionMask, ObscureWithError);
                        switch (amount.Access)
                        {
                            case DataStructures.AccessLevel.None:
                                ImGui.Text("Uknown");
                                break;
                            case DataStructures.AccessLevel.Partial:
                                ImGui.Text("~" + amount.Value.ToString("#,###,###,###,###,###,##0"));
                                break;
                            case DataStructures.AccessLevel.Full:
                                ImGui.Text(amount.Value.ToString("#,###,###,###,###,###,##0"));
                                break;
                        }
                        ImGui.TableNextColumn();
                        ImGui.Text(mineral.Accessibility.ToString("0.00"));
                    }
                }

                ImGui.EndTable();
            }
        }
    }
}