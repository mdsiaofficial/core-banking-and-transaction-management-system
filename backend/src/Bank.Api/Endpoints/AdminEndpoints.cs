using System.Security.Claims;
using Bank.Api.Filters;
using Bank.Api.Security;
using Bank.Application.Accounts;
using Bank.Application.Customers;
using Bank.Application.Payments;
using Bank.Application.Reconciliation;
using Bank.Infrastructure.Auth;
using Bank.Infrastructure.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Bank.Api.Endpoints;

public record ReasonDto(string Reason);

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/admin")
            .WithTags("Administration")
            .RequireAuthorization();

        // Customer management
        group.MapGet("/customers", SearchCustomersAsync)
            .RequireAuthorization(AuthorizationPolicies.AdminOrAuditorPolicy)
            .WithName("AdminSearchCustomers")
            .WithSummary("Search and filter customer profiles");

        group.MapGet("/customers/{id:guid}", GetCustomerByIdAsync)
            .RequireAuthorization(AuthorizationPolicies.AdminOrAuditorPolicy)
            .WithName("AdminGetCustomerById")
            .WithSummary("Get customer profile by ID");

        group.MapPost("/customers/{id:guid}/suspend", SuspendCustomerAsync)
            .RequireAuthorization(AuthorizationPolicies.AdminPolicy)
            .WithName("AdminSuspendCustomer")
            .WithSummary("Suspend customer access and operations");

        group.MapPost("/customers/{id:guid}/reactivate", ReactivateCustomerAsync)
            .RequireAuthorization(AuthorizationPolicies.AdminPolicy)
            .WithName("AdminReactivateCustomer")
            .WithSummary("Reactivate customer profile");

        // Account freeze / unfreeze
        group.MapPost("/accounts/{id:guid}/freeze", FreezeAccountAsync)
            .RequireAuthorization(AuthorizationPolicies.AdminPolicy)
            .WithName("AdminFreezeAccount")
            .WithSummary("Freeze account to prevent debits");

        group.MapPost("/accounts/{id:guid}/unfreeze", UnfreezeAccountAsync)
            .RequireAuthorization(AuthorizationPolicies.AdminPolicy)
            .WithName("AdminUnfreezeAccount")
            .WithSummary("Unfreeze account to restore normal operations");

        // Reversal
        group.MapPost("/transactions/{id:guid}/reversal", ReverseTransactionAsync)
            .RequireAuthorization(AuthorizationPolicies.AdminPolicy)
            .AddEndpointFilter<IdempotencyEndpointFilter>()
            .WithName("AdminReverseTransaction")
            .WithSummary("Reverse transaction with compensating double-entry lines");

        // Trial Balance
        group.MapGet("/ledger/trial-balance", GetTrialBalanceAsync)
            .RequireAuthorization(AuthorizationPolicies.AdminOrAuditorPolicy)
            .WithName("AdminGetTrialBalance")
            .WithSummary("Get general ledger trial balance");

        // Reconciliation
        group.MapPost("/reconciliation/runs", RunReconciliationAsync)
            .RequireAuthorization(AuthorizationPolicies.AdminPolicy)
            .WithName("AdminRunReconciliation")
            .WithSummary("Run automated ledger-balance and gapless-sequence reconciliation");

        group.MapGet("/reconciliation/runs", GetReconciliationRunsAsync)
            .RequireAuthorization(AuthorizationPolicies.AdminOrAuditorPolicy)
            .WithName("AdminGetReconciliationRuns")
            .WithSummary("List past reconciliation runs");

        group.MapGet("/reconciliation/runs/{id:guid}", GetReconciliationRunByIdAsync)
            .RequireAuthorization(AuthorizationPolicies.AdminOrAuditorPolicy)
            .WithName("AdminGetReconciliationRunById")
            .WithSummary("Get reconciliation run details and findings");

        // Admin Dashboard KPIs
        group.MapGet("/dashboard", GetAdminDashboardAsync)
            .RequireAuthorization(AuthorizationPolicies.AdminOrAuditorPolicy)
            .WithName("AdminGetDashboard")
            .WithSummary("Get administrative summary KPIs");

        return routes;
    }

    private static async Task<IResult> SearchCustomersAsync(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int limit = 50,
        [FromServices] ICustomerService customerService = null!,
        CancellationToken ct = default)
    {
        var customers = await customerService.SearchCustomersAsync(search, status, limit, ct);
        return Results.Ok(customers);
    }

    private static async Task<IResult> GetCustomerByIdAsync(
        Guid id,
        [FromServices] ICustomerService customerService,
        CancellationToken ct)
    {
        var customer = await customerService.GetCustomerByIdAsync(id, ct);
        return customer is null ? Results.NotFound() : Results.Ok(customer);
    }

    private static async Task<IResult> SuspendCustomerAsync(
        Guid id,
        [FromBody] ReasonDto request,
        [FromServices] ICustomerService customerService,
        CancellationToken ct)
    {
        await customerService.SuspendCustomerAsync(id, request.Reason, ct);
        return Results.Ok(new { message = $"Customer {id} suspended successfully." });
    }

    private static async Task<IResult> ReactivateCustomerAsync(
        Guid id,
        [FromServices] ICustomerService customerService,
        CancellationToken ct)
    {
        await customerService.ReactivateCustomerAsync(id, ct);
        return Results.Ok(new { message = $"Customer {id} reactivated successfully." });
    }

    private static async Task<IResult> FreezeAccountAsync(
        Guid id,
        [FromBody] ReasonDto request,
        [FromServices] IAccountService accountService,
        CancellationToken ct)
    {
        await accountService.FreezeAccountAsync(id, request.Reason, ct);
        return Results.Ok(new { message = $"Account {id} frozen successfully." });
    }

    private static async Task<IResult> UnfreezeAccountAsync(
        Guid id,
        [FromBody] ReasonDto request,
        [FromServices] IAccountService accountService,
        CancellationToken ct)
    {
        await accountService.UnfreezeAccountAsync(id, request.Reason, ct);
        return Results.Ok(new { message = $"Account {id} unfrozen successfully." });
    }

    private static async Task<IResult> ReverseTransactionAsync(
        Guid id,
        [FromBody] ReversalRequest request,
        [FromServices] IPaymentService paymentService,
        ClaimsPrincipal user,
        HttpContext httpContext,
        CancellationToken ct)
    {
        string adminUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value
            ?? "ADMIN";

        Guid? recordId = httpContext.Items.TryGetValue("IdempotencyRecordId", out var rIdObj) && rIdObj is Guid rId
            ? rId
            : null;

        var reversal = await paymentService.ProcessReversalAsync(id, request, adminUserId, recordId, ct);
        httpContext.Items["TransactionId"] = reversal.Id;

        return Results.Created($"/api/v1/transactions/{reversal.Id}", reversal);
    }

    private static async Task<IResult> GetTrialBalanceAsync(
        [FromServices] IPaymentService paymentService,
        CancellationToken ct)
    {
        var trialBalance = await paymentService.GetTrialBalanceAsync(ct);
        return Results.Ok(trialBalance);
    }

    private static async Task<IResult> RunReconciliationAsync(
        [FromServices] IReconciliationService reconciliationService,
        CancellationToken ct)
    {
        var run = await reconciliationService.RunReconciliationAsync(ct);
        return Results.Ok(run);
    }

    private static async Task<IResult> GetReconciliationRunsAsync(
        [FromQuery] int limit = 20,
        [FromServices] IReconciliationService reconciliationService = null!,
        CancellationToken ct = default)
    {
        var runs = await reconciliationService.GetRunsAsync(limit, ct);
        return Results.Ok(runs);
    }

    private static async Task<IResult> GetReconciliationRunByIdAsync(
        Guid id,
        [FromServices] IReconciliationService reconciliationService,
        CancellationToken ct)
    {
        var run = await reconciliationService.GetRunByIdAsync(id, ct);
        return run is null ? Results.NotFound() : Results.Ok(run);
    }

    private static async Task<IResult> GetAdminDashboardAsync(
        [FromServices] BankDbContext db,
        CancellationToken ct)
    {
        var totalAccounts = await db.Accounts.CountAsync(ct);
        var totalCustomers = await db.Customers.CountAsync(ct);
        var activeCustomers = await db.Customers.CountAsync(c => c.Status == Domain.Enums.CustomerStatus.Active, ct);

        var totalDeposits = await db.Accounts
            .Where(a => a.Kind == Domain.Enums.AccountKind.Customer)
            .SumAsync(a => a.Balance, ct);

        var today = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        var todayTransactionsCount = await db.Transactions
            .CountAsync(t => t.PostedAt >= today, ct);

        var todayVolume = await db.Transactions
            .Where(t => t.PostedAt >= today)
            .SumAsync(t => t.Amount, ct);

        var unresolvedDiscrepancies = await db.ReconciliationFindings
            .CountAsync(f => f.ResolvedAt == null, ct);

        return Results.Ok(new
        {
            totalAccounts,
            totalCustomers,
            activeCustomers,
            totalCustomerDeposits = totalDeposits,
            todayTransactionsCount,
            todayTransactionVolume = todayVolume,
            unresolvedDiscrepancies,
            asOf = DateTimeOffset.UtcNow
        });
    }
}
