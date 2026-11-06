using Bank.Domain.Entities;
using Bank.Domain.Enums;
using Bank.Domain.Exceptions;
using Bank.Domain.Services;
using Bank.Domain.ValueObjects;

namespace Bank.Domain.Tests;

public class AccountLifecycleAndOverdraftTests
{
    private readonly TransferPolicy _transferPolicy = new();

    private static Account CreateCustomerAccount(
        decimal balance = 1000m,
        decimal overdraftLimit = 0m,
        decimal minBalance = 0m,
        decimal? dailyDebitLimit = null,
        AccountStatus status = AccountStatus.Active)
    {
        return new Account
        {
            Id = Guid.CreateVersion7(),
            AccountNumber = "1010000000018",
            Kind = AccountKind.Customer,
            ProductType = AccountType.Savings,
            LedgerClass = LedgerClass.Liability,
            Currency = "BDT",
            Balance = balance,
            OverdraftLimit = overdraftLimit,
            MinBalance = minBalance,
            DailyDebitLimit = dailyDebitLimit,
            Status = status,
            LastEntrySeq = 0
        };
    }

    [Fact]
    public void Freeze_ActiveAccount_ShouldChangeStatusToFrozen()
    {
        var account = CreateCustomerAccount();

        account.Freeze();

        account.Status.Should().Be(AccountStatus.Frozen);
        account.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Unfreeze_FrozenAccount_ShouldRestoreActiveStatus()
    {
        var account = CreateCustomerAccount(status: AccountStatus.Frozen);

        account.Unfreeze();

        account.Status.Should().Be(AccountStatus.Active);
    }

    [Fact]
    public void Freeze_ClosedAccount_ShouldThrowAccountClosedException()
    {
        var account = CreateCustomerAccount(balance: 0m);
        account.Close();

        var act = () => account.Freeze();

        act.Should().Throw<AccountClosedException>();
    }

    [Fact]
    public void Unfreeze_ClosedAccount_ShouldThrowAccountClosedException()
    {
        var account = CreateCustomerAccount(balance: 0m);
        account.Close();

        var act = () => account.Unfreeze();

        act.Should().Throw<AccountClosedException>();
    }

    [Fact]
    public void Close_AccountWithZeroBalanceAndHolds_ShouldSucceed()
    {
        var account = CreateCustomerAccount(balance: 0m);

        account.Close();

        account.Status.Should().Be(AccountStatus.Closed);
        account.ClosedAt.Should().NotBeNull();
    }

    [Fact]
    public void Close_AccountWithRemainingBalance_ShouldThrowDomainException()
    {
        var account = CreateCustomerAccount(balance: 50.00m);

        var act = () => account.Close();

        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be("NON_ZERO_BALANCE");
    }

    [Fact]
    public void Close_AccountWithPendingHolds_ShouldThrowDomainException()
    {
        var account = CreateCustomerAccount(balance: 0m);
        account.HeldAmount = 100m;

        var act = () => account.Close();

        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be("PENDING_HOLDS");
    }

    [Fact]
    public void CanDebit_WhenAccountIsFrozen_ShouldReturnFalseWithReason()
    {
        var account = CreateCustomerAccount(balance: 1000m, status: AccountStatus.Frozen);

        bool canDebit = account.CanDebit(new Money(100m), out string? reason);

        canDebit.Should().BeFalse();
        reason.Should().Contain("frozen");
    }

    [Fact]
    public void CanDebit_WhenAccountIsClosed_ShouldReturnFalseWithReason()
    {
        var account = CreateCustomerAccount(balance: 0m);
        account.Close();

        bool canDebit = account.CanDebit(new Money(100m), out string? reason);

        canDebit.Should().BeFalse();
        reason.Should().Contain("closed");
    }

    [Fact]
    public void CanCredit_WhenAccountIsClosed_ShouldReturnFalseWithReason()
    {
        var account = CreateCustomerAccount(balance: 0m);
        account.Close();

        bool canCredit = account.CanCredit(new Money(100m), out string? reason);

        canCredit.Should().BeFalse();
        reason.Should().Contain("closed");
    }

    [Fact]
    public void CanDebit_WhenAmountExceedsMinBalance_ShouldReturnFalse()
    {
        // Balance 1000, MinBalance 500 -> Max debitable is 500
        var account = CreateCustomerAccount(balance: 1000m, minBalance: 500m);

        bool canDebitAllowed = account.CanDebit(new Money(500m), out _);
        bool canDebitExceeded = account.CanDebit(new Money(500.01m), out string? reason);

        canDebitAllowed.Should().BeTrue();
        canDebitExceeded.Should().BeFalse();
        reason.Should().Contain("Insufficient funds");
    }

    [Fact]
    public void ApplyEntry_WithinOverdraftLimit_ShouldSucceed()
    {
        // Balance 100, Overdraft 500 -> Available 600
        var account = CreateCustomerAccount(balance: 100m, overdraftLimit: 500m);

        account.ApplyEntry(EntryDirection.Debit, new Money(550m), 1);

        account.Balance.Should().Be(-450m);
        account.LastEntrySeq.Should().Be(1);
    }

    [Fact]
    public void ApplyEntry_ExceedingOverdraftLimit_ShouldThrowInsufficientFundsException()
    {
        // Balance 100, Overdraft 500 -> Debit 601 violates overdraft
        var account = CreateCustomerAccount(balance: 100m, overdraftLimit: 500m);

        var act = () => account.ApplyEntry(EntryDirection.Debit, new Money(600.0001m), 1);

        act.Should().Throw<InsufficientFundsException>()
            .WithMessage("*violates overdraft limit*");
    }

    [Fact]
    public void TransferPolicy_TransferToSameAccount_ShouldThrowDomainException()
    {
        var account = CreateCustomerAccount();

        var act = () => _transferPolicy.ValidateTransfer(account, account, new Money(100m));

        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be("SAME_ACCOUNT_TRANSFER");
    }

    [Fact]
    public void TransferPolicy_TransferFromFrozenAccount_ShouldThrowAccountFrozenException()
    {
        var source = CreateCustomerAccount(balance: 1000m, status: AccountStatus.Frozen);
        var dest = CreateCustomerAccount(balance: 100m);

        var act = () => _transferPolicy.ValidateTransfer(source, dest, new Money(50m));

        act.Should().Throw<AccountFrozenException>()
            .Which.ErrorCode.Should().Be("ACCOUNT_FROZEN");
    }

    [Fact]
    public void TransferPolicy_TransferFromClosedAccount_ShouldThrowAccountClosedException()
    {
        var source = CreateCustomerAccount(balance: 0m);
        source.Close();
        var dest = CreateCustomerAccount(balance: 100m);

        var act = () => _transferPolicy.ValidateTransfer(source, dest, new Money(50m));

        act.Should().Throw<AccountClosedException>()
            .Which.ErrorCode.Should().Be("ACCOUNT_CLOSED");
    }

    [Fact]
    public void TransferPolicy_DailyDebitLimitExceeded_ShouldThrowDailyLimitExceededException()
    {
        // Daily limit 10,000. Already debited today: 9,000. New transfer: 1,500 (total 10,500)
        var source = CreateCustomerAccount(balance: 50000m, dailyDebitLimit: 10000m);
        var dest = CreateCustomerAccount(balance: 1000m);

        var act = () => _transferPolicy.ValidateTransfer(source, dest, new Money(1500m), todayTotalDebited: 9000m);

        act.Should().Throw<DailyLimitExceededException>()
            .Which.ErrorCode.Should().Be("DAILY_LIMIT_EXCEEDED");
    }

    [Fact]
    public void TransferPolicy_TransferWithinDailyLimit_ShouldPassValidation()
    {
        var source = CreateCustomerAccount(balance: 50000m, dailyDebitLimit: 10000m);
        var dest = CreateCustomerAccount(balance: 1000m);

        var act = () => _transferPolicy.ValidateTransfer(source, dest, new Money(1000m), todayTotalDebited: 8500m);

        act.Should().NotThrow();
    }
}
