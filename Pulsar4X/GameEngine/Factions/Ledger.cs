using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Pulsar4X.Factions;

public enum TransactionCategory
{
    InitialInvestment,
    Research
}

public class Transaction
{
    public DateTime Date { get; set; }
    public TransactionCategory Category { get; set; }
    public string Description { get; set; }
    public decimal Amount { get; set; }
    public bool IsExpense => Amount < 0;
    public bool IsIncome => Amount > 0;
    public string Month => Date.ToString("yyyy-MM");
    public int Year => Date.Year;

    public Transaction(DateTime date, TransactionCategory category, string description, decimal amount)
    {
        Date = date;
        Category = category;
        Description = description;
        Amount = amount;
    }

    public override string ToString()
    {
        return $"{Date:yyyy-MM-dd} | {Category,-15} | {Description,-30} | {Amount,10:C2}";
    }
}

public class Ledger
{
    [JsonProperty]
    private List<Transaction> _transactions;
    [JsonProperty]
    private decimal _currentFunds;

    public Ledger()
    {
        _transactions = new List<Transaction>();
        _currentFunds = 0;
    }

    public void AddTransaction(DateTime date, TransactionCategory category, string description, decimal amount)
    {
        var transaction = new Transaction(date, category, description, amount);
        _transactions.Add(transaction);
        _currentFunds += transaction.Amount;
    }

    public void AddExpense(DateTime date, TransactionCategory category, string description, decimal amount)
    {
        // Ensure expense amount is negative
        if (amount > 0)
            amount = -amount;

        AddTransaction(date, category, description, amount);
    }

    public void AddIncome(DateTime date, TransactionCategory category, string description, decimal amount)
    {
        // Ensure income amount is positive
        if (amount < 0)
            amount = -amount;

        AddTransaction(date, category, description, amount);
    }

    public void RemoveTransaction(int index)
    {
        if (index >= 0 && index < _transactions.Count)
        {
            _transactions.RemoveAt(index);
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Transaction index is out of range.");
        }
    }

    public List<Transaction> GetAllTransactions()
    {
        return _transactions.OrderByDescending(t => t.Date).ToList();
    }

    public List<Transaction> GetTransactionsByCategory(TransactionCategory category)
    {
        return _transactions
            .Where(t => t.Category == category)
            .OrderByDescending(t => t.Date)
            .ToList();
    }

    public List<Transaction> GetTransactionsByMonth(int year, int month)
    {
        return _transactions
            .Where(t => t.Date.Year == year && t.Date.Month == month)
            .OrderByDescending(t => t.Date)
            .ToList();
    }

    public List<Transaction> GetTransactionsByYear(int year)
    {
        return _transactions
            .Where(t => t.Date.Year == year)
            .OrderByDescending(t => t.Date)
            .ToList();
    }

    public decimal GetTotalIncome()
    {
        return _transactions.Where(t => t.IsIncome).Sum(t => t.Amount);
    }

    public decimal GetTotalExpenses()
    {
        return _transactions.Where(t => t.IsExpense).Sum(t => t.Amount);
    }

    public decimal GetNetTotal()
    {
        return _transactions.Sum(t => t.Amount);
    }

    public decimal GetCurrentFunds() => _currentFunds;

    public Dictionary<TransactionCategory, decimal> GetTotalsByCategory()
    {
        return _transactions
            .GroupBy(t => t.Category)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(t => t.Amount)
            );
    }

    public Dictionary<string, decimal> GetMonthlyTotals()
    {
        return _transactions
            .GroupBy(t => t.Month)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(t => t.Amount)
            );
    }

    public Dictionary<string, decimal> GetMonthlyTotalsByCategory(TransactionCategory category)
    {
        return _transactions
            .Where(t => t.Category == category)
            .GroupBy(t => t.Month)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(t => t.Amount)
            );
    }

    public Dictionary<int, decimal> GetYearlyTotals()
    {
        return _transactions
            .GroupBy(t => t.Year)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(t => t.Amount)
            );
    }

    public Dictionary<int, decimal> GetYearlyTotalsByCategory(TransactionCategory category)
    {
        return _transactions
            .Where(t => t.Category == category)
            .GroupBy(t => t.Year)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(t => t.Amount)
            );
    }

    public Dictionary<string, Dictionary<TransactionCategory, decimal>> GetMonthlyCategoryBreakdown()
    {
        return _transactions
            .GroupBy(t => t.Month)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(t => t.Category)
                    .ToDictionary(
                        cg => cg.Key,
                        cg => cg.Sum(t => t.Amount)
                    )
            );
    }

    public Dictionary<int, Dictionary<TransactionCategory, decimal>> GetYearlyCategoryBreakdown()
    {
        return _transactions
            .GroupBy(t => t.Year)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(t => t.Category)
                    .ToDictionary(
                        cg => cg.Key,
                        cg => cg.Sum(t => t.Amount)
                    )
            );
    }
}