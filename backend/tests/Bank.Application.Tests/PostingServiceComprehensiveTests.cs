using Bank.Domain.Common;
using Bank.Domain.Entities;
using Bank.Domain.Enums;
using Bank.Domain.Exceptions;
using Bank.Domain.Services;
using Bank.Domain.ValueObjects;

namespace Bank.Application.Tests;

public class PostingServiceComprehensiveTests
{
    private readonly PostingService _postingService = new();

    [Fact]
    public void Post_WithBalancedEntries_SucceedsAndUpdatesAccountBalances()
    {
        var cash = new Account
        {
            Id = Guid.CreateVersion7(),
            AccountNumber = SystemAccounts.CashAccountNumber,
            Kind = AccountKind.Internal,
            ProductType = AccountType.Cash,
            LedgerClass = LedgerClass.Asset,
            Currency = "BDT",
            Balance = 1000m,
            LastEntrySeq = 0
        };

        var customer = new Account
        {
            Id = Guid.CreateVersion7(),
            AccountNumber = "1010000000018",
            Kind = AccountKind.Customer,
            ProductType = AccountType.Savings,
            LedgerClass = LedgerClass.Liability,
            Currency = "BDT",
            Balance = 500m,
            LastEntrySeq = 0
        };

        var amount = new Money(200m, "BDT");
        var instructions = new List<PostingInstruction>
        {
            new(cash, EntryDirection.Debit, amount),
            new(customer, EntryDirection.Credit, amount)
        };

        var txn = _postingService.Post(
            TransactionReference.Generate(),
            TransactionType.Deposit,
            amount,
            "Test deposit",
            "TEST",
            "user1",
            false,
            instructions);

        txn.Should().NotBeNull();
        txn.Entries.Count.Should().Be(2);

        // Cash (Asset): debit increases balance -> 1000 + 200 = 1200
        cash.Balance.Should().Be(1200m);
        cash.LastEntrySeq.Should().Be(1);

        // Customer (Liability): credit increases balance -> 500 + 200 = 700
        customer.Balance.Should().Be(700m);
        customer.LastEntrySeq.Should().Be(1);
    }

    [Fact]
    public void Post_WithUnbalancedEntries_ThrowsUnbalancedTransactionException()
    {
        var cash = new Account
        {
            Id = Guid.CreateVersion7(),
            AccountNumber = SystemAccounts.CashAccountNumber,
            Kind = AccountKind.Internal,
            ProductType = AccountType.Cash,
            LedgerClass = LedgerClass.Asset,
            Currency = "BDT",
            Balance = 1000m,
            LastEntrySeq = 0
        };

        var customer = new Account
        {
            Id = Guid.CreateVersion7(),
            AccountNumber = "1010000000018",
            Kind = AccountKind.Customer,
            ProductType = AccountType.Savings,
            LedgerClass = LedgerClass.Liability,
            Currency = "BDT",
            Balance = 500m,
            LastEntrySeq = 0
        };

        var instructions = new List<PostingInstruction>
        {
            new(cash, EntryDirection.Debit, new Money(200m, "BDT")),
            new(customer, EntryDirection.Credit, new Money(150m, "BDT")) // Unbalanced!
        };

        Action act = () => _postingService.Post(
            TransactionReference.Generate(),
            TransactionType.Deposit,
            new Money(200m, "BDT"),
            "Unbalanced",
            "TEST",
            "user1",
            false,
            instructions);

        act.Should().Throw<UnbalancedTransactionException>();
    }

    [Fact]
    public void Post_WithLessThanTwoEntries_ThrowsUnbalancedTransactionException()
    {
        var customer = new Account
        {
            Id = Guid.CreateVersion7(),
            AccountNumber = "1010000000018",
            Kind = AccountKind.Customer,
            ProductType = AccountType.Savings,
            LedgerClass = LedgerClass.Liability,
            Currency = "BDT",
            Balance = 500m,
            LastEntrySeq = 0
        };

        var instructions = new List<PostingInstruction>
        {
            new(customer, EntryDirection.Credit, new Money(100m, "BDT"))
        };

        Action act = () => _postingService.Post(
            TransactionReference.Generate(),
            TransactionType.Deposit,
            new Money(100m, "BDT"),
            "Single line",
            "TEST",
            "user1",
            false,
            instructions);

        act.Should().Throw<UnbalancedTransactionException>();
    }

    [Fact]
    public void Post_WithNegativeOrZeroAmount_ThrowsInvalidMoneyException()
    {
        var cash = new Account
        {
            Id = Guid.CreateVersion7(),
            AccountNumber = SystemAccounts.CashAccountNumber,
            Kind = AccountKind.Internal,
            ProductType = AccountType.Cash,
            LedgerClass = LedgerClass.Asset,
            Currency = "BDT",
            Balance = 1000m,
            LastEntrySeq = 0
        };

        var customer = new Account
        {
            Id = Guid.CreateVersion7(),
            AccountNumber = "1010000000018",
            Kind = AccountKind.Customer,
            ProductType = AccountType.Savings,
            LedgerClass = LedgerClass.Liability,
            Currency = "BDT",
            Balance = 500m,
            LastEntrySeq = 0
        };

        var instructions = new List<PostingInstruction>
        {
            new(cash, EntryDirection.Debit, new Money(0m, "BDT")),
            new(customer, EntryDirection.Credit, new Money(0m, "BDT"))
        };

        Action act = () => _postingService.Post(
            TransactionReference.Generate(),
            TransactionType.Deposit,
            new Money(0m, "BDT"),
            "Zero amount",
            "TEST",
            "user1",
            false,
            instructions);

        act.Should().Throw<InvalidMoneyException>();
    }
}
