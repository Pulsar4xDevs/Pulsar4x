using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
using ImGuiNET;
using Pulsar4X.Api;

namespace Pulsar4X.Client
{
    /// <summary>
    /// Appraisal popup: a cargo good or component design's recipe cost at current market prices,
    /// compared with a listing ask when one is given. Opened from the market or the Production tab.
    /// Formatters and resolvers are pure for tests.
    /// </summary>
    public static class MarketAppraisalDisplay
    {
        public static readonly int[] ScaleFactors = { 1, 100, 1000 };

        static readonly Vector4 WindowBg = new(0.08f, 0.08f, 0.10f, 1f);
        static readonly Vector4 RowBg = new(0.08f, 0.08f, 0.10f, 1f);
        static readonly Vector4 RowBgAlt = new(0.11f, 0.11f, 0.14f, 1f);
        static readonly Vector4 HeaderBg = new(0.12f, 0.14f, 0.18f, 1f);

        static GoodAppraisal? _shown;
        static decimal? _ask;
        static bool _open;

        public static string Money(decimal? amount)
        {
            if (amount is not decimal value)
                return "—";
            if (value == 0)
                return "0 cr";
            // "0.##" hid labour like 0.000625 cr by rounding it to 0.00. Keep ordinary
            // values short, but give small non-zero amounts enough significant figures.
            return value.ToString("0.########", CultureInfo.InvariantCulture) + " cr";
        }

        /// <summary>
        /// One line's quantity. Takes a <see cref="double"/> so it works with the current
        /// integer field and a later fractional (double or decimal) field via a cast.
        /// </summary>
        public static string Quantity(double quantity)
        {
            if (quantity == 0)
                return "0";
            return quantity.ToString("0.########", CultureInfo.InvariantCulture);
        }

        /// <summary>Linear scale of a per-1 appraisal cost. Null stays null.</summary>
        public static decimal? ScaleCost(decimal? amount, int scale)
            => amount is decimal value ? value * scale : null;

        public static string ScaledMoney(decimal? amount, int scale)
            => Money(ScaleCost(amount, scale));

        public static string ScaledQuantity(double quantity, int scale)
            => Quantity(quantity * scale);

        public static string ColumnHeader(int scale)
        {
            if (scale == 1)
                return "Per 1";
            if (scale == 1000)
                return "Per 1,000";
            return "Per " + scale.ToString("N0", CultureInfo.InvariantCulture);
        }

        /// <summary>Input line cell: scaled quantity and scaled line cost.</summary>
        public static string ScaledInputCell(double quantity, decimal? lineCost, int scale)
            => ScaledQuantity(quantity, scale) + "  ·  " + ScaledMoney(lineCost, scale);

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
            string qty = Quantity((double)line.Quantity);
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

        /// <summary>
        /// A component constructible (not a ship design or a processed material).
        /// </summary>
        public static bool IsComponentDesign(ConstructibleItemView item)
            => !item.IsShipDesign && !item.IsMaterial;

        /// <summary>
        /// Resolves a cargo or component UniqueID to the id and display name the panel should use.
        /// Looks up <paramref name="designs"/> first, then constructible rows that share that UniqueID.
        /// </summary>
        public static bool TryResolve(
            string cargoOrComponentId,
            ComponentDesignsSnapshot? designs,
            IEnumerable<ConstructibleItemView>? constructibles,
            out string id,
            out string name)
        {
            id = "";
            name = "";
            if (string.IsNullOrEmpty(cargoOrComponentId))
                return false;

            if (designs?.Designs != null)
            {
                foreach (var design in designs.Designs)
                {
                    if (design.Id == cargoOrComponentId)
                    {
                        id = design.Id;
                        name = string.IsNullOrEmpty(design.Name) ? design.Id : design.Name;
                        return true;
                    }
                }
            }

            if (constructibles != null)
            {
                foreach (var item in constructibles)
                {
                    if (item.DesignId == cargoOrComponentId)
                    {
                        id = item.DesignId;
                        name = string.IsNullOrEmpty(item.Name) ? item.DesignId : item.Name;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Resolves a production-line job to a component UniqueID and name when exactly one
        /// component constructible on the line shares the job's name. Jobs have no design id on the view.
        /// </summary>
        public static bool TryResolveJob(
            IndustryJobView job,
            IEnumerable<ConstructibleItemView> constructibles,
            out string id,
            out string name)
        {
            id = "";
            name = "";
            if (job == null || string.IsNullOrEmpty(job.Name) || constructibles == null)
                return false;

            ConstructibleItemView? match = null;
            foreach (var item in constructibles)
            {
                if (!IsComponentDesign(item) || item.Name != job.Name)
                    continue;
                if (match != null)
                    return false;
                match = item;
            }
            if (match == null || string.IsNullOrEmpty(match.DesignId))
                return false;
            id = match.DesignId;
            name = string.IsNullOrEmpty(match.Name) ? match.DesignId : match.Name;
            return true;
        }

        /// <summary>
        /// Opens the single appraisal panel for a cargo good or component design.
        /// A later open replaces the previous one. One unit is fetched; Per 100 / Per 1,000 are scaled on the client.
        /// </summary>
        public static void Open(int colonyId, string cargoOrComponentId, string name, IGameClient? client, decimal? ask = null)
        {
            if (string.IsNullOrEmpty(cargoOrComponentId))
                return;
            _shown = client?.AppraiseGood(colonyId, cargoOrComponentId, 1)
                ?? GoodAppraisal.None(cargoOrComponentId, name, 1, "not connected");
            _ask = ask;
            _open = true;
        }

        public static void DrawButton(int colonyId, string cargoId, string name, decimal? ask, IGameClient? client)
        {
            if (cargoId.Length == 0)
                return;
            if (ImGui.Button("Appraise"))
                Open(colonyId, cargoId, name, client, ask);
        }

        public static void DrawSmallButton(int colonyId, string cargoId, string name, decimal? ask, IGameClient? client)
        {
            if (cargoId.Length == 0)
                return;
            if (ImGui.SmallButton("Appraise"))
                Open(colonyId, cargoId, name, client, ask);
        }

        /// <summary>Top-level window so the panel is not clipped inside the market detail child.</summary>
        public static void DrawWindow()
        {
            if (!_open || _shown == null)
                return;
            bool open = true;
            ImGui.SetNextWindowPos(new Vector2(500f, 190f), ImGuiCond.Appearing);
            ImGui.SetNextWindowSize(new Vector2(560f, 360f), ImGuiCond.Appearing);
            ImGui.SetNextWindowSizeConstraints(new Vector2(480f, 120f), new Vector2(800f, 700f));
            ImGui.SetNextWindowBgAlpha(1f);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, WindowBg);
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
            DrawCostGrid(appraisal, ask);
        }

        static void DrawCostGrid(GoodAppraisal appraisal, decimal? ask)
        {
            string[] headers = { ColumnHeader(1), ColumnHeader(100), ColumnHeader(1000) };
            string[] labels = ask != null
                ? new[] { "Unit cost", "Inputs", "Energy", "Labour", "Total", "Ask" }
                : new[] { "Unit cost", "Inputs", "Energy", "Labour", "Total" };
            decimal?[] costs = ask != null
                ? new[] { appraisal.UnitCost, appraisal.InputCost, appraisal.EnergyCost, appraisal.LabourCost, appraisal.TotalCost, ask }
                : new[] { appraisal.UnitCost, appraisal.InputCost, appraisal.EnergyCost, appraisal.LabourCost, appraisal.TotalCost };

            float labelW = TextWidth("Unit cost");
            foreach (var label in labels)
                labelW = Math.Max(labelW, TextWidth(label));
            var inputs = appraisal.Inputs;
            if (inputs != null)
            {
                foreach (var line in inputs)
                {
                    string name = (line.Name ?? line.CargoId) + "  (" + LineMarker(line) + ")";
                    labelW = Math.Max(labelW, TextWidth(name));
                }
            }

            float[] colW = new float[3];
            for (int c = 0; c < 3; c++)
            {
                int scale = ScaleFactors[c];
                float w = TextWidth(headers[c]);
                foreach (var cost in costs)
                    w = Math.Max(w, TextWidth(ScaledMoney(cost, scale)));
                if (inputs != null)
                {
                    foreach (var line in inputs)
                    {
                        w = Math.Max(w, TextWidth(ScaledInputCell((double)line.Quantity, line.LineCost, scale)));
                    }
                }
                colW[c] = w;
            }

            ImGui.PushStyleColor(ImGuiCol.TableRowBg, RowBg);
            ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, RowBgAlt);
            ImGui.PushStyleColor(ImGuiCol.TableHeaderBg, HeaderBg);
            ImGui.PushStyleColor(ImGuiCol.ChildBg, WindowBg);
            var flags = ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit
                | ImGuiTableFlags.NoHostExtendX;
            if (ImGui.BeginTable("##appraisal-grid", 4, flags))
            {
                ImGui.TableSetupColumn("##label", ImGuiTableColumnFlags.WidthFixed, labelW);
                for (int c = 0; c < 3; c++)
                    ImGui.TableSetupColumn(headers[c], ImGuiTableColumnFlags.WidthFixed, colW[c]);
                ImGui.TableHeadersRow();
                for (int r = 0; r < labels.Length; r++)
                    CostRow(labels[r], costs[r]);

                if (inputs != null && inputs.Count > 0)
                {
                    foreach (var line in inputs)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted((line.Name ?? line.CargoId) + "  (" + LineMarker(line) + ")");
                        for (int c = 0; c < 3; c++)
                        {
                            ImGui.TableNextColumn();
                            ImGui.TextUnformatted(ScaledInputCell((double)line.Quantity, line.LineCost, ScaleFactors[c]));
                        }
                    }
                }
                ImGui.EndTable();
            }
            ImGui.PopStyleColor(4);
        }

        static void CostRow(string label, decimal? amount)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextColored(Styles.DescriptiveColor, label);
            for (int c = 0; c < 3; c++)
            {
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(ScaledMoney(amount, ScaleFactors[c]));
            }
        }

        static float TextWidth(string text)
        {
            return ImGui.CalcTextSize(text).X + ImGui.GetStyle().CellPadding.X * 2f;
        }
    }
}
