using System.Security.Claims;
using Bank.Api.Security;
using Bank.Domain.Entities;
using Bank.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Bank.Api.IntegrationTests.Security;

public class AccountOwnershipAuthorizationHandlerTests
{
    private readonly AccountOwnershipAuthorizationHandler _handler = new();
    private readonly AccountOwnershipRequirement _requirement = new();

    [Fact]
    public async Task HandleAsync_WhenCustomerOwnsAccount_ShouldSucceed()
    {
        var customerId = Guid.NewGuid();
        var account = new Account
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            AccountNumber = "1001000000010",
            Kind = AccountKind.Customer
        };

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim("customer_id", customerId.ToString()),
            new Claim(ClaimTypes.Role, "Customer")
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var context = new AuthorizationHandlerContext(new[] { _requirement }, user, account);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WhenCustomerDoesNotOwnAccount_ShouldFail_PreventingIdor()
    {
        var attackerCustomerId = Guid.NewGuid();
        var victimCustomerId = Guid.NewGuid();

        var victimAccount = new Account
        {
            Id = Guid.NewGuid(),
            CustomerId = victimCustomerId,
            AccountNumber = "1001000000028",
            Kind = AccountKind.Customer
        };

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim("customer_id", attackerCustomerId.ToString()),
            new Claim(ClaimTypes.Role, "Customer")
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var context = new AuthorizationHandlerContext(new[] { _requirement }, user, victimAccount);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
        context.HasFailed.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WhenUserIsAdmin_ShouldSucceedForAnyAccount()
    {
        var victimCustomerId = Guid.NewGuid();
        var account = new Account
        {
            Id = Guid.NewGuid(),
            CustomerId = victimCustomerId,
            AccountNumber = "1001000000036"
        };

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var context = new AuthorizationHandlerContext(new[] { _requirement }, user, account);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WhenUserIsAuditor_ShouldSucceedForAnyAccount()
    {
        var customerId = Guid.NewGuid();
        var account = new Account
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            AccountNumber = "1001000000044"
        };

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Auditor")
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var context = new AuthorizationHandlerContext(new[] { _requirement }, user, account);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }
}
