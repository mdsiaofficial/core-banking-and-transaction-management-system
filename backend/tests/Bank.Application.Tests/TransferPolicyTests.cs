using Bank.Domain.Entities;
using Bank.Domain.Enums;
using Bank.Domain.Exceptions;
using Bank.Domain.Services;
using Bank.Domain.ValueObjects;

namespace Bank.Application.Tests;

public class TransferPolicyTests
{
    private readonly TransferPolicy _policy = new();

    [Fact]
    public void ValidateTransfer_WhenSourceAndDestinationAreSame_ThrowsDomainException()
    {
        var id = Guid.CreateVersion7();
        var account = new Account
        {
            Id = id,
            AccountNumber = "1010000000018",
            Kind = AccountKind.Customer,
            Currency = "BDT",
            Balance = 1000m,
            Status = AccountStatus.Active
        };

        Action act = () => _policy.ValidateTransfer(account, account, new Money(100m, "BDT"));
        act.Should().Throw<DomainException>()
            .Where(e => e.ErrorCode == "SAME_ACCOUNT_TRANSFER");
    }

    [Fact]
    public void ValidateTransfer_WhenSourceHasInsufficientFunds_ThrowsInsufficientFundsException()
    {
        var src = new Account
        {
            Id = Guid.CreateVersion7(),
            AccountNumber = "1010000000018",
            Kind = AccountKind.Customer,
            Currency = "BDT",
            Balance = 100m,
            OverdraftLimit = 0m,
            MinBalance = 50m,
            Status = AccountStatus.Active
        };

        var dst = new Account
        {
            Id = Guid.CreateVersion7(),
            AccountNumber = "1010000000026",
            Kind = AccountKind.Customer,
            Currency = "BDT",
            Balance = 500m,
            Status = AccountStatus.Active
        };

        // Available balance is 100 - 50 = 50. Transferring 80 should fail!
        Action act = () => _policy.ValidateTransfer(src, dst, new Money(80m, "BDT"));
        act.Should().Throw<InsufficientFundsException>();
    }

    [Fact]
    public void ValidateTransfer_WhenSourceIsFrozen_ThrowsAccountFrozenException()
    {
        var src = new Account
        {
            Id = Guid.CreateVersion7(),
            AccountNumber = "1010000000018",
            Kind = AccountKind.Customer,
            Currency = "BDT",
            Balance = 1000m,
            Status = AccountStatus.Frozen
        };

        var dst = new Account
        {
            Id = Guid.CreateVersion7(),
            AccountNumber = "1010000000026",
            Kind = AccountKind.Customer,
            Currency = "BDT",
            Balance = 500m,
            Status = AccountStatus.Active
        };

        Action act = () => _policy.ValidateTransfer(src, dst, new Money(100m, "BDT"));
        act.Should().Throw<AccountFrozenException>();
    }

    [Fact]
    public void ValidateTransfer_WhenDailyLimitExceeded_ThrowsDailyLimitExceededException()
    {
        var src = new Account
        {
            Id = Guid.CreateVersion7(),
            AccountNumber = "1010000000018",
            Kind = AccountKind.Customer,
            Currency = "BDT",
            Balance = 10000m,
            DailyDebitLimit = 5000m,
            Status = AccountStatus.Active
        };

        var dst = new Account
        {
            Id = Guid.CreateVersion7(),
            AccountNumber = "1010000000026",
            Kind = AccountKind.Customer,
            Currency = "BDT",
            Balance = 500m,
            Status = AccountStatus.Active
        };

        // Already debited 4500 today, attempting 1000 -> total 5500 > 5000
        Action act = () => _policy.ValidateTransfer(src, dst, new Money(1000m, "BDT"), todayTotalDebited: 4500m);
        act.Should().Throw<DailyLimitExceededException>();
    }
}
