using Bank.Domain.Entities;
using Bank.Domain.Enums;
using Bank.Domain.Exceptions;
using Bank.Domain.Services;
using Bank.Domain.ValueObjects;

namespace Bank.Domain.Tests;

public class PostingRulesTests
{
    private readonly PostingService _postingService = new();

    private static Account CreateAccount(
        string number,
        AccountKind kind,
        AccountType type,
        LedgerClass ledgerClass,
        decimal initialBalance = 0m,
        string currency = "BDT",
        decimal overdraftLimit = 0m)
    {
        return new Account
        {
            Id = Guid.CreateVersion7(),
            AccountNumber = number,
            Kind = kind,
            ProductType = type,
            LedgerClass = ledgerClass,
            Currency = currency,
            Balance = initialBalance,
            OverdraftLimit = overdraftLimit,
            Status = AccountStatus.Active,
            LastEntrySeq = 0
        };
    }

    [Fact]
    public void Post_ValidTwoLegTransaction_ShouldSucceedAndBalanceAccounts()
    {
        // Customer A (Liability): Debit 500 (decreases liability balance)
        // Customer B (Liability): Credit 500 (increases liability balance)
        var accA = CreateAccount("1010000000018", AccountKind.Customer, AccountType.Savings, LedgerClass.Liability, 1000m);
        var accB = CreateAccount("1010000000026", AccountKind.Customer, AccountType.Savings, LedgerClass.Liability, 200m);

        var amount = new Money(500m, "BDT");
        var reference = TransactionReference.Generate();

        var instructions = new List<PostingInstruction>
        {
            new(accA, EntryDirection.Debit, amount),
            new(accB, EntryDirection.Credit, amount)
        };

        var txn = _postingService.Post(
            reference: reference,
            type: TransactionType.Transfer,
            amount: amount,
            description: "Intra-bank transfer A to B",
            channel: "ONLINE",
            initiatedByUserId: "user-123",
            onBehalfOfAdmin: false,
            instructions: instructions);

        txn.Should().NotBeNull();
        txn.Reference.Should().Be(reference.Value);
        txn.Entries.Should().HaveCount(2);

        // Verify account state
        accA.Balance.Should().Be(500m); // 1000 - 500
        accA.LastEntrySeq.Should().Be(1);

        accB.Balance.Should().Be(700m); // 200 + 500
        accB.LastEntrySeq.Should().Be(1);

        // Verify ledger entries
        var debitEntry = txn.Entries.Single(e => e.Direction == EntryDirection.Debit);
        debitEntry.AccountId.Should().Be(accA.Id);
        debitEntry.Amount.Should().Be(500m);
        debitEntry.AccountSeq.Should().Be(1);
        debitEntry.BalanceAfter.Should().Be(500m);

        var creditEntry = txn.Entries.Single(e => e.Direction == EntryDirection.Credit);
        creditEntry.AccountId.Should().Be(accB.Id);
        creditEntry.Amount.Should().Be(500m);
        creditEntry.AccountSeq.Should().Be(1);
        creditEntry.BalanceAfter.Should().Be(700m);
    }

    [Fact]
    public void Post_MultiLegBalancedSplitTransaction_ShouldSucceed()
    {
        // Transfer 1000: Debited from source 1000, Credited to dest 950, Credited to fee 50
        var source = CreateAccount("1010000000018", AccountKind.Customer, AccountType.Current, LedgerClass.Liability, 2000m);
        var destination = CreateAccount("1010000000026", AccountKind.Customer, AccountType.Savings, LedgerClass.Liability, 0m);
        var feeIncome = CreateAccount("1010000000034", AccountKind.Internal, AccountType.FeeIncome, LedgerClass.Income, 0m);

        var instructions = new List<PostingInstruction>
        {
            new(source, EntryDirection.Debit, new Money(1000m)),
            new(destination, EntryDirection.Credit, new Money(950m)),
            new(feeIncome, EntryDirection.Credit, new Money(50m))
        };

        var txn = _postingService.Post(
            reference: TransactionReference.Generate(),
            type: TransactionType.Transfer,
            amount: new Money(1000m),
            description: "Transfer with fee split",
            channel: "BRANCH",
            initiatedByUserId: "teller-1",
            onBehalfOfAdmin: false,
            instructions: instructions);

        txn.Entries.Should().HaveCount(3);
        source.Balance.Should().Be(1000m);
        destination.Balance.Should().Be(950m);
        feeIncome.Balance.Should().Be(50m);
    }

    [Fact]
    public void Post_UnbalancedDebitsAndCredits_ShouldThrowUnbalancedTransactionException()
    {
        var accA = CreateAccount("1010000000018", AccountKind.Customer, AccountType.Savings, LedgerClass.Liability, 1000m);
        var accB = CreateAccount("1010000000026", AccountKind.Customer, AccountType.Savings, LedgerClass.Liability, 200m);

        var instructions = new List<PostingInstruction>
        {
            new(accA, EntryDirection.Debit, new Money(500m)),
            new(accB, EntryDirection.Credit, new Money(499m)) // 1 BDT discrepancy
        };

        var act = () => _postingService.Post(
            reference: TransactionReference.Generate(),
            type: TransactionType.Transfer,
            amount: new Money(500m),
            description: "Unbalanced transfer",
            channel: "API",
            initiatedByUserId: "user-1",
            onBehalfOfAdmin: false,
            instructions: instructions);

        act.Should().Throw<UnbalancedTransactionException>()
            .WithMessage("*Double-entry invariant violated*");
    }

    [Fact]
    public void Post_WithFewerThanTwoInstructions_ShouldThrowUnbalancedTransactionException()
    {
        var acc = CreateAccount("1010000000018", AccountKind.Customer, AccountType.Savings, LedgerClass.Liability, 1000m);

        var instructions = new List<PostingInstruction>
        {
            new(acc, EntryDirection.Debit, new Money(100m))
        };

        var act = () => _postingService.Post(
            reference: TransactionReference.Generate(),
            type: TransactionType.Withdrawal,
            amount: new Money(100m),
            description: "Single-leg post",
            channel: "API",
            initiatedByUserId: "user-1",
            onBehalfOfAdmin: false,
            instructions: instructions);

        act.Should().Throw<UnbalancedTransactionException>()
            .WithMessage("*at least 2 entries*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Post_WithZeroOrNegativeAmount_ShouldThrowInvalidMoneyException(decimal invalidAmount)
    {
        var accA = CreateAccount("1010000000018", AccountKind.Customer, AccountType.Savings, LedgerClass.Liability, 1000m);
        var accB = CreateAccount("1010000000026", AccountKind.Customer, AccountType.Savings, LedgerClass.Liability, 200m);

        var instructions = new List<PostingInstruction>
        {
            new(accA, EntryDirection.Debit, new Money(invalidAmount)),
            new(accB, EntryDirection.Credit, new Money(invalidAmount))
        };

        var act = () => _postingService.Post(
            reference: TransactionReference.Generate(),
            type: TransactionType.Transfer,
            amount: new Money(100m),
            description: "Invalid amount transfer",
            channel: "API",
            initiatedByUserId: "user-1",
            onBehalfOfAdmin: false,
            instructions: instructions);

        act.Should().Throw<InvalidMoneyException>();
    }

    [Fact]
    public void Post_WithCurrencyMismatchBetweenAccounts_ShouldThrowCurrencyMismatchException()
    {
        var accBdt = CreateAccount("1010000000018", AccountKind.Customer, AccountType.Savings, LedgerClass.Liability, 1000m, "BDT");
        var accUsd = CreateAccount("1010000000026", AccountKind.Customer, AccountType.Savings, LedgerClass.Liability, 200m, "USD");

        var instructions = new List<PostingInstruction>
        {
            new(accBdt, EntryDirection.Debit, new Money(100m, "BDT")),
            new(accUsd, EntryDirection.Credit, new Money(100m, "USD"))
        };

        var act = () => _postingService.Post(
            reference: TransactionReference.Generate(),
            type: TransactionType.Transfer,
            amount: new Money(100m, "BDT"),
            description: "Cross-currency transfer without exchange",
            channel: "API",
            initiatedByUserId: "user-1",
            onBehalfOfAdmin: false,
            instructions: instructions);

        act.Should().Throw<CurrencyMismatchException>();
    }

    [Fact]
    public void Post_SuccessiveTransactions_ShouldMaintainStrictGaplessSequence()
    {
        var accA = CreateAccount("1010000000018", AccountKind.Customer, AccountType.Savings, LedgerClass.Liability, 1000m);
        var accB = CreateAccount("1010000000026", AccountKind.Customer, AccountType.Savings, LedgerClass.Liability, 1000m);

        for (int i = 1; i <= 5; i++)
        {
            var instructions = new List<PostingInstruction>
            {
                new(accA, EntryDirection.Debit, new Money(10m)),
                new(accB, EntryDirection.Credit, new Money(10m))
            };

            var txn = _postingService.Post(
                reference: TransactionReference.Generate(),
                type: TransactionType.Transfer,
                amount: new Money(10m),
                description: $"Transfer #{i}",
                channel: "API",
                initiatedByUserId: "user-1",
                onBehalfOfAdmin: false,
                instructions: instructions);

            accA.LastEntrySeq.Should().Be(i);
            accB.LastEntrySeq.Should().Be(i);

            txn.Entries.Single(e => e.AccountId == accA.Id).AccountSeq.Should().Be(i);
            txn.Entries.Single(e => e.AccountId == accB.Id).AccountSeq.Should().Be(i);
        }

        accA.Balance.Should().Be(950m);
        accB.Balance.Should().Be(1050m);
    }
}
