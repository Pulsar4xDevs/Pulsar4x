using System;
using System.Globalization;
using System.Numerics;
using System.Text;
using ImGuiNET;
using Pulsar4X.Api;

namespace Pulsar4X.Client
{
    /// <summary>
    /// Market-window appraisal popup (multi-faction phase 9): a good's recipe cost at current market
    /// prices, compared with the listing ask. Formatters are pure for tests.
    /// </summary>
    public static class MarketAppraisalDisplay
    {
        static GoodAppraisal? _shown;
        static decimal? _ask;
        static bool _open;

        public static string Money(decimal? amount)
            => amount is decimal value
                ? value.ToString("0.##", CultureInfo.InvariantCulture) + " cr"
                : "—";

        /// <summary>Unit cost against the current ask. "Unpriced" when the cost fields are null.</summary>
        public static string UnitCostVsAsk(decimal? unitCost, decimal? ask)
        {
            if (unitCost is not decimal cost)
                return ask is decimal listed ? "Unpriced  (ask " + Money(listed) + ")" : "Unpriced";
            if (ask is not decimal a)
                return Money(cost);
            decimal delta = cost - a;
            if (delta == 0)
                return Money(cost) + "  (same as the ask)";
            if (delta < 0)
                return Money(cost) + "  (" + Money(-delta) + " below the ask of " + Money(a) + ")";
            return Money(cost) + "  (" + Money(delta) + " above the ask of " + Money(a) + ")";
        }

        /// <summary>Seed-ask vs a listing on the body, plus a mark when the input was itself estimated.</summary>
        public static string LineMarker(AppraisalLine line)
        {
            string source = line.FromMarket ? "market" : "seed ask";
            return line.Recursed ? source + ", recursed" : source;
        }

        public static string LineText(AppraisalLine line)
        {
            string qty = line.Quantity.ToString("N0", CultureInfo.InvariantCulture);
            return line.Name + "  " + qty + " × " + Money(line.UnitPrice) + " = " + Money(line.LineCost)
                + "  (" + LineMarker(line) + ")";
        }

        public static string MissingText(GoodAppraisal appraisal)
        {
            if (appraisal.MissingPrices == null || appraisal.MissingPrices.Count == 0)
                return "";
            var sb = new StringBuilder();
            for (int i = 0; i < appraisal.MissingPrices.Count; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                sb.Append(appraisal.MissingPrices[i]);
            }
            return sb.ToString();
        }

        public static bool IsPriced(GoodAppraisal appraisal)
            => string.IsNullOrEmpty(appraisal.Reason) && appraisal.UnitCost != null;

        public static void DrawButton(int colonyId, string cargoId, string name, decimal? ask, IGameClient? client)
        {
            if (cargoId.Length == 0)
                return;
            if (ImGui.Button("Appraise"))
            {
                _shown = client?.AppraiseGood(colonyId, cargoId, 1)
                    ?? GoodAppraisal.None(cargoId, name, 1, "not connected");
                _ask = ask;
                _open = true;
            }
        }

        /// <summary>Top-level window so the panel is not clipped inside the market detail child.</summary>
        public static void DrawWindow()
        {
            if (!_open || _shown == null)
                return;
            bool open = true;
            ImGui.SetNextWindowPos(new Vector2(500f, 190f), ImGuiCond.Appearing);
            ImGui.SetNextWindowSize(new Vector2(440f, 320f), ImGuiCond.Appearing);
            ImGui.SetNextWindowSizeConstraints(new Vector2(400f, 120f), new Vector2(700f, 700f));
            ImGui.SetNextWindowBgAlpha(1f);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.08f, 0.08f, 0.10f, 1f));
            ImGui.PushStyleColor(ImGuiCol.TitleBg, new Vector4(0.10f, 0.10f, 0.14f, 1f));
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(0.12f, 0.16f, 0.24f, 1f));
            if (ImGui.Begin("Appraisal", ref open,
                    ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings))
            {
                Draw(_shown, _ask);
            }
            ImGui.End();
            ImGui.PopStyleColor(3);
            if (!open)
                _open = false;
        }

        static void Draw(GoodAppraisal appraisal, decimal? ask)
        {
            string title = "Appraisal: " + (appraisal.Name ?? appraisal.CargoId);
            if (appraisal.Quantity > 1)
                title += "  × " + appraisal.Quantity.ToString("N0", CultureInfo.InvariantCulture);
            ImGui.TextUnformatted(title);

            if (!string.IsNullOrEmpty(appraisal.Reason))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.BadColor);
                ImGui.PushTextWrapPos(ImGui.GetCursorPos().X + ImGui.GetContentRegionAvail().X);
                ImGui.TextUnformatted(appraisal.Reason);
                ImGui.PopTextWrapPos();
                ImGui.PopStyleColor();
            }

            string missing = MissingText(appraisal);
            if (missing.Length > 0)
            {
                ImGui.PushTextWrapPos(ImGui.GetCursorPos().X + ImGui.GetContentRegionAvail().X);
                ImGui.TextUnformatted("Missing prices: " + missing);
                ImGui.PopTextWrapPos();
            }

            if (appraisal.Cycles != null && appraisal.Cycles.Count > 0)
            {
                var sb = new StringBuilder("Cycle: ");
                for (int i = 0; i < appraisal.Cycles.Count; i++)
                {
                    if (i > 0)
                        sb.Append(", ");
                    sb.Append(appraisal.Cycles[i]);
                }
                ImGui.TextUnformatted(sb.ToString());
            }

            ImGui.Spacing();
            LabelValue("Unit cost", UnitCostVsAsk(appraisal.UnitCost, ask));
            LabelValue("Inputs", Money(appraisal.InputCost));
            LabelValue("Energy", Money(appraisal.EnergyCost));
            LabelValue("Labour", Money(appraisal.LabourCost));
            LabelValue("Total", Money(appraisal.TotalCost));

            if (appraisal.Inputs == null || appraisal.Inputs.Count == 0)
                return;
            ImGui.Spacing();
            ImGui.TextUnformatted("Inputs");
            if (ImGui.BeginTable("##appraisal-inputs", 5,
                    Styles.TableFlags | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("Input", ImGuiTableColumnFlags.WidthStretch, 1.4f);
                ImGui.TableSetupColumn("Qty", ImGuiTableColumnFlags.WidthFixed, 50);
                ImGui.TableSetupColumn("Price", ImGuiTableColumnFlags.WidthFixed, 80);
                ImGui.TableSetupColumn("Cost", ImGuiTableColumnFlags.WidthFixed, 80);
                ImGui.TableSetupColumn("Source", ImGuiTableColumnFlags.WidthStretch, 1f);
                ImGui.TableHeadersRow();
                foreach (var line in appraisal.Inputs)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(line.Name ?? line.CargoId);
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(line.Quantity.ToString("N0", CultureInfo.InvariantCulture));
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(Money(line.UnitPrice));
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(Money(line.LineCost));
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(LineMarker(line));
                }
                ImGui.EndTable();
            }
        }

        static void LabelValue(string label, string value)
        {
            ImGui.TextColored(Styles.DescriptiveColor, label);
            ImGui.SameLine();
            ImGui.TextUnformatted(value);
        }
    }
}
