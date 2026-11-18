using System;
using System.Collections.Generic;
using System.Globalization;
using Pulsar4X.Api;

namespace Pulsar4X.Client;

/// <summary>One category's total on a year or month statement, plus the lines that make it up (newest first).</summary>
public sealed record LedgerCategoryTotal(
    string Category,
    string Label,
    decimal Total,
    bool Income,
    IReadOnlyList<LedgerEntrySnapshot> Entries);

/// <summary>One month's statement: income and expense totals, then totals per category.</summary>
public sealed record LedgerMonthStatement(
    int Year,
    int Month,
    DateTime MonthStart,
    decimal IncomeTotal,
    decimal ExpenseTotal,
    IReadOnlyList<LedgerCategoryTotal> IncomeCategories,
    IReadOnlyList<LedgerCategoryTotal> ExpenseCategories);

/// <summary>One year's statement: income and expense totals, category rollups, and the months newest first.</summary>
public sealed record LedgerYearStatement(
    int Year,
    decimal IncomeTotal,
    decimal ExpenseTotal,
    IReadOnlyList<LedgerCategoryTotal> IncomeCategories,
    IReadOnlyList<LedgerCategoryTotal> ExpenseCategories,
    IReadOnlyList<LedgerMonthStatement> Months);

/// <summary>
/// Groups ledger rows into yearly then monthly statements with income/expense category totals.
/// No ImGui — the window only renders what this returns.
/// </summary>
public static class LedgerTree
{
    public static IReadOnlyList<LedgerYearStatement> Build(IEnumerable<LedgerEntrySnapshot>? rows)
    {
        if (rows == null)
            return Array.Empty<LedgerYearStatement>();

        var years = new Dictionary<int, List<LedgerEntrySnapshot>>();
        var yearOrder = new List<int>();
        foreach (var row in rows)
        {
            int year = row.Date.Year;
            if (!years.TryGetValue(year, out var list))
            {
                list = new List<LedgerEntrySnapshot>();
                years[year] = list;
                yearOrder.Add(year);
            }
            list.Add(row);
        }

        yearOrder.Sort((a, b) => b.CompareTo(a));
        var result = new List<LedgerYearStatement>(yearOrder.Count);
        foreach (int year in yearOrder)
            result.Add(BuildYear(year, years[year]));
        return result;
    }

    public static string YearLabel(int year) => DateDisplay.FormatYear(year);

    public static string MonthLabel(DateTime monthStart) => DateDisplay.FormatMonthYear(monthStart);

    public static string Money(decimal amount)
        => amount.ToString("C2", CultureInfo.CurrentCulture);

    public static string StatementLabel(string period, decimal income, decimal expense)
        => period + "    Income " + Money(income) + "    Expense " + Money(expense);

    public static string CategoryLine(LedgerCategoryTotal category)
        => category.Label + "    " + Money(category.Total);

    static LedgerYearStatement BuildYear(int year, List<LedgerEntrySnapshot> rows)
    {
        var months = new Dictionary<(int Year, int Month), List<LedgerEntrySnapshot>>();
        var monthOrder = new List<(int Year, int Month)>();
        foreach (var row in rows)
        {
            var key = (row.Date.Year, row.Date.Month);
            if (!months.TryGetValue(key, out var list))
            {
                list = new List<LedgerEntrySnapshot>();
                months[key] = list;
                monthOrder.Add(key);
            }
            list.Add(row);
        }

        monthOrder.Sort((a, b) => b.Year != a.Year ? b.Year.CompareTo(a.Year) : b.Month.CompareTo(a.Month));
        var statements = new List<LedgerMonthStatement>(monthOrder.Count);
        foreach (var key in monthOrder)
            statements.Add(BuildMonth(key.Year, key.Month, months[key]));

        var (income, expense, incomeCats, expenseCats) = Rollup(rows);
        return new LedgerYearStatement(year, income, expense, incomeCats, expenseCats, statements);
    }

    static LedgerMonthStatement BuildMonth(int year, int month, List<LedgerEntrySnapshot> rows)
    {
        var (income, expense, incomeCats, expenseCats) = Rollup(rows);
        return new LedgerMonthStatement(
            year, month, new DateTime(year, month, 1), income, expense, incomeCats, expenseCats);
    }

    static (decimal Income, decimal Expense, IReadOnlyList<LedgerCategoryTotal> IncomeCats, IReadOnlyList<LedgerCategoryTotal> ExpenseCats)
        Rollup(List<LedgerEntrySnapshot> rows)
    {
        var income = new Dictionary<string, List<LedgerEntrySnapshot>>(StringComparer.Ordinal);
        var expense = new Dictionary<string, List<LedgerEntrySnapshot>>(StringComparer.Ordinal);
        var incomeOrder = new List<string>();
        var expenseOrder = new List<string>();
        decimal incomeTotal = 0;
        decimal expenseTotal = 0;

        foreach (var row in rows)
        {
            bool isIncome = row.Amount >= 0;
            var bag = isIncome ? income : expense;
            var order = isIncome ? incomeOrder : expenseOrder;
            if (!bag.TryGetValue(row.Category, out var list))
            {
                list = new List<LedgerEntrySnapshot>();
                bag[row.Category] = list;
                order.Add(row.Category);
            }
            list.Add(row);
            if (isIncome)
                incomeTotal += row.Amount;
            else
                expenseTotal += row.Amount;
        }

        return (incomeTotal, expenseTotal, Totals(income, incomeOrder, income: true), Totals(expense, expenseOrder, income: false));
    }

    static IReadOnlyList<LedgerCategoryTotal> Totals(
        Dictionary<string, List<LedgerEntrySnapshot>> bag, List<string> order, bool income)
    {
        var result = new List<LedgerCategoryTotal>(order.Count);
        foreach (var category in order)
        {
            var entries = bag[category];
            decimal total = 0;
            foreach (var entry in entries)
                total += entry.Amount;
            result.Add(new LedgerCategoryTotal(
                category, LedgerWindow.CategoryLabel(category), total, income, entries));
        }
        return result;
    }
}
