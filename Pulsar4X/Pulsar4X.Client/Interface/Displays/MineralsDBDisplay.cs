using System;
using System.Linq;
using ImGuiNET;

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

    }
}
