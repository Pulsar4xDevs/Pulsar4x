using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Api;
using Pulsar4X.Client.Interface.Widgets;

namespace Pulsar4X.Client;

/// <summary>
/// The corporation ledger. Opened from the funds line in <see cref="Selector"/>.
/// Rows come from <see cref="FactionSnapshot.Transactions"/>, newest first.
/// </summary>
public class LedgerWindow : UniquePulsarGuiWindow<LedgerWindow>
{
    string? _flowPeriodId;

    LedgerWindow()
    {
    }

    internal static LedgerWindow GetInstance()
    {
        if (_uiState.TryGetUniqueWindow<LedgerWindow>(out var window))
            return window;

        return _uiState.AddUniqueWindow(new LedgerWindow());
    }

    internal override void Display()
    {
        if (!IsActive)
            return;

        ImGui.SetNextWindowSize(new Vector2(820, 520), ImGuiCond.FirstUseEver);
        using (var w = Window.Scope("Transactions", ref IsActive))
        {
            if (!w.Visible)
                return;

            var faction = _uiState.GameClient?.Galaxy.Faction;
            if (faction == null)
            {
                ImGui.TextUnformatted("No corporation.");
                return;
            }

            ImGui.TextUnformatted(faction.Name);
            if (faction.Funds < 0)
                Finances.ColouredText(Finances.BannerColour, "Balance " + faction.Funds.ToString("C2", CultureInfo.CurrentCulture));
            else
                ImGui.TextUnformatted("Balance " + faction.Funds.ToString("C2", CultureInfo.CurrentCulture));
            // The galaxy's faction is the player's own; NPC factions pay no wages and have no books here.
            Finances.Draw(faction);
            ImGui.Separator();

            var years = LedgerTree.Build(faction.Transactions);
            if (ImGui.BeginTabBar("LedgerTabs"))
            {
                if (ImGui.BeginTabItem("Statement"))
                {
                    DrawStatement(years);
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Flow"))
                {
                    DrawFlow(years);
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }
        }
    }

    static void DrawStatement(IReadOnlyList<LedgerYearStatement> years)
    {
        if (years.Count == 0)
        {
            ImGui.TextUnformatted("No transactions.");
            return;
        }

        if (ImGui.BeginChild("LedgerTree", new Vector2(0, ImGui.GetContentRegionAvail().Y), ImGuiChildFlags.None))
        {
            for (int y = 0; y < years.Count; y++)
                DrawYear(years[y]);
        }
        ImGui.EndChild();
    }

    void DrawFlow(IReadOnlyList<LedgerYearStatement> years)
    {
        var periods = LedgerSankeyLayout.Periods(years);
        var period = LedgerSankeyLayout.Resolve(periods, _flowPeriodId);
        if (period != null)
            _flowPeriodId = period.Id;

        if (ImGui.BeginCombo("##flow-period", period?.Label ?? "No periods"))
        {
            foreach (var option in periods)
            {
                bool selected = period != null && option.Id == period.Id;
                if (ImGui.Selectable(option.Label + "###" + option.Id, selected))
                    _flowPeriodId = option.Id;
                if (selected)
                    ImGui.SetItemDefaultFocus();
            }
            ImGui.EndCombo();
        }

        if (ImGui.BeginChild("LedgerFlow", new Vector2(0, ImGui.GetContentRegionAvail().Y), ImGuiChildFlags.None))
        {
            if (period == null)
                ImGui.TextUnformatted(LedgerSankeyLayout.EmptyMessage);
            else
                DrawSankey(period);
        }
        ImGui.EndChild();
    }

    static void DrawSankey(LedgerSankeyPeriod period)
    {
        var origin = ImGui.GetCursorScreenPos();
        var avail = ImGui.GetContentRegionAvail();
        if (avail.X < 1f)
            avail.X = 1f;
        if (avail.Y < 1f)
            avail.Y = 1f;
        ImGui.Dummy(avail);

        var diagram = LedgerSankeyLayout.Build(
            period.IncomeCategories,
            period.ExpenseCategories,
            period.IncomeTotal,
            period.ExpenseTotal,
            origin.X,
            origin.Y,
            avail.X,
            avail.Y,
            ImGui.CalcTextSize,
            LedgerTree.Money);

        if (diagram.IsEmpty)
        {
            ImGui.SetCursorScreenPos(origin);
            ImGui.TextUnformatted(diagram.EmptyMessage ?? LedgerSankeyLayout.EmptyMessage);
            return;
        }

        var draw = ImGui.GetWindowDrawList();
        foreach (var band in diagram.Bands)
        {
            uint colour = Colour(band.Kind, node: false);
            var poly = LedgerSankeyLayout.BandPolygon(band);
            for (int i = 0; i < poly.Length; i++)
                draw.PathLineTo(poly[i]);
            draw.PathFillConcave(colour);
        }

        foreach (var node in diagram.Nodes)
        {
            draw.AddRectFilled(
                new Vector2(node.X, node.Y),
                new Vector2(node.X + node.Width, node.Y + node.Height),
                Colour(node.Kind, node: true),
                3f);
        }

        foreach (var node in diagram.Nodes)
        {
            DrawNodeLabel(node);
        }

        if (ImGui.IsWindowHovered())
        {
            var mouse = ImGui.GetMousePos();
            var hit = diagram.HitTest(mouse.X, mouse.Y);
            if (hit != null)
            {
                ImGui.BeginTooltip();
                foreach (var tip in LedgerSankeyLayout.TooltipLines(hit, LedgerTree.Money))
                    ImGui.TextUnformatted(tip);
                ImGui.EndTooltip();
            }
        }
    }

    static uint Colour(LedgerSankeyBandKind kind, bool node)
        => Colour(kind switch
        {
            LedgerSankeyBandKind.Income => LedgerSankeyNodeKind.Income,
            LedgerSankeyBandKind.Expense => LedgerSankeyNodeKind.Expense,
            LedgerSankeyBandKind.NetSaved => LedgerSankeyNodeKind.NetSaved,
            _ => LedgerSankeyNodeKind.Shortfall,
        }, node);

    static void DrawNodeLabel(LedgerSankeyNode node)
    {
        float line = node.LabelLineHeight > 1f ? node.LabelLineHeight : ImGui.GetTextLineHeight();
        DrawAlignedLabel(node, node.FittedLabel, node.LabelY);
        DrawAlignedLabel(node, node.FittedAmount, node.LabelY + line);
    }

    static void DrawAlignedLabel(LedgerSankeyNode node, string text, float y)
    {
        if (string.IsNullOrEmpty(text))
            return;
        float x = node.LabelX;
        if (node.LabelAlignRight)
        {
            float w = ImGui.CalcTextSize(text).X;
            x = node.LabelX + Math.Max(0f, node.LabelMaxWidth - w);
        }
        ImGui.SetCursorScreenPos(new Vector2(x, y));
        ImGui.TextUnformatted(text);
    }

    static uint Colour(LedgerSankeyNodeKind kind, bool node)
    {
        Vector4 rgb = kind switch
        {
            LedgerSankeyNodeKind.Income => new Vector4(0.22f, 0.62f, 0.35f, 1f),
            LedgerSankeyNodeKind.Expense => new Vector4(0.78f, 0.28f, 0.26f, 1f),
            LedgerSankeyNodeKind.NetSaved => new Vector4(0.18f, 0.55f, 0.52f, 1f),
            LedgerSankeyNodeKind.Shortfall => new Vector4(0.78f, 0.52f, 0.20f, 1f),
            _ => new Vector4(0.32f, 0.40f, 0.52f, 1f),
        };
        if (node)
            return ImGui.ColorConvertFloat4ToU32(rgb);
        const float lift = 0.40f;
        var band = new Vector4(
            rgb.X + (1f - rgb.X) * lift,
            rgb.Y + (1f - rgb.Y) * lift,
            rgb.Z + (1f - rgb.Z) * lift,
            0.50f);
        return ImGui.ColorConvertFloat4ToU32(band);
    }

    static void DrawYear(LedgerYearStatement year)
    {
        bool open = ImGui.TreeNode(LedgerTree.StatementLabel(LedgerTree.YearLabel(year.Year), year.IncomeTotal, year.ExpenseTotal)
            + "###year-" + year.Year.ToString(CultureInfo.InvariantCulture));
        if (!open)
            return;

        DrawCategoryGroups(year.IncomeCategories, year.ExpenseCategories, "year-" + year.Year.ToString(CultureInfo.InvariantCulture));
        foreach (var month in year.Months)
            DrawMonth(month);
        ImGui.TreePop();
    }

    static void DrawMonth(LedgerMonthStatement month)
    {
        string id = month.Year.ToString(CultureInfo.InvariantCulture) + "-" + month.Month.ToString("00", CultureInfo.InvariantCulture);
        bool open = ImGui.TreeNode(LedgerTree.StatementLabel(LedgerTree.MonthLabel(month.MonthStart), month.IncomeTotal, month.ExpenseTotal)
            + "###month-" + id);
        if (!open)
            return;

        DrawCategoryGroups(month.IncomeCategories, month.ExpenseCategories, "month-" + id);
        ImGui.TreePop();
    }

    static void DrawCategoryGroups(
        IReadOnlyList<LedgerCategoryTotal> income,
        IReadOnlyList<LedgerCategoryTotal> expense,
        string idPrefix)
    {
        if (income.Count > 0)
        {
            ImGui.TextDisabled("Income");
            foreach (var category in income)
                DrawCategory(category, idPrefix);
        }
        if (expense.Count > 0)
        {
            ImGui.TextDisabled("Expense");
            foreach (var category in expense)
                DrawCategory(category, idPrefix);
        }
    }

    static void DrawCategory(LedgerCategoryTotal category, string idPrefix)
    {
        bool open = ImGui.TreeNode(LedgerTree.CategoryLine(category)
            + "###cat-" + idPrefix + "-" + category.Category + (category.Income ? "-in" : "-ex"));
        if (!open)
            return;

        if (ImGui.BeginTable("##entries-" + idPrefix + "-" + category.Category + (category.Income ? "-in" : "-ex"),
                3, Styles.TableFlags))
        {
            ImGui.TableSetupColumn("Date", ImGuiTableColumnFlags.WidthFixed,
                System.Math.Max(168f, ImGui.CalcTextSize(DateDisplay.WidestDateTimeSample()).X + 8f));
            ImGui.TableSetupColumn("Description", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Amount", ImGuiTableColumnFlags.WidthFixed, 120);
            ImGui.TableHeadersRow();
            foreach (var row in category.Entries)
            {
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(DateDisplay.FormatDateTime(row.Date));
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(row.Description);
                if (ImGui.IsItemHovered() && row.Description.Length > 0)
                {
                    ImGui.BeginTooltip();
                    ImGui.TextUnformatted(row.Description);
                    ImGui.EndTooltip();
                }
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(LedgerTree.Money(row.Amount));
            }
            ImGui.EndTable();
        }
        ImGui.TreePop();
    }

    /// <summary>The ledger's category column. Wages and research are billed once a month, on the 1st.</summary>
    public static string CategoryLabel(string category) => category switch
    {
        "InitialInvestment" => "Initial investment",
        "Research" => "Research (monthly)",
        "Trade" => "Trade",
        FactionSnapshot.WagesCategory => "Wages (monthly)",
        FactionSnapshot.StaffWagesCategory => "Staff wages (monthly)",
        FactionSnapshot.TaxCategory => "Tax",
        "CrewTransfer" => "Crew transfer",
        "Charter" => "Charter",
        _ => category
    };
}
