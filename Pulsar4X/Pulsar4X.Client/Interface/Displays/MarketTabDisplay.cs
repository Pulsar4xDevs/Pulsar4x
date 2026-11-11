using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Api;

namespace Pulsar4X.Client
{
    /// <summary>
    /// Market tab: goods list on the left, the selected good's prices, chart, and listing editor on the right.
    /// Relocation listings are not shown here.
    /// </summary>
    public static class MarketTabDisplay
    {
        static readonly Vector4 StockColor = new(0.35f, 0.55f, 0.85f, 0.9f);
        static readonly Vector4 BuyColor = new(0.85f, 0.65f, 0.25f, 0.9f);
        static readonly Vector4 SellColor = new(0.35f, 0.75f, 0.40f, 0.9f);
        static readonly Vector4[] Palette =
        {
            new(0.416f, 0.651f, 0.902f, 1f),
            new(0.851f, 0.659f, 0.290f, 1f),
            new(0.298f, 0.686f, 0.416f, 1f),
            new(0.776f, 0.478f, 0.776f, 1f),
            new(0.902f, 0.549f, 0.404f, 1f),
            new(0.404f, 0.737f, 0.737f, 1f),
            new(0.839f, 0.737f, 0.357f, 1f),
            new(0.639f, 0.639f, 0.902f, 1f),
        };

        sealed class RowEdit
        {
            public int Sell;
            public int Buy;
            public int Reserve;
            public double Ask;
            public double Bid;
        }

        sealed class OfficeUi
        {
            public string Filter = "";
            public string SelectedId = "";
            public bool Compare;
            public readonly HashSet<string> Checked = new();
            public bool Candles = true;
            public bool Index = true;
            public MarketChartPreset Preset = MarketChartPreset.Week;
            public bool CustomRange;
            public DateTime CustomFrom;
            public DateTime CustomTo;
            public bool DateFieldActive;
            public int FromYear;
            public int FromMonth = 1;
            public int FromDay = 1;
            public int ToYear;
            public int ToMonth = 1;
            public int ToDay = 1;
            public readonly Dictionary<string, RowEdit> Rows = new();
            public readonly RowEdit Add = new();
            public string AddFor = "";
        }

        readonly struct Fact
        {
            public Fact(string id, string label, string value, Vector4? color)
            {
                Id = id;
                Label = label;
                Value = value;
                Color = color;
            }

            public string Id { get; }
            public string Label { get; }
            public string Value { get; }
            public Vector4? Color { get; }
        }

        static readonly Dictionary<int, OfficeUi> Ui = new();

        public static void Display(
            EntitySnapshot entity,
            MarketView? market,
            MarketHistoryView? history,
            IntelBookView? book,
            string officeSystemId,
            DateTime now,
            GlobalUIState uiState,
            bool compact)
        {
            if (market == null)
            {
                ImGui.Text("This colony has no logistics office.");
                IntelDisplay.Display(entity, book, officeSystemId, uiState);
                return;
            }

            var ui = Office(entity.Id);
            var listed = new List<MarketGoodView>();
            foreach (var good in market.Goods)
            {
                if (!good.Relocation)
                    listed.Add(good);
            }
            var addable = new List<MarketGoodChoice>();
            foreach (var choice in market.Addable)
            {
                bool already = false;
                foreach (var good in listed)
                {
                    if (good.CargoId == choice.CargoId)
                    {
                        already = true;
                        break;
                    }
                }
                if (!already)
                    addable.Add(choice);
            }
            DropStaleChecks(ui, listed);
            string selectedId = ResolveSelected(ui, listed, addable);
            ui.SelectedId = selectedId;

            ImGui.PushID(entity.Id);
            DrawToolbar(entity, market, ui, uiState, listed);
            if (market.CanEdit && entity.Kind == BodyKind.Colony)
                DrawSupplyStatus(entity);

            // Narrow (the entity card, or a small window): the goods list sits above the detail at full width.
            if (compact || ImGui.GetContentRegionAvail().X < 600f)
            {
                int rows = addable.Count > 0 ? listed.Count + addable.Count + 2 : listed.Count + 1;
                float listH = ImGui.GetFrameHeightWithSpacing() * 1.5f + ImGui.GetTextLineHeightWithSpacing() * 1.25f * rows;
                if (listH > 220f)
                    listH = 220f;
                ImGui.BeginChild("##market-goods", new Vector2(0f, listH), ImGuiChildFlags.Borders);
                DrawGoods(ui, listed, addable, ref selectedId);
                ImGui.EndChild();
                DrawDetail(entity, market, history, book, officeSystemId, now, uiState, ui, listed, addable, selectedId, true);
                IntelDisplay.Display(entity, book, officeSystemId, uiState);
            }
            else
            {
                float listW = 210f;
                float bodyH = ImGui.GetContentRegionAvail().Y;
                if (bodyH < 160f)
                    bodyH = 160f;
                ImGui.BeginChild("##market-goods", new Vector2(listW, bodyH), ImGuiChildFlags.Borders);
                DrawGoods(ui, listed, addable, ref selectedId);
                ImGui.EndChild();
                ImGui.SameLine();
                ImGui.BeginChild("##market-detail", new Vector2(0f, bodyH));
                DrawDetail(entity, market, history, book, officeSystemId, now, uiState, ui, listed, addable, selectedId, false);
                ImGui.EndChild();
            }

            ui.SelectedId = selectedId;
            ImGui.PopID();
        }

        static void DrawToolbar(EntitySnapshot entity, MarketView market, OfficeUi ui, GlobalUIState uiState, List<MarketGoodView> listed)
        {
            string name = entity.GetView<NameView>()?.Name ?? "Colony";
            ImGui.Text(name);
            ImGui.SameLine();
            ImGui.TextDisabled("Listings " + OfficeSlots(market) + " / " + market.Capacity);
            if (!market.CanEdit && !market.ExchangeOpen)
            {
                ImGui.SameLine();
                ImGui.Text("Trade is not mutual.");
            }

            bool supply = market.CanEdit && entity.Kind == BodyKind.Colony;
            float spacing = ImGui.GetStyle().ItemSpacing.X;
            float total = ButtonWidth("Compare");
            if (supply)
                total += spacing + ButtonWidth("Run markets") + spacing + ButtonWidth("Balance") + spacing + ButtonWidth("Stockpile");
            ImGui.SameLine();
            if (ImGui.GetContentRegionAvail().X < total)
            {
                ImGui.NewLine();
            }
            else
            {
                float buttonX = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - total;
                if (buttonX > ImGui.GetCursorPosX())
                    ImGui.SetCursorPosX(buttonX);
            }

            if (Segment("Compare", ui.Compare))
            {
                ui.Compare = !ui.Compare;
                if (ui.Compare && ui.Checked.Count == 0 && IsListed(ui.SelectedId, listed))
                    ui.Checked.Add(ui.SelectedId);
            }
            if (!supply)
                return;

            ImGui.SameLine();
            if (ImGui.Button("Run markets"))
                uiState.GameClient?.SubmitCommandAsync(new SupplyLocalCommand(entity.Id, SupplyMode.Run));
            ImGui.SameLine();
            if (ImGui.Button("Balance"))
                uiState.GameClient?.SubmitCommandAsync(new SupplyLocalCommand(entity.Id, SupplyMode.Balance));
            ImGui.SameLine();
            if (ImGui.Button("Stockpile"))
                uiState.GameClient?.SubmitCommandAsync(new SupplyLocalCommand(entity.Id, SupplyMode.Stockpile));
        }

        static void DrawSupplyStatus(EntitySnapshot entity)
        {
            string span = entity.GetView<ColonyView>()?.CommandSpan ?? "";
            if (!string.IsNullOrEmpty(span))
                ImGui.Text(span);

            ImGui.TextWrapped("Stockpile raises this colony's reserve, and that reserve stays after the order is replaced.");

            if (entity.GetView<OrdersView>() is { } orders
                && orders.goal.Name is "Run markets" or "Balance" or "Stockpile")
            {
                ImGui.Text($"{orders.goal.Name}: {orders.goal.Status}");
                if (!string.IsNullOrEmpty(orders.goal.Message))
                    ImGui.TextWrapped(orders.goal.Message);
            }
        }

        static void DrawGoods(OfficeUi ui, List<MarketGoodView> listed, List<MarketGoodChoice> addable, ref string selectedId)
        {
            ImGui.SetNextItemWidth(-1f);
            string filter = ui.Filter;
            ImGui.InputTextWithHint("##good-search", "Search goods...", ref filter, 64);
            ui.Filter = filter;

            int shownListed = 0;
            foreach (var good in listed)
            {
                if (NameMatches(good.Name, filter))
                    shownListed++;
            }
            int shownAddable = 0;
            foreach (var choice in addable)
            {
                if (NameMatches(choice.Name, filter))
                    shownAddable++;
            }

            if (listed.Count == 0 && addable.Count == 0)
            {
                ImGui.Text("No listings.");
                return;
            }
            if (shownListed == 0 && shownAddable == 0)
            {
                ImGui.TextDisabled("No match.");
                return;
            }

            if (shownListed > 0)
                DrawListedTable(ui, listed, filter, ref selectedId);
            if (shownAddable > 0)
            {
                ImGui.Spacing();
                ImGui.TextDisabled("Add a listing");
                DrawAddableTable(addable, filter, ref selectedId);
            }
        }

        static void DrawListedTable(OfficeUi ui, List<MarketGoodView> listed, string filter, ref string selectedId)
        {
            int columns = ui.Compare ? 4 : 3;
            string tableId = ui.Compare ? "##market-goods-compare" : "##market-goods-rows";
            if (!ImGui.BeginTable(tableId, columns, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.PadOuterX))
                return;

            if (ui.Compare)
                ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 22f);
            ImGui.TableSetupColumn("Good", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Bid", ImGuiTableColumnFlags.WidthFixed, 52f);
            ImGui.TableSetupColumn("Ask", ImGuiTableColumnFlags.WidthFixed, 52f);
            ImGui.TableHeadersRow();

            uint selectedBg = ImGui.ColorConvertFloat4ToU32(new Vector4(0.26f, 0.59f, 0.98f, 0.35f));
            for (int i = 0; i < listed.Count; i++)
            {
                var good = listed[i];
                if (!NameMatches(good.Name, filter))
                    continue;

                bool selected = good.CargoId == selectedId;
                ImGui.TableNextRow();
                if (selected)
                    ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, selectedBg, -1);
                ImGui.PushID(good.CargoId);

                if (ui.Compare)
                {
                    ImGui.TableNextColumn();
                    bool on = ui.Checked.Contains(good.CargoId);
                    if (ImGui.Checkbox("##cmp", ref on))
                    {
                        MarketChartData.ToggleCompare(ui.Checked, good.CargoId);
                        selectedId = good.CargoId;
                    }
                }

                ImGui.TableNextColumn();
                if (ImGui.Selectable(good.Name + "###row", selected))
                {
                    // In Compare the whole row toggles the good, so a click that lands beside the small box still counts.
                    if (ui.Compare)
                        MarketChartData.ToggleCompare(ui.Checked, good.CargoId);
                    selectedId = good.CargoId;
                }
                ImGui.TableNextColumn();
                RightText(Money(good.Bid));
                if (ImGui.IsItemClicked())
                    selectedId = good.CargoId;
                ImGui.TableNextColumn();
                RightText(Money(good.Ask));
                if (ImGui.IsItemClicked())
                    selectedId = good.CargoId;
                ImGui.PopID();
            }

            ImGui.EndTable();
        }

        static void DrawAddableTable(List<MarketGoodChoice> addable, string filter, ref string selectedId)
        {
            if (!ImGui.BeginTable("##market-add-rows", 3, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.PadOuterX))
                return;
            ImGui.TableSetupColumn("Good", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Bid", ImGuiTableColumnFlags.WidthFixed, 52f);
            ImGui.TableSetupColumn("Ask", ImGuiTableColumnFlags.WidthFixed, 52f);

            uint selectedBg = ImGui.ColorConvertFloat4ToU32(new Vector4(0.26f, 0.59f, 0.98f, 0.35f));
            foreach (var choice in addable)
            {
                if (!NameMatches(choice.Name, filter))
                    continue;
                bool selected = choice.CargoId == selectedId;
                ImGui.TableNextRow();
                if (selected)
                    ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, selectedBg, -1);
                ImGui.PushID(choice.CargoId);
                ImGui.TableNextColumn();
                if (ImGui.Selectable(choice.Name + "###add", selected))
                    selectedId = choice.CargoId;
                ImGui.TableNextColumn();
                RightText("—");
                if (ImGui.IsItemClicked())
                    selectedId = choice.CargoId;
                ImGui.TableNextColumn();
                RightText("—");
                if (ImGui.IsItemClicked())
                    selectedId = choice.CargoId;
                ImGui.PopID();
            }
            ImGui.EndTable();
        }

        static void DrawDetail(
            EntitySnapshot entity,
            MarketView market,
            MarketHistoryView? history,
            IntelBookView? book,
            string officeSystemId,
            DateTime now,
            GlobalUIState uiState,
            OfficeUi ui,
            List<MarketGoodView> listed,
            List<MarketGoodChoice> addable,
            string selectedId,
            bool compact)
        {
            MarketGoodView? selectedGood = null;
            foreach (var good in listed)
            {
                if (good.CargoId == selectedId)
                {
                    selectedGood = good;
                    break;
                }
            }
            MarketGoodChoice? selectedChoice = null;
            if (selectedGood == null)
            {
                foreach (var choice in addable)
                {
                    if (choice.CargoId == selectedId)
                    {
                        selectedChoice = choice;
                        break;
                    }
                }
            }

            if (selectedGood == null && selectedChoice == null)
            {
                ImGui.Text("No listings.");
                if (!compact)
                    IntelDisplay.Display(entity, book, officeSystemId, uiState);
                return;
            }

            var shownIds = ShownCargoIds(ui, listed, selectedId);
            DateTime? earliest = EarliestDay(history, shownIds);
            var range = CurrentRange(ui, now, earliest);
            string title = Title(ui, listed, selectedGood, selectedChoice);
            DrawTitle(ui, history, selectedId, title, range);

            var facts = BuildFacts(ui, history, listed, selectedGood, selectedId);
            DrawFacts(facts);
            DrawChartControls(ui, now, earliest, listed, history, shownIds);

            range = CurrentRange(ui, now, earliest);
            float chartH = 200f;
            if (!compact)
            {
                float reserve = ImGui.GetFrameHeightWithSpacing();
                if (market.CanEdit)
                {
                    if (ImGui.GetContentRegionAvail().X >= 560f)
                        reserve += ImGui.GetFrameHeightWithSpacing() * 3f + 8f;
                    else
                        reserve += ImGui.GetFrameHeightWithSpacing() * 6f + 8f;
                }
                chartH = ImGui.GetContentRegionAvail().Y - reserve;
                if (chartH < 120f)
                    chartH = 120f;
            }
            DrawPlot(ui, history, listed, addable, selectedId, range, chartH);

            if (market.CanEdit)
                DrawEditor(entity.Id, market, selectedGood, selectedId, ui, uiState);
            if (!compact)
                IntelDisplay.Display(entity, book, officeSystemId, uiState);
        }

        static void DrawTitle(OfficeUi ui, MarketHistoryView? history, string selectedId, string title, MarketChartRange range)
        {
            string? changeText = null;
            Vector4 changeColor = MarketChart.Up;
            if (!ui.Compare && MarketChartData.TryRangeChange(DaysFor(history, selectedId), range.From, range.To, out var change))
            {
                string absolute = Signed(change.Absolute, "0.00");
                if (change.HasPercent)
                    changeText = absolute + "  ·  " + Signed(change.Percent, "0.0") + "% " + RangePhrase(ui);
                else
                    changeText = absolute + "  " + RangePhrase(ui);
                if (change.Absolute < 0)
                    changeColor = MarketChart.Down;
            }

            float avail = ImGui.GetContentRegionAvail().X;
            ImGui.PushFont(Styles.MediumFont, 16f);
            var titleSize = ImGui.CalcTextSize(title);
            bool changeFits = changeText != null && titleSize.X + ImGui.CalcTextSize(changeText).X + 16f < avail;
            if (changeFits)
                ImGui.TextUnformatted(title);
            else
                ImGui.TextWrapped(title);
            ImGui.PopFont();

            if (changeText == null)
                return;
            if (changeFits)
            {
                ImGui.SameLine();
                float changeW = ImGui.CalcTextSize(changeText).X;
                float rest = ImGui.GetContentRegionAvail().X;
                if (rest > changeW)
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + rest - changeW);
            }
            ImGui.TextColored(changeColor, changeText);
        }

        static List<Fact> BuildFacts(
            OfficeUi ui, MarketHistoryView? history, List<MarketGoodView> listed, MarketGoodView? selectedGood, string selectedId)
        {
            var facts = new List<Fact>();
            if (ui.Compare)
            {
                int colorIndex = 0;
                foreach (var good in listed)
                {
                    Vector4 color = Palette[colorIndex % Palette.Length];
                    colorIndex++;
                    if (!ui.Checked.Contains(good.CargoId))
                        continue;
                    string last = "—";
                    if (MarketChartData.TryLastTrade(DaysFor(history, good.CargoId), out decimal close))
                        last = Money(close);
                    facts.Add(new Fact(good.CargoId, good.Name + " · last", last, color));
                }
                return facts;
            }

            if (selectedGood == null)
            {
                string last = "—";
                if (MarketChartData.TryLastTrade(DaysFor(history, selectedId), out decimal addableClose))
                    last = Money(addableClose);
                facts.Add(new Fact("bid", "Bid", "—", null));
                facts.Add(new Fact("ask", "Ask", "—", null));
                facts.Add(new Fact("last", "Last trade", last, null));
                facts.Add(new Fact("spread", "Spread", "—", null));
                facts.Add(new Fact("stock", "Stock", "—", StockColor));
                facts.Add(new Fact("reserve", "Reserve", "—", null));
                facts.Add(new Fact("buy", "Buy qty", "—", BuyColor));
                facts.Add(new Fact("sell", "Sell qty", "—", SellColor));
                return facts;
            }

            string lastTrade = "—";
            if (MarketChartData.TryLastTrade(DaysFor(history, selectedGood.CargoId), out decimal tradedClose))
                lastTrade = Money(tradedClose);
            facts.Add(new Fact("bid", "Bid", Money(selectedGood.Bid), null));
            facts.Add(new Fact("ask", "Ask", Money(selectedGood.Ask), null));
            facts.Add(new Fact("last", "Last trade", lastTrade, null));
            facts.Add(new Fact("spread", "Spread", Money(selectedGood.Ask - selectedGood.Bid), null));
            facts.Add(new Fact("stock", "Stock", Qty(selectedGood.Stock), StockColor));
            facts.Add(new Fact("reserve", "Reserve", Qty(selectedGood.Reserve), null));
            facts.Add(new Fact("buy", "Buy qty", Qty(selectedGood.BuyQuantity), BuyColor));
            facts.Add(new Fact("sell", "Sell qty", Qty(selectedGood.SellQuantity), SellColor));
            return facts;
        }

        static void DrawFacts(List<Fact> facts)
        {
            if (facts.Count == 0)
                return;
            float spacing = ImGui.GetStyle().ItemSpacing.X;
            float rowLeft = ImGui.GetCursorPosX();
            float maxX = rowLeft + ImGui.GetContentRegionAvail().X;
            float boxH = ImGui.GetTextLineHeight() * 2f + 16f;
            ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.102f, 0.102f, 0.102f, 1f));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(6f, 4f));
            for (int i = 0; i < facts.Count; i++)
            {
                var fact = facts[i];
                float labelW = ImGui.CalcTextSize(fact.Label).X + 16f;
                float boxW = labelW < 92f ? 92f : labelW;
                if (boxW > 220f)
                    boxW = 220f;
                if (i > 0 && ImGui.GetCursorPosX() + spacing + boxW <= maxX)
                    ImGui.SameLine();
                ImGui.BeginChild("fact-" + fact.Id, new Vector2(boxW, boxH), ImGuiChildFlags.Borders);
                ImGui.TextDisabled(fact.Label);
                if (fact.Color is Vector4 color)
                    ImGui.TextColored(color, fact.Value);
                else
                    ImGui.TextUnformatted(fact.Value);
                ImGui.EndChild();
            }
            ImGui.PopStyleVar();
            ImGui.PopStyleColor();
        }

        static void DrawChartControls(
            OfficeUi ui,
            DateTime now,
            DateTime? earliest,
            List<MarketGoodView> listed,
            MarketHistoryView? history,
            List<string> shownIds)
        {
            var range = CurrentRange(ui, now, earliest);
            if (!ui.DateFieldActive)
            {
                ui.FromYear = range.From.Year;
                ui.FromMonth = range.From.Month;
                ui.FromDay = range.From.Day;
                ui.ToYear = range.To.Year;
                ui.ToMonth = range.To.Month;
                ui.ToDay = range.To.Day;
            }

            float rowLeft = ImGui.GetCursorPosX();
            float maxX = rowLeft + ImGui.GetContentRegionAvail().X;
            bool lineShown = ui.Compare || !ui.Candles;
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0f, ImGui.GetStyle().ItemSpacing.Y));
            if (Segment("Candles", !lineShown))
            {
                ui.Candles = true;
                ui.Compare = false;
            }
            ImGui.SameLine();
            if (Segment("Line", lineShown))
                ui.Candles = false;
            ImGui.PopStyleVar();

            if (ui.Compare)
            {
                float scaleW = ButtonWidth("Price") + ButtonWidth("Index");
                ImGui.SameLine();
                if (ImGui.GetCursorPosX() + scaleW > maxX)
                    ImGui.NewLine();
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0f, ImGui.GetStyle().ItemSpacing.Y));
                if (Segment("Price", !ui.Index))
                    ui.Index = false;
                ImGui.SameLine();
                if (Segment("Index", ui.Index))
                    ui.Index = true;
                ImGui.PopStyleVar();
            }

            float rangeW = ButtonWidth("1W") + ButtonWidth("1M") + ButtonWidth("3M") + ButtonWidth("1Y") + ButtonWidth("All");
            ImGui.SameLine();
            if (ImGui.GetCursorPosX() + rangeW > maxX)
                ImGui.NewLine();
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0f, ImGui.GetStyle().ItemSpacing.Y));
            if (Segment("1W", !ui.CustomRange && ui.Preset == MarketChartPreset.Week))
                SetPreset(ui, MarketChartPreset.Week);
            ImGui.SameLine();
            if (Segment("1M", !ui.CustomRange && ui.Preset == MarketChartPreset.Month))
                SetPreset(ui, MarketChartPreset.Month);
            ImGui.SameLine();
            if (Segment("3M", !ui.CustomRange && ui.Preset == MarketChartPreset.ThreeMonths))
                SetPreset(ui, MarketChartPreset.ThreeMonths);
            ImGui.SameLine();
            if (Segment("1Y", !ui.CustomRange && ui.Preset == MarketChartPreset.Year))
                SetPreset(ui, MarketChartPreset.Year);
            ImGui.SameLine();
            if (Segment("All", !ui.CustomRange && ui.Preset == MarketChartPreset.All))
                SetPreset(ui, MarketChartPreset.All);
            ImGui.PopStyleVar();

            int yearW = 52;
            int mdW = 32;
            float spacing = ImGui.GetStyle().ItemSpacing.X;
            float fromW = ImGui.CalcTextSize("From").X + yearW + mdW + mdW + spacing * 3f;
            float toW = ImGui.CalcTextSize("To").X + yearW + mdW + mdW + spacing * 3f;
            ImGui.SameLine();
            if (ImGui.GetCursorPosX() + fromW + spacing + toW > maxX)
                ImGui.NewLine();
            bool datesActive = false;
            ImGui.AlignTextToFramePadding();
            ImGui.Text("From");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(yearW);
            bool fromEdited = ImGui.InputInt("##from-y", ref ui.FromYear, 0, 0);
            datesActive |= ImGui.IsItemActive();
            ImGui.SameLine();
            ImGui.SetNextItemWidth(mdW);
            fromEdited |= ImGui.InputInt("##from-m", ref ui.FromMonth, 0, 0);
            datesActive |= ImGui.IsItemActive();
            ImGui.SameLine();
            ImGui.SetNextItemWidth(mdW);
            fromEdited |= ImGui.InputInt("##from-d", ref ui.FromDay, 0, 0);
            datesActive |= ImGui.IsItemActive();
            ImGui.SameLine();
            if (ImGui.GetCursorPosX() + toW > maxX)
                ImGui.NewLine();
            ImGui.AlignTextToFramePadding();
            ImGui.Text("To");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(yearW);
            bool toEdited = ImGui.InputInt("##to-y", ref ui.ToYear, 0, 0);
            datesActive |= ImGui.IsItemActive();
            ImGui.SameLine();
            ImGui.SetNextItemWidth(mdW);
            toEdited |= ImGui.InputInt("##to-m", ref ui.ToMonth, 0, 0);
            datesActive |= ImGui.IsItemActive();
            ImGui.SameLine();
            ImGui.SetNextItemWidth(mdW);
            toEdited |= ImGui.InputInt("##to-d", ref ui.ToDay, 0, 0);
            datesActive |= ImGui.IsItemActive();
            ui.DateFieldActive = datesActive;
            if (fromEdited || toEdited)
            {
                ui.DateFieldActive = true;
                ui.CustomRange = true;
                DateTime from = ClampDate(ui.FromYear, ui.FromMonth, ui.FromDay);
                DateTime to = ClampDate(ui.ToYear, ui.ToMonth, ui.ToDay);
                if (from > to)
                {
                    if (fromEdited && !toEdited)
                        to = from;
                    else
                        from = to;
                }
                ui.CustomFrom = from;
                ui.CustomTo = to;
            }

            DrawLegend(ui, listed, history, shownIds, CurrentRange(ui, now, earliest));
        }

        static void DrawLegend(
            OfficeUi ui, List<MarketGoodView> listed, MarketHistoryView? history, List<string> shownIds, MarketChartRange range)
        {
            bool quoteMid = false;
            bool any = false;
            bool lines = ui.Compare || !ui.Candles;
            float spacing = ImGui.GetStyle().ItemSpacing.X;
            float maxX = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X;
            if (ui.Compare)
            {
                int colorIndex = 0;
                foreach (var good in listed)
                {
                    Vector4 color = Palette[colorIndex % Palette.Length];
                    colorIndex++;
                    if (!ui.Checked.Contains(good.CargoId))
                        continue;
                    float need = ImGui.GetTextLineHeight() + ImGui.CalcTextSize(good.Name).X + spacing;
                    if (any && ImGui.GetCursorPosX() + need <= maxX)
                        ImGui.SameLine();
                    Swatch(color, good.Name);
                    any = true;
                }
            }

            if (lines)
            {
                foreach (var cargoId in shownIds)
                {
                    if (SeriesUsesQuoteMid(history, cargoId, range))
                        quoteMid = true;
                }
            }
            if (!quoteMid)
                return;
            float noteW = ImGui.CalcTextSize("quote mid").X + spacing;
            if (any && ImGui.GetCursorPosX() + noteW <= maxX)
                ImGui.SameLine();
            ImGui.TextDisabled("quote mid");
        }

        static void DrawPlot(
            OfficeUi ui,
            MarketHistoryView? history,
            List<MarketGoodView> listed,
            List<MarketGoodChoice> addable,
            string selectedId,
            MarketChartRange range,
            float chartH)
        {
            var size = MarketChartData.BucketSizeFor(range.From, range.To);
            if (ui.Compare && ui.Checked.Count == 0)
            {
                ImGui.TextUnformatted("No goods checked.");
                float rest = chartH - ImGui.GetTextLineHeightWithSpacing();
                if (rest > 0f)
                    ImGui.Dummy(new Vector2(1f, rest));
                return;
            }
            if (ui.Compare)
            {
                IReadOnlyList<MarketChartBucket> axis = Array.Empty<MarketChartBucket>();
                var series = new List<MarketLineSeries>();
                int colorIndex = 0;
                foreach (var good in listed)
                {
                    Vector4 color = Palette[colorIndex % Palette.Length];
                    colorIndex++;
                    if (!ui.Checked.Contains(good.CargoId))
                        continue;
                    var buckets = MarketChartData.Aggregate(DaysFor(history, good.CargoId), range.From, range.To, size);
                    if (buckets.Count == 0)
                        continue;
                    if (axis.Count == 0)
                        axis = buckets;
                    if (buckets.Count != axis.Count)
                        continue;
                    var values = new decimal?[buckets.Count];
                    var quote = new bool[buckets.Count];
                    for (int i = 0; i < buckets.Count; i++)
                    {
                        values[i] = buckets[i].Line;
                        quote[i] = buckets[i].QuoteMid;
                    }
                    IReadOnlyList<decimal?> plotted = ui.Index ? MarketChartData.Index(values) : values;
                    series.Add(new MarketLineSeries
                    {
                        Name = good.Name,
                        Color = color,
                        Values = plotted,
                        QuoteMid = quote,
                    });
                }
                if (axis.Count == 0)
                {
                    EmptyChart(chartH);
                    return;
                }
                MarketChart.Draw("compare", axis, false, series, ui.Index, chartH);
                return;
            }

            var one = MarketChartData.Aggregate(DaysFor(history, selectedId), range.From, range.To, size);
            if (one.Count == 0)
            {
                EmptyChart(chartH);
                return;
            }
            if (ui.Candles)
            {
                MarketChart.Draw(selectedId, one, true, Array.Empty<MarketLineSeries>(), false, chartH);
                return;
            }

            var lineValues = new decimal?[one.Count];
            var lineQuote = new bool[one.Count];
            Vector4 lineColor = SellColor;
            string lineName = selectedId;
            bool named = false;
            for (int i = 0; i < listed.Count; i++)
            {
                if (listed[i].CargoId == selectedId)
                {
                    lineColor = Palette[i % Palette.Length];
                    lineName = listed[i].Name;
                    named = true;
                    break;
                }
            }
            if (!named)
            {
                foreach (var choice in addable)
                {
                    if (choice.CargoId == selectedId)
                    {
                        lineName = choice.Name;
                        break;
                    }
                }
            }
            for (int i = 0; i < one.Count; i++)
            {
                lineValues[i] = one[i].Line;
                lineQuote[i] = one[i].QuoteMid;
            }
            var lines = new List<MarketLineSeries>
            {
                new MarketLineSeries
                {
                    Name = lineName,
                    Color = lineColor,
                    Values = lineValues,
                    QuoteMid = lineQuote,
                },
            };
            MarketChart.Draw(selectedId, one, false, lines, false, chartH);
        }

        static void EmptyChart(float chartH)
        {
            ImGui.TextUnformatted("No trades recorded yet.");
            float rest = chartH - ImGui.GetTextLineHeightWithSpacing();
            if (rest > 0f)
                ImGui.Dummy(new Vector2(1f, rest));
        }

        static void DrawEditor(
            int entityId, MarketView market, MarketGoodView? selectedGood, string selectedId, OfficeUi ui, GlobalUIState uiState)
        {
            if (selectedId.Length == 0)
                return;
            ImGui.Spacing();
            bool idle = !ImGui.IsAnyItemActive();
            ImGui.PushID("market-edit-" + entityId);
            bool horizontal = ImGui.GetContentRegionAvail().X >= 560f;
            if (selectedGood != null)
            {
                var edit = Row(ui, selectedGood, idle);
                DrawFields(edit, horizontal);
                if (horizontal)
                    ImGui.SameLine();
                if (horizontal)
                    ImGui.BeginGroup();
                if (horizontal)
                    ImGui.Dummy(new Vector2(1f, ImGui.GetTextLineHeight()));
                if (ImGui.Button("Set"))
                    SubmitSet(entityId, selectedGood.CargoId, edit, uiState);
                ImGui.SameLine();
                if (ImGui.Button("Remove"))
                    uiState.GameClient?.SubmitCommandAsync(new ClearMarketListingCommand(entityId, selectedGood.CargoId));
                if (horizontal)
                    ImGui.EndGroup();
            }
            else if (OfficeSlots(market) < market.Capacity)
            {
                var draft = ui.Add;
                if (ui.AddFor != selectedId)
                {
                    ui.AddFor = selectedId;
                    draft.Sell = 0;
                    draft.Buy = 0;
                    draft.Reserve = 0;
                    draft.Ask = 0;
                    draft.Bid = 0;
                }
                DrawFields(draft, horizontal);
                if (horizontal)
                    ImGui.SameLine();
                if (horizontal)
                    ImGui.BeginGroup();
                if (horizontal)
                    ImGui.Dummy(new Vector2(1f, ImGui.GetTextLineHeight()));
                if (ImGui.Button("Add"))
                {
                    SubmitSet(entityId, selectedId, draft, uiState);
                    draft.Sell = 0;
                    draft.Buy = 0;
                    draft.Reserve = 0;
                    draft.Ask = 0;
                    draft.Bid = 0;
                }
                if (horizontal)
                    ImGui.EndGroup();
            }
            else
            {
                ImGui.Text("This office is full.");
            }
            ImGui.PopID();
        }

        static void DrawFields(RowEdit edit, bool horizontal)
        {
            if (!horizontal)
            {
                ImGui.SetNextItemWidth(140f);
                ImGui.InputInt("Sell", ref edit.Sell);
                ImGui.SetNextItemWidth(140f);
                ImGui.InputDouble("Ask", ref edit.Ask, 0, 0, "%.2f");
                ImGui.SetNextItemWidth(140f);
                ImGui.InputInt("Buy", ref edit.Buy);
                ImGui.SetNextItemWidth(140f);
                ImGui.InputDouble("Bid", ref edit.Bid, 0, 0, "%.2f");
                ImGui.SetNextItemWidth(140f);
                ImGui.InputInt("Reserve", ref edit.Reserve);
                return;
            }

            FieldInt("Sell", ref edit.Sell);
            ImGui.SameLine();
            FieldDouble("Ask", ref edit.Ask);
            ImGui.SameLine();
            FieldInt("Buy", ref edit.Buy);
            ImGui.SameLine();
            FieldDouble("Bid", ref edit.Bid);
            ImGui.SameLine();
            FieldInt("Reserve", ref edit.Reserve);
        }

        static void FieldInt(string label, ref int value)
        {
            ImGui.BeginGroup();
            ImGui.TextDisabled(label);
            ImGui.SetNextItemWidth(72f);
            ImGui.InputInt("##" + label, ref value, 0, 0);
            ImGui.EndGroup();
        }

        static void FieldDouble(string label, ref double value)
        {
            ImGui.BeginGroup();
            ImGui.TextDisabled(label);
            ImGui.SetNextItemWidth(72f);
            ImGui.InputDouble("##" + label, ref value, 0, 0, "%.2f");
            ImGui.EndGroup();
        }

        static void Swatch(Vector4 color, string name)
        {
            var draw = ImGui.GetWindowDrawList();
            var pos = ImGui.GetCursorScreenPos();
            float side = ImGui.GetTextLineHeight() * 0.7f;
            float y = pos.Y + (ImGui.GetTextLineHeight() - side) * 0.5f;
            draw.AddRectFilled(new Vector2(pos.X, y), new Vector2(pos.X + side, y + side), ImGui.ColorConvertFloat4ToU32(color));
            ImGui.Dummy(new Vector2(side, ImGui.GetTextLineHeight()));
            ImGui.SameLine();
            ImGui.TextUnformatted(name);
        }

        static void RightText(string text)
        {
            float width = ImGui.CalcTextSize(text).X;
            float avail = ImGui.GetContentRegionAvail().X;
            if (avail > width)
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + avail - width);
            ImGui.TextUnformatted(text);
        }

        static bool Segment(string label, bool on)
        {
            if (on)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.26f, 0.59f, 0.98f, 0.40f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.26f, 0.59f, 0.98f, 0.55f));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.26f, 0.59f, 0.98f, 0.70f));
            }
            bool pressed = ImGui.Button(label);
            if (on)
                ImGui.PopStyleColor(3);
            return pressed;
        }

        static float ButtonWidth(string label)
        {
            return ImGui.CalcTextSize(label).X + ImGui.GetStyle().FramePadding.X * 2f;
        }

        static void SetPreset(OfficeUi ui, MarketChartPreset preset)
        {
            ui.Preset = preset;
            ui.CustomRange = false;
            ui.DateFieldActive = false;
        }

        static OfficeUi Office(int entityId)
        {
            if (!Ui.TryGetValue(entityId, out var ui))
            {
                ui = new OfficeUi();
                Ui[entityId] = ui;
            }
            return ui;
        }

        static int OfficeSlots(MarketView market)
        {
            int count = 0;
            foreach (var good in market.Goods)
            {
                if (!good.Relocation)
                    count++;
            }
            return count;
        }

        static bool NameMatches(string name, string filter)
        {
            return string.IsNullOrEmpty(filter)
                || name.Contains(filter, StringComparison.OrdinalIgnoreCase);
        }

        static string ResolveSelected(OfficeUi ui, List<MarketGoodView> listed, List<MarketGoodChoice> addable)
        {
            if (ui.SelectedId.Length > 0)
            {
                foreach (var good in listed)
                {
                    if (good.CargoId == ui.SelectedId)
                        return ui.SelectedId;
                }
                foreach (var choice in addable)
                {
                    if (choice.CargoId == ui.SelectedId)
                        return ui.SelectedId;
                }
            }
            if (listed.Count > 0)
                return listed[0].CargoId;
            if (addable.Count > 0)
                return addable[0].CargoId;
            return "";
        }

        static void DropStaleChecks(OfficeUi ui, List<MarketGoodView> listed)
        {
            if (ui.Checked.Count == 0)
                return;
            var stale = new List<string>();
            foreach (var id in ui.Checked)
            {
                bool found = false;
                foreach (var good in listed)
                {
                    if (good.CargoId == id)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    stale.Add(id);
            }
            foreach (var id in stale)
                ui.Checked.Remove(id);
        }

        static List<string> ShownCargoIds(OfficeUi ui, List<MarketGoodView> listed, string selectedId)
        {
            var ids = new List<string>();
            if (ui.Compare)
            {
                foreach (var good in listed)
                {
                    if (ui.Checked.Contains(good.CargoId))
                        ids.Add(good.CargoId);
                }
            }
            if (ids.Count == 0 && selectedId.Length > 0)
                ids.Add(selectedId);
            return ids;
        }

        static string Title(OfficeUi ui, List<MarketGoodView> listed, MarketGoodView? selectedGood, MarketGoodChoice? selectedChoice)
        {
            if (ui.Compare)
            {
                string title = "";
                foreach (var good in listed)
                {
                    if (!ui.Checked.Contains(good.CargoId))
                        continue;
                    if (title.Length > 0)
                        title += ", ";
                    title += good.Name;
                }
                if (title.Length > 0)
                    return title;
            }
            if (selectedGood != null)
                return selectedGood.Name;
            if (selectedChoice != null)
                return selectedChoice.Name;
            return "Market";
        }

        static IReadOnlyList<PricePointView>? DaysFor(MarketHistoryView? history, string? cargoId)
        {
            if (history == null || string.IsNullOrEmpty(cargoId))
                return null;
            foreach (var good in history.Goods)
            {
                if (good.CargoId == cargoId)
                    return good.Days;
            }
            return null;
        }

        static DateTime? EarliestDay(MarketHistoryView? history, List<string> cargoIds)
        {
            DateTime? earliest = null;
            foreach (var cargoId in cargoIds)
            {
                var days = DaysFor(history, cargoId);
                if (days == null)
                    continue;
                foreach (var day in days)
                {
                    if (day == null)
                        continue;
                    DateTime date = day.Day.Date;
                    if (earliest == null || date < earliest.Value)
                        earliest = date;
                }
            }
            return earliest;
        }

        static MarketChartRange CurrentRange(OfficeUi ui, DateTime now, DateTime? earliest)
        {
            if (!ui.CustomRange)
                return MarketChartData.Preset(ui.Preset, now, earliest);
            DateTime from = ui.CustomFrom.Date;
            DateTime to = ui.CustomTo.Date;
            if (from > to)
                from = to;
            return new MarketChartRange(from, to);
        }

        static bool SeriesUsesQuoteMid(MarketHistoryView? history, string cargoId, MarketChartRange range)
        {
            var buckets = MarketChartData.Aggregate(
                DaysFor(history, cargoId), range.From, range.To, MarketChartData.BucketSizeFor(range.From, range.To));
            for (int i = 0; i < buckets.Count; i++)
            {
                if (buckets[i].QuoteMid && buckets[i].Line != null)
                    return true;
            }
            return false;
        }

        static bool IsListed(string cargoId, List<MarketGoodView> listed)
        {
            if (cargoId.Length == 0)
                return false;
            foreach (var good in listed)
            {
                if (good.CargoId == cargoId)
                    return true;
            }
            return false;
        }

        static string RangePhrase(OfficeUi ui)
        {
            if (ui.CustomRange)
                return "in range";
            if (ui.Preset == MarketChartPreset.Week)
                return "this week";
            if (ui.Preset == MarketChartPreset.Month)
                return "this month";
            if (ui.Preset == MarketChartPreset.ThreeMonths)
                return "3 months";
            if (ui.Preset == MarketChartPreset.Year)
                return "this year";
            return "all";
        }

        static string Signed(decimal value, string format)
        {
            string number = value.ToString(format, CultureInfo.InvariantCulture);
            if (value > 0)
                return "+" + number;
            return number;
        }

        static string Money(decimal value)
        {
            return value.ToString("0.00", CultureInfo.InvariantCulture);
        }

        static string Qty(long value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture);
        }

        static DateTime ClampDate(int year, int month, int day)
        {
            if (year < 1)
                year = 1;
            if (year > 9999)
                year = 9999;
            if (month < 1)
                month = 1;
            if (month > 12)
                month = 12;
            int dim = DateTime.DaysInMonth(year, month);
            if (day < 1)
                day = 1;
            if (day > dim)
                day = dim;
            return new DateTime(year, month, day);
        }

        static RowEdit Row(OfficeUi ui, MarketGoodView good, bool idle)
        {
            if (idle || !ui.Rows.TryGetValue(good.CargoId, out var edit))
            {
                edit = Seed(good);
                ui.Rows[good.CargoId] = edit;
            }
            return edit;
        }

        static RowEdit Seed(MarketGoodView good)
        {
            return new RowEdit
            {
                Sell = ClampToInt(good.SellQuantity),
                Buy = ClampToInt(good.BuyQuantity),
                Reserve = ClampToInt(good.Reserve),
                Ask = (double)good.Ask,
                Bid = (double)good.Bid,
            };
        }

        static int ClampToInt(long value)
        {
            if (value < 0)
                return 0;
            if (value > int.MaxValue)
                return int.MaxValue;
            return (int)value;
        }

        static void SubmitSet(int entityId, string cargoId, RowEdit edit, GlobalUIState uiState)
        {
            uiState.GameClient?.SubmitCommandAsync(new SetMarketListingCommand(
                entityId,
                cargoId,
                edit.Sell,
                (decimal)edit.Ask,
                edit.Buy,
                (decimal)edit.Bid,
                edit.Reserve));
        }
    }
}
