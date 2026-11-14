using System;
using System.Globalization;
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
                ImGui.TableSetupColumn("Yours / Body");
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
                    ImGui.Text(FormatAnnualProduction(mineral));
                    if(ImGui.IsItemHovered())
                        ImGui.SetTooltip(ProductionTooltip(mineral));
                    ImGui.TableNextColumn();
                    var years = FormatYearsToDepletion(mineral.YearsToDepletion);
                    ImGui.Text(years);
                    if(mineral.YearsToDepletion != null && ImGui.IsItemHovered())
                        ImGui.SetTooltip("Every colony on this body will exhaust the available " + mineral.Name + " in " + years + " years.");
                }

                ImGui.EndTable();

                if(mining.Minerals.Count == 0)
                {
                    ImGui.Text("No minerals available.");
                }
            }
        }

        /// <summary>This colony's annual production and the body's total, or "-" when nobody mines it.</summary>
        public static string FormatAnnualProduction(MineralMiningRow mineral)
        {
            if (!mineral.CanMine && mineral.BodyAnnualProduction <= 0)
                return "-";
            return Quantity(mineral.AnnualProduction) + " / " + Quantity(mineral.BodyAnnualProduction);
        }

        /// <summary>"-" when the body is unmined or the amount is unsurveyed; otherwise years at the body total.</summary>
        public static string FormatYearsToDepletion(double? years)
        {
            if (years is not double value)
                return "-";
            if (value < 1)
                return Math.Round(value, 3).ToString("0.000", CultureInfo.InvariantCulture);
            return Math.Round(value, 4).ToString("#0.0", CultureInfo.InvariantCulture);
        }

        static string Quantity(long amount) => amount.ToString("#,##0", CultureInfo.InvariantCulture);

        static string ProductionTooltip(MineralMiningRow mineral)
        {
            if (!mineral.CanMine && mineral.BodyAnnualProduction <= 0)
                return "This colony is currently unable to mine " + mineral.Name + ".";
            return "This colony produces " + Quantity(mineral.AnnualProduction) + " of " + mineral.Name
                + " per year. Every colony on this body produces " + Quantity(mineral.BodyAnnualProduction) + ".";
        }
    }
}
