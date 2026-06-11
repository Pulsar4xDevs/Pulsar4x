using System;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Api;

namespace Pulsar4X.Client
{
    /// <summary>Snapshot-based mining overview: mine count plus the per-mineral table joining
    /// deposits, stockpile and production (all pre-joined server-side in <see cref="ColonyMiningView"/>).</summary>
    public static class ColonyMiningDisplay
    {
        public static void Display(this ColonyMiningView mining)
        {
            Vector2 topSize = ImGui.GetContentRegionAvail();
            if(ImGui.BeginChild("NumberOfMines", new Vector2(topSize.X, 28f), ImGuiChildFlags.Borders, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                ImGui.Text("Number of Mines:");
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("You can build more mines on this colony using the Production tab.");
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.HighlightColor);
                ImGui.Text(mining.NumberOfMines.ToString());
                ImGui.PopStyleColor();
            }
            ImGui.EndChild();

            if(ImGui.BeginTable("###MineralTable", 6, ImGuiTableFlags.BordersV | ImGuiTableFlags.BordersOuterH | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("Mineral");
                ImGui.TableSetupColumn("Stockpile");
                ImGui.TableSetupColumn("Available to Mine");
                ImGui.TableSetupColumn("Accessibility");
                ImGui.TableSetupColumn("Annual Production");
                ImGui.TableSetupColumn("Years to Depletion");
                ImGui.TableHeadersRow();

                foreach(var mineral in mining.Minerals)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text(mineral.Name);
                    if(ImGui.IsItemHovered())
                        DisplayHelpers.DescriptiveTooltip(mineral.Name, "Mineral", mineral.Description);
                    ImGui.TableNextColumn();
                    ImGui.Text(mineral.Stockpile?.ToString("#,###,###,###,###,###,##0") ?? "Unavailable");
                    if(ImGui.IsItemHovered())
                        ImGui.SetTooltip("Amount of " + mineral.Name + " available for use in the colony stockpile.");

                    ImGui.TableNextColumn();
                    ImGui.Text(mineral.AvailableToMine?.ToString("#,###,###,###,###,###,##0") ?? "N/A");
                    if(ImGui.IsItemHovered())
                        ImGui.SetTooltip("Amount of " + mineral.Name + " available that can be mined from this colony.");
                    ImGui.TableNextColumn();
                    ImGui.Text(mineral.Accessibility.ToString("0.00"));
                    if(ImGui.IsItemHovered())
                        ImGui.SetTooltip("How easy it is to mine " + mineral.Name + " from this colony.\n\n1.0 = easiest\n0.0 = hardest");
                    ImGui.TableNextColumn();
                    if(mineral.CanMine)
                    {
                        ImGui.Text(mineral.AnnualProduction.ToString("#,###,###"));
                        if(ImGui.IsItemHovered())
                            ImGui.SetTooltip("Annual production of " + mineral.Name + " from this colony.");
                    }
                    else
                    {
                        ImGui.Text("-");
                        if(ImGui.IsItemHovered())
                            ImGui.SetTooltip("This colony is currently unable to mine " + mineral.Name + ".");
                    }
                    ImGui.TableNextColumn();
                    if(mineral.AnnualProduction > 0)
                    {
                        var amount = mineral.AvailableToMine ?? 0;
                        string yearsToDepletion = Math.Round((double)amount / (double)mineral.AnnualProduction, 4).ToString("#.0");
                        ImGui.Text(yearsToDepletion);
                        if(ImGui.IsItemHovered())
                            ImGui.SetTooltip("The colony will exhaust the available " + mineral.Name + " in " + yearsToDepletion + " years.");
                    }
                    else
                    {
                        ImGui.Text("-");
                    }
                }

                ImGui.EndTable();

                if(mining.Minerals.Count == 0)
                {
                    ImGui.Text("No minerals available.");
                }
            }
        }
    }
}
