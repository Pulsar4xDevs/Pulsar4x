using System;
using NUnit.Framework;
using Pulsar4X.Factions;

namespace Pulsar4X.Tests;

[TestFixture]
public class LedgerTests
{
    [Test]
    public void VerifyLedgerStartingValues()
    {
        Ledger ledger = new Ledger();

        Assert.AreEqual(0, ledger.GetCurrentFunds(), "Ledger current funds must start at 0");
    }


    [Test]
    public void VerifyIncome()
    {
        decimal total = 0;
        decimal amount = 1000;
        Ledger ledger = new Ledger();
        ledger.AddIncome(DateTime.Now, TransactionCategory.InitialInvestment, "Test", amount);
        total += amount;

        Assert.AreEqual(total, ledger.GetCurrentFunds(), "Ledger funds should be equal to the deposited amount");
        Assert.AreEqual(1, ledger.GetAllTransactions().Count);

        Random random = new Random();
        amount = random.Next();
        ledger.AddIncome(DateTime.Now, TransactionCategory.Research, "Test", amount);
        total += amount;
        Assert.AreEqual(total, ledger.GetCurrentFunds(), "Ledger funds should be equal to the subtracted amount");
        Assert.AreEqual(2, ledger.GetAllTransactions().Count);
    }

    [Test]
    public void VerifyExpenses()
    {
        decimal total = 0;
        decimal amount = 1000;
        Ledger ledger = new Ledger();
        ledger.AddExpense(DateTime.Now, TransactionCategory.InitialInvestment, "Test", amount);
        total += amount;

        Assert.AreEqual(-total, ledger.GetCurrentFunds(), "Ledger funds should be equal to the subtracted amount");
        Assert.AreEqual(1, ledger.GetAllTransactions().Count);

        Random random = new Random();
        amount = random.Next();
        ledger.AddExpense(DateTime.Now, TransactionCategory.Research, "Test", amount);
        total += amount;
        Assert.AreEqual(-total, ledger.GetCurrentFunds(), "Ledger funds should be equal to the subtracted amount");
        Assert.AreEqual(2, ledger.GetAllTransactions().Count);
    }
}