using System.Security.Claims;
using Bank.Api.Filters;
using Bank.Api.Security;
using Bank.Application.Accounts;
using Bank.Application.Payments;
using Bank.Infrastructure.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Bank.Api.Endpoints;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1")
            .WithTags("Payments & Transactions")
            .RequireAuthorization();

        group.MapPost("/deposits", ProcessDepositAsync)
            .AddEndpointFilter<IdempotencyEndpointFilter>()
            .WithName("ProcessDeposit")
            .WithSummary("Deposit money into an account (idempotent, single DB transaction)");

        group.MapPost("/withdrawals", ProcessWithdrawalAsync)
            .AddEndpointFilter<IdempotencyEndpointFilter>()
            .WithName("ProcessWithdrawal")
            .WithSummary("Withdraw money from an account (idempotent, single DB transaction)");

        group.MapPost("/transfers", ProcessTransferAsync)
            .AddEndpointFilter<IdempotencyEndpointFilter>()
            .WithName("ProcessTransfer")
            .WithSummary("Transfer money between accounts (idempotent, deadlock-free row locking)");

        group.MapGet("/transactions/{id:guid}", GetTransactionByIdAsync)
            .WithName("GetTransactionById")
            .WithSummary("Get transaction details and immutable double-entry ledger lines");

        return routes;
    }

    private static async Task<IResult> ProcessDepositAsync(
        [FromBody] DepositRequest request,
        [FromServices] IPaymentService paymentService,
        [FromServices] ApplicationDbContextLocator locator,
        ClaimsPrincipal user,
        HttpContext httpContext,
        CancellationToken ct)
    {
        string userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value
            ?? "ANONYMOUS";

        Guid? recordId = httpContext.Items.TryGetValue("IdempotencyRecordId", out var rIdObj) && rIdObj is Guid rId
            ? rId
            : null;

        var transaction = await paymentService.ProcessDepositAsync(request, userId, recordId, ct);
        httpContext.Items["TransactionId"] = transaction.Id;

        return Results.Created($"/api/v1/transactions/{transaction.Id}", transaction);
    }

    private static async Task<IResult> ProcessWithdrawalAsync(
        [FromBody] WithdrawalRequest request,
        [FromServices] IPaymentService paymentService,
        [FromServices] IAccountService accountService,
        [FromServices] ApplicationDbContextLocator locator,
        ClaimsPrincipal user,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var account = await accountService.GetAccountByIdAsync(request.AccountId, ct);
        if (account is null)
        {
            return Results.NotFound(new ProblemDetails
            {
                Title = "Account Not Found",
                Detail = $"Account {request.AccountId} was not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        bool isAuthorized = await locator.CanAccessAccountAsync(user, account, ct);
        if (!isAuthorized)
        {
            return Results.Forbid();
        }

        string userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value
            ?? "ANONYMOUS";

        Guid? recordId = httpContext.Items.TryGetValue("IdempotencyRecordId", out var rIdObj) && rIdObj is Guid rId
            ? rId
            : null;

        var transaction = await paymentService.ProcessWithdrawalAsync(request, userId, recordId, ct);
        httpContext.Items["TransactionId"] = transaction.Id;

        return Results.Created($"/api/v1/transactions/{transaction.Id}", transaction);
    }

    private static async Task<IResult> ProcessTransferAsync(
        [FromBody] TransferRequest request,
        [FromServices] IPaymentService paymentService,
        [FromServices] IAccountService accountService,
        [FromServices] ApplicationDbContextLocator locator,
        ClaimsPrincipal user,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var sourceAcc = await accountService.GetAccountByIdAsync(request.SourceAccountId, ct);
        if (sourceAcc is null)
        {
            return Results.NotFound(new ProblemDetails
            {
                Title = "Source Account Not Found",
                Detail = $"Source account {request.SourceAccountId} was not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        bool isAuthorized = await locator.CanAccessAccountAsync(user, sourceAcc, ct);
        if (!isAuthorized)
        {
            return Results.Forbid();
        }

        string userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value
            ?? "ANONYMOUS";

        Guid? recordId = httpContext.Items.TryGetValue("IdempotencyRecordId", out var rIdObj) && rIdObj is Guid rId
            ? rId
            : null;

        var transaction = await paymentService.ProcessTransferAsync(request, userId, recordId, ct);
        httpContext.Items["TransactionId"] = transaction.Id;

        return Results.Created($"/api/v1/transactions/{transaction.Id}", transaction);
    }

    private static async Task<IResult> GetTransactionByIdAsync(
        Guid id,
        ClaimsPrincipal user,
        [FromServices] IPaymentService paymentService,
        [FromServices] IAccountService accountService,
        [FromServices] ApplicationDbContextLocator locator,
        CancellationToken ct)
    {
        var transaction = await paymentService.GetTransactionByIdAsync(id, ct);
        if (transaction is null)
        {
            return Results.NotFound();
        }

        if (user.IsInRole(ApplicationRole.Admin) || user.IsInRole(ApplicationRole.Auditor))
        {
            return Results.Ok(transaction);
        }

        var customerId = user.GetCustomerId() ?? await locator.ResolveCustomerIdAsync(user, ct);
        if (!customerId.HasValue)
        {
            return Results.Forbid();
        }

        var distinctAccountIds = transaction.Entries.Select(e => e.AccountId).Distinct();
        bool hasAccess = false;
        foreach (var accountId in distinctAccountIds)
        {
            var account = await accountService.GetAccountByIdAsync(accountId, ct);
            if (account != null && account.CustomerId == customerId.Value)
            {
                hasAccess = true;
                break;
            }
        }

        if (!hasAccess)
        {
            return Results.Forbid();
        }

        return Results.Ok(transaction);
    }
}
