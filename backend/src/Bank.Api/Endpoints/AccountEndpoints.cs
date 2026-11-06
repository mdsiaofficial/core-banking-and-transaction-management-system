using System.Security.Claims;
using Bank.Api.Security;
using Bank.Application.Accounts;
using Bank.Domain.Enums;
using Bank.Infrastructure.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Bank.Api.Endpoints;

public record OpenAccountDto(
    string ProductType,
    decimal OverdraftLimit = 0m,
    decimal MinBalance = 0m,
    decimal? DailyDebitLimit = null);

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/accounts")
            .WithTags("Accounts")
            .RequireAuthorization();

        group.MapPost("/", OpenAccountAsync)
            .WithName("OpenAccount")
            .WithSummary("Open a new savings or current account");

        group.MapGet("/", GetAccountsAsync)
            .WithName("GetAccounts")
            .WithSummary("Get user's accounts or list all accounts (Admin)");

        group.MapGet("/{id:guid}", GetAccountByIdAsync)
            .WithName("GetAccountById")
            .WithSummary("Get account details by ID");

        group.MapGet("/{id:guid}/balance", GetAccountBalanceAsync)
            .WithName("GetAccountBalance")
            .WithSummary("Get account balance details");

        group.MapGet("/{id:guid}/transactions", GetAccountTransactionsAsync)
            .WithName("GetAccountTransactions")
            .WithSummary("Get account ledger entries with keyset cursor pagination");

        group.MapGet("/{id:guid}/statement", ExportAccountStatementAsync)
            .WithName("ExportAccountStatement")
            .WithSummary("Export account statement as CSV");

        group.MapPost("/{id:guid}/close", CloseAccountAsync)
            .WithName("CloseAccount")
            .WithSummary("Close an account with zero balance");

        return routes;
    }

    private static async Task<IResult> OpenAccountAsync(
        [FromBody] OpenAccountDto request,
        [FromServices] IAccountService accountService,
        [FromServices] ApplicationDbContextLocator locator,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var customerId = await locator.ResolveCustomerIdAsync(user, ct);
        if (!customerId.HasValue)
        {
            return Results.Forbid();
        }

        if (!Enum.TryParse<AccountType>(request.ProductType, true, out var productType))
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Invalid Product Type",
                Detail = "ProductType must be 'Savings' or 'Current'.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var account = await accountService.OpenAccountAsync(
            customerId.Value,
            productType,
            request.OverdraftLimit,
            request.MinBalance,
            request.DailyDebitLimit,
            ct);

        return Results.Created($"/api/v1/accounts/{account.Id}", account);
    }

    private static async Task<IResult> GetAccountsAsync(
        ClaimsPrincipal user,
        [FromServices] IAccountService accountService,
        [FromServices] ApplicationDbContextLocator locator,
        [FromQuery] string? status = null,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        bool isAdmin = user.IsInRole(ApplicationRole.Admin) || user.IsInRole(ApplicationRole.Auditor);

        if (isAdmin)
        {
            var all = await accountService.GetAllAccountsAsync(status, limit, ct);
            return Results.Ok(all);
        }

        var customerId = await locator.ResolveCustomerIdAsync(user, ct);
        if (!customerId.HasValue)
        {
            return Results.Ok(Array.Empty<AccountDto>());
        }

        var customerAccounts = await accountService.GetAccountsByCustomerIdAsync(customerId.Value, ct);
        return Results.Ok(customerAccounts);
    }

    private static async Task<IResult> GetAccountByIdAsync(
        Guid id,
        ClaimsPrincipal user,
        [FromServices] IAccountService accountService,
        [FromServices] ApplicationDbContextLocator locator,
        CancellationToken ct)
    {
        var account = await accountService.GetAccountByIdAsync(id, ct);
        if (account is null) return Results.NotFound();

        bool isAuthorized = await locator.CanAccessAccountAsync(user, account, ct);
        if (!isAuthorized) return Results.Forbid();

        return Results.Ok(account);
    }

    private static async Task<IResult> GetAccountBalanceAsync(
        Guid id,
        ClaimsPrincipal user,
        [FromServices] IAccountService accountService,
        [FromServices] ApplicationDbContextLocator locator,
        CancellationToken ct)
    {
        var account = await accountService.GetAccountByIdAsync(id, ct);
        if (account is null) return Results.NotFound();

        bool isAuthorized = await locator.CanAccessAccountAsync(user, account, ct);
        if (!isAuthorized) return Results.Forbid();

        var balance = await accountService.GetAccountBalanceAsync(id, ct);
        return balance is null ? Results.NotFound() : Results.Ok(balance);
    }

    private static async Task<IResult> GetAccountTransactionsAsync(
        Guid id,
        ClaimsPrincipal user,
        [FromServices] IAccountService accountService,
        [FromServices] ApplicationDbContextLocator locator,
        [FromQuery] string? cursor = null,
        [FromQuery] int limit = 20,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] string? direction = null,
        CancellationToken ct = default)
    {
        var account = await accountService.GetAccountByIdAsync(id, ct);
        if (account is null) return Results.NotFound();

        bool isAuthorized = await locator.CanAccessAccountAsync(user, account, ct);
        if (!isAuthorized) return Results.Forbid();

        var result = await accountService.GetAccountEntriesKeysetAsync(id, cursor, limit, from, to, direction, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> ExportAccountStatementAsync(
        Guid id,
        ClaimsPrincipal user,
        [FromServices] IAccountService accountService,
        [FromServices] ApplicationDbContextLocator locator,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken ct = default)
    {
        var account = await accountService.GetAccountByIdAsync(id, ct);
        if (account is null) return Results.NotFound();

        bool isAuthorized = await locator.CanAccessAccountAsync(user, account, ct);
        if (!isAuthorized) return Results.Forbid();

        byte[] csvBytes = await accountService.ExportStatementCsvAsync(id, from, to, ct);
        string fileName = $"statement_{account.AccountNumber}_{DateTime.UtcNow:yyyyMMdd}.csv";

        return Results.File(csvBytes, "text/csv", fileName);
    }

    private static async Task<IResult> CloseAccountAsync(
        Guid id,
        ClaimsPrincipal user,
        [FromServices] IAccountService accountService,
        [FromServices] ApplicationDbContextLocator locator,
        CancellationToken ct)
    {
        var account = await accountService.GetAccountByIdAsync(id, ct);
        if (account is null) return Results.NotFound();

        bool isAuthorized = await locator.CanAccessAccountAsync(user, account, ct);
        if (!isAuthorized) return Results.Forbid();

        await accountService.CloseAccountAsync(id, ct);
        return Results.NoContent();
    }
}
