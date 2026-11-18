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
        if (!Window.Begin("Transactions", ref IsActive))
        {
            Window.End();
            return;
        }

        var faction = _uiState.GameClient?.Galaxy.Faction;
        if (faction == null)
        {
            ImGui.TextUnformatted("No corporation.");
            Window.End();
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
        if (years.Count == 0)
        {
            ImGui.TextUnformatted("No transactions.");
            Window.End();
            return;
        }

        if (ImGui.BeginChild("LedgerTree", new Vector2(0, ImGui.GetContentRegionAvail().Y), ImGuiChildFlags.None))
        {
            for (int y = 0; y < years.Count; y++)
                DrawYear(years[y]);
        }
        ImGui.EndChild();

        Window.End();
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
        _ => category
    };
}
